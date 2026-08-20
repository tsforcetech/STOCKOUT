using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Emcore.IdentityAccess.Worker;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxRow>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken);
    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid id, string error, int maxAttempts, CancellationToken cancellationToken);
}

public class OutboxRepository : IOutboxRepository
{
    private readonly string _connectionString;

    public OutboxRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<OutboxRow>> GetPendingBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QueryAsync<OutboxRow>(
            "dbo.PR_IDENTITY_CLAIM_OUTBOX_BATCH",
            new { BatchSize = batchSize },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED",
            new { Id = id },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task MarkFailedAsync(Guid id, string error, int maxAttempts, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_MARK_OUTBOX_FAILED",
            new { Id = id, LastError = error, MaxAttempts = maxAttempts },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
