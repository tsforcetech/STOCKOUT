using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Emcore.IdentityAccess.Worker;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxRow>> GetPendingBatchAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> MarkPublishedAsync(Guid id, byte[] claimRowVersion, CancellationToken cancellationToken);
    Task<bool> MarkFailedAsync(Guid id, byte[] claimRowVersion, string error, int maxAttempts, CancellationToken cancellationToken);
}

public class OutboxRepository : IOutboxRepository
{
    private readonly string _connectionString;

    public OutboxRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<OutboxRow>> GetPendingBatchAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QueryAsync<OutboxRow>(
            "dbo.PR_IDENTITY_CLAIM_OUTBOX_BATCH",
            new { BatchSize = batchSize, MaxAttempts = maxAttempts },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<bool> MarkPublishedAsync(Guid id, byte[] claimRowVersion, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.QuerySingleOrDefaultAsync<int?>(
            "dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED",
            new { Id = id, ClaimRowVersion = claimRowVersion },
            commandType: System.Data.CommandType.StoredProcedure);
            
        return affected.GetValueOrDefault() > 0;
    }

    public async Task<bool> MarkFailedAsync(Guid id, byte[] claimRowVersion, string error, int maxAttempts, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.QuerySingleOrDefaultAsync<int?>(
            "dbo.PR_IDENTITY_MARK_OUTBOX_FAILED",
            new { Id = id, ClaimRowVersion = claimRowVersion, LastError = error, MaxAttempts = maxAttempts },
            commandType: System.Data.CommandType.StoredProcedure);
            
        return affected.GetValueOrDefault() > 0;
    }
}
