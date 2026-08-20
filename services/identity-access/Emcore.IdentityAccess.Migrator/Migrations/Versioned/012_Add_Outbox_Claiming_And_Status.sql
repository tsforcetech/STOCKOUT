-- Migration 012
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IDENTITY_OUTBOX') AND name = 'Status')
BEGIN
    ALTER TABLE dbo.IDENTITY_OUTBOX ADD Status VARCHAR(50) NOT NULL DEFAULT 'Pending';
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IDENTITY_OUTBOX') AND name = 'ClaimedAtUtc')
BEGIN
    ALTER TABLE dbo.IDENTITY_OUTBOX ADD ClaimedAtUtc DATETIME2 NULL;
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IDENTITY_OUTBOX') AND name = 'LastAttemptAtUtc')
BEGIN
    ALTER TABLE dbo.IDENTITY_OUTBOX ADD LastAttemptAtUtc DATETIME2 NULL;
END
GO

-- Update existing records if modifying for the first time
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.IDENTITY_OUTBOX') AND name = 'IsPublished')
BEGIN
    UPDATE dbo.IDENTITY_OUTBOX
    SET Status = CASE 
        WHEN IsPublished = 1 THEN 'Published'
        WHEN AttemptCount >= 10 THEN 'Failed'
        ELSE 'Pending'
    END
    WHERE Status = 'Pending';
END
GO

DROP PROCEDURE IF EXISTS dbo.PR_IDENTITY_GET_PENDING_OUTBOX;
DROP PROCEDURE IF EXISTS dbo.PR_IDENTITY_CLAIM_OUTBOX_BATCH;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CLAIM_OUTBOX_BATCH
    @BatchSize INT = 50,
    @MaxAttempts INT = 10,
    @StaleTimeoutMinutes INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Now DATETIME2 = GETUTCDATE();
    
    WITH CTE AS (
        SELECT TOP (@BatchSize) *
        FROM dbo.IDENTITY_OUTBOX WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE (Status = 'Pending' OR (Status = 'Processing' AND DATEADD(MINUTE, @StaleTimeoutMinutes, ClaimedAtUtc) < @Now))
          AND AttemptCount < @MaxAttempts
          AND (LastAttemptAtUtc IS NULL OR DATEADD(MINUTE, AttemptCount, LastAttemptAtUtc) < @Now)
        ORDER BY CreatedAtUtc ASC
    )
    UPDATE CTE
    SET Status = 'Processing',
        ClaimedAtUtc = @Now
    OUTPUT 
        inserted.Id, 
        inserted.MessageType, 
        inserted.SchemaVersion, 
        inserted.Payload, 
        inserted.CreatedAtUtc, 
        inserted.AttemptCount,
        inserted.CorrelationId,
        inserted.TraceId,
        inserted.RowVersion;
END;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED
    @Id UNIQUEIDENTIFIER,
    @ClaimRowVersion TIMESTAMP
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDENTITY_OUTBOX 
    SET IsPublished = 1, 
        Status = 'Published',
        PublishedAtUtc = GETUTCDATE(),
        LastError = NULL,
        ClaimedAtUtc = NULL
    WHERE Id = @Id
      AND Status = 'Processing'
      AND RowVersion = @ClaimRowVersion;

    SELECT @@ROWCOUNT;
END;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_FAILED
    @Id UNIQUEIDENTIFIER,
    @ClaimRowVersion TIMESTAMP,
    @LastError NVARCHAR(MAX),
    @MaxAttempts INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDENTITY_OUTBOX 
    SET AttemptCount = AttemptCount + 1, 
        LastError = @LastError,
        LastAttemptAtUtc = GETUTCDATE(),
        ClaimedAtUtc = NULL,
        Status = CASE WHEN AttemptCount + 1 >= @MaxAttempts THEN 'Failed' ELSE 'Pending' END
    WHERE Id = @Id
      AND Status = 'Processing'
      AND RowVersion = @ClaimRowVersion;

    SELECT @@ROWCOUNT;
END;
GO
