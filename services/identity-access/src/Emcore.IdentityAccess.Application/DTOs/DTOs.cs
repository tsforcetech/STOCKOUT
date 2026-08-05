using System;
using System.Collections.Generic;

namespace Emcore.IdentityAccess.Application.DTOs;

public record RegisterRequest(string Email, string Mobile, string Password);
public record RegisterResponse(string UserId, string Email, string Mobile);

public record VerifyRequest(string UserId, string Token, string Channel);
public record ResendVerificationRequest(string UserId, string Channel);

public record LoginRequest(string EmailOrMobile, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public record ForgotPasswordRequest(string EmailOrMobile);
public record ResetPasswordRequest(string Token, string NewPassword);

public record SessionDto(string SessionId, string Status, DateTime CreatedAtUtc, DateTime? RevokedAtUtc);
