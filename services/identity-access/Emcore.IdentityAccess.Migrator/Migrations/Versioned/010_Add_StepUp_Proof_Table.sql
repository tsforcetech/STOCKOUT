-- Migration 010: Add StepUp Proof Table for Hardening
-- Creates the table and procedures to support secure, one-time Step-Up proofs.

CREATE TABLE dbo.STEP_UP_PROOF
(
    ProofId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionId UNIQUEIDENTIFIER NULL,
    TargetAction NVARCHAR(100) NOT NULL,
    ProofHash NVARCHAR(255) NOT NULL,
    IssuedAtUtc DATETIME2 NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    ConsumedAtUtc DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Issued'
);
GO

CREATE INDEX IX_STEP_UP_PROOF_UserId ON dbo.STEP_UP_PROOF(UserId);
GO
CREATE INDEX IX_STEP_UP_PROOF_ProofHash ON dbo.STEP_UP_PROOF(ProofHash);
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_STEPUP_PROOF
    @ProofId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @SessionId UNIQUEIDENTIFIER = NULL,
    @TargetAction NVARCHAR(100),
    @ProofHash NVARCHAR(255),
    @IssuedAtUtc DATETIME2,
    @ExpiresAtUtc DATETIME2,
    @Status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.STEP_UP_PROOF (ProofId, UserId, SessionId, TargetAction, ProofHash, IssuedAtUtc, ExpiresAtUtc, Status)
    VALUES (@ProofId, @UserId, @SessionId, @TargetAction, @ProofHash, @IssuedAtUtc, @ExpiresAtUtc, @Status);
END
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CONSUME_STEPUP_PROOF
    @ProofHash NVARCHAR(255),
    @UserId UNIQUEIDENTIFIER,
    @SessionId UNIQUEIDENTIFIER = NULL,
    @TargetAction NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @ProofId UNIQUEIDENTIFIER;

    -- Atomic check and consume
    UPDATE dbo.STEP_UP_PROOF
    SET Status = 'Consumed', ConsumedAtUtc = @Now
    OUTPUT INSERTED.ProofId
    WHERE ProofHash = @ProofHash
      AND UserId = @UserId
      AND (SessionId = @SessionId OR (@SessionId IS NULL AND SessionId IS NULL))
      AND TargetAction = @TargetAction
      AND Status = 'Issued'
      AND ExpiresAtUtc >= @Now
      AND ConsumedAtUtc IS NULL;
END
GO

-- Add SessionId to STEP_UP_CHALLENGE
ALTER TABLE dbo.STEP_UP_CHALLENGE ADD SessionId UNIQUEIDENTIFIER NULL;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_CREATE_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @SessionId UNIQUEIDENTIFIER = NULL,
    @TokenHash NVARCHAR(255),
    @TargetAction NVARCHAR(100),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.STEP_UP_CHALLENGE (Id, UserId, SessionId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc, AttemptCount)
    VALUES (@Id, @UserId, @SessionId, @TokenHash, @TargetAction, 'Issued', @ExpiresAtUtc, SYSUTCDATETIME(), 0);
END
GO

ALTER PROCEDURE dbo.PR_IDENTITY_GET_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, SessionId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc, AttemptCount, ConsumedAtUtc
    FROM dbo.STEP_UP_CHALLENGE
    WHERE Id = @Id AND UserId = @UserId;
END
GO

ALTER PROCEDURE dbo.PR_IDENTITY_CONSUME_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TargetAction NVARCHAR(100),
    @TokenHash NVARCHAR(255),
    @MaxAttempts INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @CurrentStatus NVARCHAR(50);
    DECLARE @CurrentHash NVARCHAR(255);
    DECLARE @CurrentAttempts INT;
    DECLARE @ExpiresAt DATETIME2;

    -- We need atomic consumption/attempt tracking
    -- Lock the row
    SELECT @CurrentStatus = Status, 
           @CurrentHash = TokenHash, 
           @CurrentAttempts = AttemptCount,
           @ExpiresAt = ExpiresAtUtc
        FROM dbo.STEP_UP_CHALLENGE WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @Id AND UserId = @UserId AND TargetAction = @TargetAction;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT NULL AS Id; -- Not found or wrong user/action
        RETURN;
    END

    IF @CurrentStatus != 'Issued' OR @ExpiresAt < @Now
    BEGIN
        IF @CurrentStatus = 'Issued'
        BEGIN
            UPDATE dbo.STEP_UP_CHALLENGE SET Status = 'Failed' WHERE Id = @Id;
        END
        SELECT NULL AS Id;
        RETURN;
    END

    IF @CurrentHash != @TokenHash
    BEGIN
        UPDATE dbo.STEP_UP_CHALLENGE SET AttemptCount = AttemptCount + 1 WHERE Id = @Id;
        
        -- If max reached, lock it
        IF (@CurrentAttempts + 1) >= @MaxAttempts
        BEGIN
            UPDATE dbo.STEP_UP_CHALLENGE SET Status = 'Failed' WHERE Id = @Id;
        END

        SELECT NULL AS Id;
        RETURN;
    END

    -- Success
    UPDATE dbo.STEP_UP_CHALLENGE 
    SET Status = 'Verified', ConsumedAtUtc = @Now 
    OUTPUT INSERTED.Id
    WHERE Id = @Id;
END
GO
