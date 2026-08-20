-- Migration 012
ALTER TABLE dbo.IDENTITY_OUTBOX ADD Status VARCHAR(50) NOT NULL DEFAULT 'Pending';
ALTER TABLE dbo.IDENTITY_OUTBOX ADD ClaimedAtUtc DATETIME2 NULL;
ALTER TABLE dbo.IDENTITY_OUTBOX ADD LastAttemptAtUtc DATETIME2 NULL;
GO

-- Update existing records
UPDATE dbo.IDENTITY_OUTBOX
SET Status = CASE 
    WHEN IsPublished = 1 THEN 'Published'
    WHEN AttemptCount >= 10 THEN 'Failed'
    ELSE 'Pending'
END;
GO

-- We drop old procedures and recreate them or modify
DROP PROCEDURE IF EXISTS dbo.PR_IDENTITY_GET_PENDING_OUTBOX;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CLAIM_OUTBOX_BATCH
    @BatchSize INT = 50,
    @StaleTimeoutMinutes INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Now DATETIME2 = GETUTCDATE();
    
    -- Claim pending or stale processing messages
    WITH CTE AS (
        SELECT TOP (@BatchSize) *
        FROM dbo.IDENTITY_OUTBOX WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE (Status = 'Pending' OR (Status = 'Processing' AND DATEADD(MINUTE, @StaleTimeoutMinutes, ClaimedAtUtc) < @Now))
          AND AttemptCount < 10
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
        inserted.TraceId;
END;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDENTITY_OUTBOX 
    SET IsPublished = 1, 
        Status = 'Published',
        PublishedAtUtc = GETUTCDATE(),
        LastError = NULL,
        ClaimedAtUtc = NULL
    WHERE Id = @Id;
END;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_FAILED
    @Id UNIQUEIDENTIFIER,
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
    WHERE Id = @Id;
END;
GO
