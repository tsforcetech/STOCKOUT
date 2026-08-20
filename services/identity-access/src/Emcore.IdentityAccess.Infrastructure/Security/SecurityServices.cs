using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Emcore.IdentityAccess.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Emcore.IdentityAccess.Infrastructure.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100000;
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private static readonly HashAlgorithmName HashAlg = HashAlgorithmName.SHA512;

    public string AlgorithmName => "PBKDF2-SHA512-V1";

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlg, HashSize);

        return $"v1:pbkdf2:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;

        var parts = hash.Split(':');
        if (parts.Length != 5 || parts[0] != "v1" || parts[1] != "pbkdf2")
        {
            return false;
        }

        if (!int.TryParse(parts[2], out int iterations)) return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedHash = Convert.FromBase64String(parts[4]);
        }
        catch
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlg, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

public class JwtTokenGenerator : ITokenGenerator, IJwksService
{
    private readonly RSA _rsaKey;
    private readonly string _keyId;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly IConfiguration? _configuration;

    public JwtTokenGenerator(IConfiguration? configuration = null)
    {
        _configuration = configuration;

        bool isTest = configuration == null ||
                      configuration.GetConnectionString("IdentityDatabase") == "inmemory-test-db";

        bool jwtEnabled = configuration?.GetValue<bool>("Jwt:Enabled") ?? (configuration != null);

        _issuer = configuration?["Jwt:Issuer"] ?? "";
        _audience = configuration?["Jwt:Audience"] ?? "";
        _keyId = configuration?["Jwt:KeyId"] ?? "";
        string? configuredKey = configuration?["Jwt:SigningKey"];
        int tokenLifetime = configuration?.GetValue<int>("Jwt:AccessTokenLifetimeMinutes") ?? 0;

        string? otpPepper = configuration?["Otp:HmacPepper"] ?? configuration?["Security:OtpPepper"];

        if (configuration != null && jwtEnabled)
        {
            if (string.IsNullOrWhiteSpace(_issuer)) throw new InvalidOperationException("Startup validation failed: Jwt:Issuer is missing.");
            if (string.IsNullOrWhiteSpace(_audience)) throw new InvalidOperationException("Startup validation failed: Jwt:Audience is missing.");
            if (string.IsNullOrWhiteSpace(_keyId)) throw new InvalidOperationException("Startup validation failed: Jwt:KeyId is missing.");
            if (string.IsNullOrWhiteSpace(configuredKey)) throw new InvalidOperationException("Startup validation failed: Jwt:SigningKey is missing.");
            if (tokenLifetime <= 0) throw new InvalidOperationException("Startup validation failed: Jwt:AccessTokenLifetimeMinutes must be > 0.");
        }
        else if (configuration == null)
        {
            _issuer = "https://identity.emcore.platform";
            _audience = "https://api.emcore.platform";
            _keyId = "emcore-id-key-v1";
        }

        if (configuration != null && string.IsNullOrWhiteSpace(otpPepper))
        {
            throw new InvalidOperationException("Startup validation failed: Otp:HmacPepper is missing.");
        }

        if (isTest && string.IsNullOrWhiteSpace(configuredKey))
        {
            _rsaKey = RSA.Create(2048);
        }
        else
        {
            _rsaKey = RSA.Create();
            if (!string.IsNullOrWhiteSpace(configuredKey))
            {
                try
                {
                    _rsaKey.ImportFromPem(configuredKey.ToCharArray());
                }
                catch
                {
                    try
                    {
                        byte[] keyBytes = Convert.FromBase64String(configuredKey);
                        _rsaKey.ImportPkcs8PrivateKey(keyBytes, out _);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Startup validation failed: Jwt:SigningKey could not be imported as a valid RSA private key.", ex);
                    }
                }
            }
        }
    }

    public string GenerateAccessToken(string userId, string email, string sessionId, bool emailVerified, string amr = "pwd")
    {
        var now = DateTimeOffset.UtcNow;
        int lifetimeMinutes = _configuration?.GetValue<int>("Jwt:AccessTokenLifetimeMinutes") ?? 15;
        var exp = now.AddMinutes(lifetimeMinutes > 0 ? lifetimeMinutes : 15);

        var header = new { alg = "RS256", typ = "JWT", kid = _keyId };
        var payload = new
        {
            sub = userId,
            jti = Guid.NewGuid().ToString("N"),
            sid = sessionId,
            email = email,
            email_verified = emailVerified,
            iat = now.ToUnixTimeSeconds(),
            nbf = now.ToUnixTimeSeconds() - 60, // 60s clock skew
            exp = exp.ToUnixTimeSeconds(),
            iss = _issuer,
            aud = _audience,
            amr = new[] { amr },
            auth_time = now.ToUnixTimeSeconds(),
            sec_ver = 1
        };

        string headerBase64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        string payloadBase64 = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        string unsignedToken = $"{headerBase64}.{payloadBase64}";

        byte[] signatureBytes = _rsaKey.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{unsignedToken}.{signatureBase64}";
    }

    public (string Token, string Hash) GenerateRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(48);
        string token = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return (token, HashToken(token));
    }

    public (string Token, string Hash) GenerateVerificationToken()
    {
        int otp = RandomNumberGenerator.GetInt32(100000, 999999);
        string token = otp.ToString();
        return (token, HashToken(token));
    }

    public (string Token, string Hash) GenerateKeyedVerificationToken(string verificationId, string normalizedDestination)
    {
        int otp = RandomNumberGenerator.GetInt32(100000, 999999);
        string token = otp.ToString();
        return (token, HashKeyedToken(verificationId, normalizedDestination, token));
    }

    public (string Token, string Hash) GeneratePasswordResetToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return (token, HashToken(token));
    }

    public string HashToken(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return string.Empty;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string HashKeyedToken(string verificationId, string normalizedDestination, string rawOtp)
    {
        if (string.IsNullOrEmpty(rawOtp)) return string.Empty;
        string? pepper = _configuration?["Otp:HmacPepper"] ?? _configuration?["Security:OtpPepper"];
        if (string.IsNullOrEmpty(pepper))
        {
            if (_configuration == null) pepper = "default-dev-test-hmac-pepper-do-not-use-in-prod";
            else throw new InvalidOperationException("Otp:HmacPepper is missing.");
        }
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        string data = $"{verificationId}|{normalizedDestination.ToLowerInvariant()}|{rawOtp}";
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string GetJwksJson()
    {
        var parameters = _rsaKey.ExportParameters(false);
        string mod = Base64UrlEncode(parameters.Modulus!);
        string exp = Base64UrlEncode(parameters.Exponent!);

        var jwk = new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = _keyId,
            n = mod,
            e = exp
        };

        var jwks = new { keys = new[] { jwk } };
        return JsonSerializer.Serialize(jwks, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
