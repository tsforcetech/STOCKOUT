namespace Emcore.BuildingBlocks.Data;

public sealed class SqlDatabaseOptions
{
    public const string SectionName = "Database";
    public string? ConnectionString { get; init; }
    public int CommandTimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; }
}

public interface ISqlConnectionFactory { }
public class SqlConnectionFactory : ISqlConnectionFactory { }
public interface IStoredProcedureExecutor
{
    System.Threading.Tasks.Task ExecuteAsync(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<T>> QueryAsync<T>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<T?> QuerySingleOrDefaultAsync<T>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<TResult> QueryMultipleAsync<TResult>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default);
}
public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    public System.Threading.Tasks.Task ExecuteAsync(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<T>> QueryAsync<T>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(System.Linq.Enumerable.Empty<T>());
    public System.Threading.Tasks.Task<T?> QuerySingleOrDefaultAsync<T>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(default(T));
    public System.Threading.Tasks.Task<TResult> QueryMultipleAsync<TResult>(StoredProcedureCommand command, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(default(TResult)!);
}
public class StoredProcedureCommand { }
public class DatabaseDependencyState { }
public class DatabaseNotConfiguredException : System.Exception { }
public static class DatabaseRegistrationExtensions { }
