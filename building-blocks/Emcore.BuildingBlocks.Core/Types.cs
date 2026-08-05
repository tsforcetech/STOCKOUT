namespace Emcore.BuildingBlocks.Core;

public class Result { }
public class Result<T> { }
public class Error { }
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
