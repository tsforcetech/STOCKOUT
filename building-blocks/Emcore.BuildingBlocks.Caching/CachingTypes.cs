namespace Emcore.BuildingBlocks.Caching;

public class RedisOptions { }
public interface ICacheService { }
public class NoOpCacheService : ICacheService { }
public class RedisCacheService : ICacheService { }
public class CacheKeyBuilder { }
public static class CacheRegistrationExtensions { }
