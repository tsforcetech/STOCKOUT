namespace Emcore.BuildingBlocks.Idempotency;

public class IdempotencyOptions { }
public interface IIdempotencyStore { }
public class IdempotencyRequest { }
public class IdempotencyResult { }
public enum IdempotencyStatus { Pending, Success, Failed }
public class IdempotencyKeyValidator { }
public class NoOpIdempotencyStore : IIdempotencyStore { }
