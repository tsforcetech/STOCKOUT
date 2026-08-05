using System;

namespace Emcore.IdentityAccess.Domain.Exceptions;

public class IdentityDomainException : Exception
{
    public string ErrorCode { get; }
    public IdentityDomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
