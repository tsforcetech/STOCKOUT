CREATE OR ALTER PROCEDURE dbo.PR_IDENTITY_VERIFY_ACCOUNT
    @UserId UNIQUEIDENTIFIER,
    @Channel VARCHAR(50),
    @TokenHash VARCHAR(255),
    @OutboxId UNIQUEIDENTIFIER,
    @OutboxMessageType VARCHAR(255),
    @OutboxPayload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @VerId UNIQUEIDENTIFIER;
    DECLARE @StoredHash VARCHAR(255);
    DECLARE @Expires DATETIME2;
    DECLARE @Status VARCHAR(50);
    DECLARE @AttemptCount INT;

    BEGIN TRANSACTION;
    BEGIN TRY
        SELECT TOP 1 @VerId = Id, @StoredHash = TokenHash, @Expires = ExpiresAtUtc, @Status = Status, @AttemptCount = AttemptCount
        FROM dbo.ACCOUNT_VERIFICATION WITH (UPDLOCK, ROWLOCK)
        WHERE UserId = @UserId AND Channel = @Channel AND Status = 'Issued'
        ORDER BY CreatedAtUtc DESC;
        
        IF @VerId IS NULL OR @Status <> 'Issued' OR @Expires < GETUTCDATE() OR @StoredHash <> @TokenHash OR @AttemptCount >= 5
        BEGIN
            IF @VerId IS NOT NULL
            BEGIN
                UPDATE dbo.ACCOUNT_VERIFICATION SET AttemptCount = AttemptCount + 1, UpdatedAtUtc = GETUTCDATE() WHERE Id = @VerId;
                IF @AttemptCount + 1 >= 5
                BEGIN
                    UPDATE dbo.ACCOUNT_VERIFICATION SET Status = 'Expired', UpdatedAtUtc = GETUTCDATE() WHERE Id = @VerId;
                END
            END
            COMMIT TRANSACTION;
            RETURN -1; -- Invalid or expired
        END

        UPDATE dbo.ACCOUNT_VERIFICATION SET Status = 'Verified', UpdatedAtUtc = GETUTCDATE() WHERE Id = @VerId;
        
        IF @Channel = 'Email'
            UPDATE dbo.USER_EMAIL SET IsVerified = 1, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
        ELSE IF @Channel = 'Mobile'
            UPDATE dbo.USER_MOBILE SET IsVerified = 1, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
            
        UPDATE dbo.USER_ACCOUNT SET Status = 'Active', UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId AND Status = 'PendingVerification';
        
        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.PR_IDENTITY_RESET_PASSWORD
    @UserId UNIQUEIDENTIFIER = NULL,
    @TokenHash VARCHAR(255),
    @NewPasswordHash VARCHAR(255),
    @HashAlgorithm VARCHAR(50),
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RecId UNIQUEIDENTIFIER;
    DECLARE @ResolvedUserId UNIQUEIDENTIFIER;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        SELECT TOP 1 @RecId = Id, @ResolvedUserId = UserId FROM dbo.ACCOUNT_RECOVERY WITH (UPDLOCK, ROWLOCK)
        WHERE TokenHash = @TokenHash AND Status = 'Created' AND ExpiresAtUtc > GETUTCDATE();
        
        IF @RecId IS NULL
        BEGIN
            COMMIT TRANSACTION;
            RETURN -1; -- Invalid recovery challenge
        END
        
        IF @UserId IS NOT NULL AND @UserId <> @ResolvedUserId
        BEGIN
            COMMIT TRANSACTION;
            RETURN -1; -- Mismatched user
        END
        
        UPDATE dbo.ACCOUNT_RECOVERY SET Status = 'Completed' WHERE Id = @RecId;
        
        INSERT INTO dbo.PASSWORD_HISTORY (UserId, PasswordHash, CreatedAtUtc)
        SELECT UserId, PasswordHash, GETUTCDATE() FROM dbo.USER_CREDENTIAL WHERE UserId = @ResolvedUserId;
        
        UPDATE dbo.USER_CREDENTIAL SET PasswordHash = @NewPasswordHash, HashAlgorithm = @HashAlgorithm, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @ResolvedUserId;
        
        -- Revoke all sessions on password reset
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE UserId = @ResolvedUserId AND Status = 'Active';
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE SessionId IN (SELECT Id FROM dbo.USER_SESSION WHERE UserId = @ResolvedUserId);
        
        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
