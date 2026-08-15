-- Migration 009: Add Attempt Tracking and Consumption to StepUp Challenge

ALTER TABLE dbo.STEP_UP_CHALLENGE ADD AttemptCount INT NOT NULL DEFAULT 0;
ALTER TABLE dbo.STEP_UP_CHALLENGE ADD ConsumedAtUtc DATETIME2 NULL;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_CREATE_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @TargetAction VARCHAR(100),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    INSERT INTO dbo.STEP_UP_CHALLENGE (Id, UserId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc, AttemptCount)
    VALUES (@Id, @UserId, @TokenHash, @TargetAction, 'Issued', @ExpiresAtUtc, GETUTCDATE(), 0);
END;
GO

ALTER PROCEDURE dbo.PR_IDENTITY_GET_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc, AttemptCount, ConsumedAtUtc
    FROM dbo.STEP_UP_CHALLENGE
    WHERE Id = @Id AND UserId = @UserId;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CONSUME_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @MaxAttempts INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @CurrentStatus VARCHAR(50);
        DECLARE @CurrentAttempts INT;
        DECLARE @ExpiresAt DATETIME2;
        DECLARE @ActualTokenHash VARCHAR(255);

        SELECT @CurrentStatus = Status, @CurrentAttempts = AttemptCount, @ExpiresAt = ExpiresAtUtc, @ActualTokenHash = TokenHash
        FROM dbo.STEP_UP_CHALLENGE WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @Id AND UserId = @UserId;

        IF @CurrentStatus IS NULL
        BEGIN
            COMMIT TRANSACTION;
            RETURN -1; -- Not Found
        END

        IF @CurrentStatus <> 'Issued' OR @ExpiresAt < GETUTCDATE()
        BEGIN
            COMMIT TRANSACTION;
            RETURN -2; -- Invalid or Expired
        END

        IF @CurrentAttempts >= @MaxAttempts
        BEGIN
            UPDATE dbo.STEP_UP_CHALLENGE SET Status = 'Failed' WHERE Id = @Id;
            COMMIT TRANSACTION;
            RETURN -3; -- Max Attempts Reached
        END
        
        -- Increment attempt count
        UPDATE dbo.STEP_UP_CHALLENGE SET AttemptCount = AttemptCount + 1 WHERE Id = @Id;

        -- Verify token hash
        IF @ActualTokenHash <> @TokenHash
        BEGIN
            -- If this attempt reached the limit, mark the challenge as failed
            IF (@CurrentAttempts + 1) >= @MaxAttempts
            BEGIN
                UPDATE dbo.STEP_UP_CHALLENGE SET Status = 'Failed' WHERE Id = @Id;
            END
            COMMIT TRANSACTION;
            RETURN -4; -- Invalid Hash
        END

        -- Mark as consumed/verified
        UPDATE dbo.STEP_UP_CHALLENGE 
        SET Status = 'Verified', ConsumedAtUtc = GETUTCDATE() 
        WHERE Id = @Id;

        COMMIT TRANSACTION;
        RETURN 0; -- Success
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
