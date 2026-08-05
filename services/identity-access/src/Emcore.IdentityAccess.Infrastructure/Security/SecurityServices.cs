using System;
using System.Security.Cryptography;
using Emcore.IdentityAccess.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Emcore.IdentityAccess.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string AlgorithmName => "BCrypt";

    public string HashPassword(string password)
    {
        // Placeholder for BCrypt hash
        return password + "_hashed";
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Placeholder for BCrypt verify
        return hash == password + "_hashed";
    }
}

public class JwtTokenGenerator : ITokenGenerator
{
    public string GenerateAccessToken(string userId, string email, string sessionId)
    {
        return $"jwt_access_token_for_{userId}";
    }

    public (string Token, string Hash) GenerateRefreshToken()
    {
        var token = Guid.NewGuid().ToString("N");
        return (token, token + "_hash");
    }

    public (string Token, string Hash) GenerateVerificationToken()
    {
        var token = Guid.NewGuid().ToString("N");
        return (token, token + "_hash");
    }

    public (string Token, string Hash) GeneratePasswordResetToken()
    {
        var token = Guid.NewGuid().ToString("N");
        return (token, token + "_hash");
    }
}
