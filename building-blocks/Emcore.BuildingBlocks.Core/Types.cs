namespace Emcore.BuildingBlocks.Core;

public class Result 
{ 
    public bool IsSuccess { get; set; } = true;
    public Error? Error { get; set; }
    public static Result Success() => new Result();
    public static Result Failure(Error error) => new Result { IsSuccess = false, Error = error };
}

public class Result<T> : Result 
{ 
    public T? Value { get; set; }
    public static Result<T> Success(T value) => new Result<T> { Value = value, IsSuccess = true };
    public static implicit operator Result<T>(T? value) => new Result<T> { Value = value, IsSuccess = value != null };
}

public class Error 
{ 
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ErrorType Type { get; set; }
}

public enum ErrorType { Failure, Validation, NotFound, Conflict, Forbidden }
public class DomainException : System.Exception { }
public class NotFoundException : System.Exception { }
public class ConflictException : System.Exception { }
public class ForbiddenException : System.Exception { }
public class ValidationException : System.Exception { }
public interface IClock { }
public class SystemClock : IClock { }
public interface IIdGenerator { }
public class UlidGenerator : IIdGenerator { }
public static class Guard { }
