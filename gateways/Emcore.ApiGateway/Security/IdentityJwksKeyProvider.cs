using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Emcore.ApiGateway.Security;

public interface IJwksKeyProvider
{
    IEnumerable<SecurityKey> GetKeys(string kid);
}

public class IdentityJwksKeyProvider : IJwksKeyProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _jwksUrl;
    private readonly ILogger<IdentityJwksKeyProvider> _logger;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private const string CacheKey = "IdentityJwks";

    public IdentityJwksKeyProvider(
        IHttpClientFactory httpClientFactory,
        string jwksUrl,
        IMemoryCache cache,
        ILogger<IdentityJwksKeyProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jwksUrl = jwksUrl;
        _cache = cache;
        _logger = logger;
    }

    public IEnumerable<SecurityKey> GetKeys(string kid)
    {
        var keys = GetKeysAsync().GetAwaiter().GetResult();
        var key = keys.FirstOrDefault(k => k.KeyId == kid);

        if (key == null)
        {
            _logger.LogWarning("Key {Kid} not found in cache, refreshing JWKS...", kid);
            // Refresh once if unknown kid
            // Clear cache and refresh
            _cache.Remove(CacheKey);
            keys = RefreshKeysAsync().GetAwaiter().GetResult();
            key = keys.FirstOrDefault(k => k.KeyId == kid);
        }

        if (key != null)
        {
            return new[] { key };
        }

        return Enumerable.Empty<SecurityKey>();
    }

    private async Task<List<SecurityKey>> GetKeysAsync()
    {
        if (_cache.TryGetValue<List<SecurityKey>>(CacheKey, out var keys) && keys != null)
        {
            return keys;
        }
        return await RefreshKeysAsync();
    }

    private async Task<List<SecurityKey>> RefreshKeysAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // double-check
            if (_cache.TryGetValue<List<SecurityKey>>(CacheKey, out var cachedKeys) && cachedKeys != null)
            {
                // if they were just refreshed by another thread
                return cachedKeys;
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(_jwksUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jwks = JsonSerializer.Deserialize<JwksResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var result = new List<SecurityKey>();
            if (jwks?.Keys != null)
            {
                foreach (var keyData in jwks.Keys)
                {
                    if (keyData.Kty?.ToUpperInvariant() == "RSA" && !string.IsNullOrEmpty(keyData.N) && !string.IsNullOrEmpty(keyData.E))
                    {
                        var rsa = RSA.Create();
                        rsa.ImportParameters(new RSAParameters
                        {
                            Modulus = Base64UrlEncoder.DecodeBytes(keyData.N),
                            Exponent = Base64UrlEncoder.DecodeBytes(keyData.E)
                        });
                        var rsaKey = new RsaSecurityKey(rsa) { KeyId = keyData.Kid };
                        result.Add(rsaKey);
                    }
                }
            }

            _cache.Set(CacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh JWKS keys.");
            return new List<SecurityKey>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private class JwksResponse
    {
        [JsonPropertyName("keys")]
        public List<JwkKey>? Keys { get; set; }
    }

    private class JwkKey
    {
        [JsonPropertyName("kty")]
        public string? Kty { get; set; }
        [JsonPropertyName("kid")]
        public string? Kid { get; set; }
        [JsonPropertyName("n")]
        public string? N { get; set; }
        [JsonPropertyName("e")]
        public string? E { get; set; }
    }
}
