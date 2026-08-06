CREATE PROCEDURE dbo.PR_IDENTITY_REGISTER_USER
    @Id UNIQUEIDENTIFIER,
    @UlidId VARCHAR(26),
    @EmailAddress VARCHAR(255),
    @NormalizedEmail VARCHAR(255),
    @MobileNumber VARCHAR(50),
    @NormalizedMobile VARCHAR(50),
    @PasswordHash VARCHAR(255),
    @HashAlgorithm VARCHAR(50),
    @VerificationId UNIQUEIDENTIFIER,
    @VerificationTokenHash VARCHAR(255),
    @VerificationChannel VARCHAR(50),
    @VerificationExpiresAtUtc DATETIME2,
    @OutboxId UNIQUEIDENTIFIER,
    @OutboxMessageType VARCHAR(255),
    @OutboxPayload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM dbo.USER_EMAIL WHERE NormalizedEmail = @NormalizedEmail AND NormalizedEmail <> '')
    BEGIN
        RETURN -1; -- Duplicate Email
    END
    
    IF EXISTS (SELECT 1 FROM dbo.USER_MOBILE WHERE NormalizedMobile = @NormalizedMobile AND NormalizedMobile <> '')
    BEGIN
        RETURN -2; -- Duplicate Mobile
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.USER_ACCOUNT (Id, UlidId, Status, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@Id, @UlidId, 'PendingVerification', GETUTCDATE(), GETUTCDATE());
        
        INSERT INTO dbo.USER_CREDENTIAL (UserId, PasswordHash, HashAlgorithm, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@Id, @PasswordHash, @HashAlgorithm, GETUTCDATE(), GETUTCDATE());
        
        INSERT INTO dbo.USER_EMAIL (UserId, EmailAddress, NormalizedEmail, IsVerified, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@Id, @EmailAddress, @NormalizedEmail, 0, GETUTCDATE(), GETUTCDATE());
        
        IF (@MobileNumber IS NOT NULL AND @MobileNumber <> '')
        BEGIN
            INSERT INTO dbo.USER_MOBILE (UserId, MobileNumber, NormalizedMobile, IsVerified, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@Id, @MobileNumber, @NormalizedMobile, 0, GETUTCDATE(), GETUTCDATE());
        END
        
        INSERT INTO dbo.ACCOUNT_VERIFICATION (Id, UserId, TokenHash, Channel, Status, AttemptCount, ExpiresAtUtc, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@VerificationId, @Id, @VerificationTokenHash, @VerificationChannel, 'Issued', 0, @VerificationExpiresAtUtc, GETUTCDATE(), GETUTCDATE());
        
        INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
        VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_USER_BY_EMAIL_OR_MOBILE
    @Identifier VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserId UNIQUEIDENTIFIER = NULL;
    
    SELECT @UserId = UserId FROM dbo.USER_EMAIL WHERE NormalizedEmail = @Identifier;
    IF @UserId IS NULL
        SELECT @UserId = UserId FROM dbo.USER_MOBILE WHERE NormalizedMobile = @Identifier;
        
    IF @UserId IS NOT NULL
    BEGIN
        SELECT 
            u.Id, u.UlidId, u.Status, u.CreatedAtUtc, u.UpdatedAtUtc,
            e.EmailAddress, e.NormalizedEmail, e.IsVerified AS EmailVerified,
            m.MobileNumber, m.NormalizedMobile, m.IsVerified AS MobileVerified,
            c.PasswordHash, c.HashAlgorithm,
            ISNULL(l.FailedCount, 0) AS FailedCount, l.LockoutEndUtc
        FROM dbo.USER_ACCOUNT u
        INNER JOIN dbo.USER_CREDENTIAL c ON u.Id = c.UserId
        INNER JOIN dbo.USER_EMAIL e ON u.Id = e.UserId
        LEFT JOIN dbo.USER_MOBILE m ON u.Id = m.UserId
        LEFT JOIN dbo.LOGIN_ATTEMPT l ON u.Id = l.UserId
        WHERE u.Id = @UserId;
    END
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_USER_BY_ID
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        u.Id, u.UlidId, u.Status, u.CreatedAtUtc, u.UpdatedAtUtc,
        e.EmailAddress, e.NormalizedEmail, e.IsVerified AS EmailVerified,
        m.MobileNumber, m.NormalizedMobile, m.IsVerified AS MobileVerified,
        ISNULL(l.FailedCount, 0) AS FailedCount, l.LockoutEndUtc
    FROM dbo.USER_ACCOUNT u
    INNER JOIN dbo.USER_EMAIL e ON u.Id = e.UserId
    LEFT JOIN dbo.USER_MOBILE m ON u.Id = m.UserId
    LEFT JOIN dbo.LOGIN_ATTEMPT l ON u.Id = l.UserId
    WHERE u.Id = @UserId;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_VERIFICATION
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @Channel VARCHAR(50),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ACCOUNT_VERIFICATION SET Status = 'Cancelled', UpdatedAtUtc = GETUTCDATE()
    WHERE UserId = @UserId AND Channel = @Channel AND Status = 'Issued';
    
    INSERT INTO dbo.ACCOUNT_VERIFICATION (Id, UserId, TokenHash, Channel, Status, AttemptCount, ExpiresAtUtc, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@Id, @UserId, @TokenHash, @Channel, 'Issued', 0, @ExpiresAtUtc, GETUTCDATE(), GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_VERIFY_ACCOUNT
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
    
    SELECT TOP 1 @VerId = Id, @StoredHash = TokenHash, @Expires = ExpiresAtUtc, @Status = Status
    FROM dbo.ACCOUNT_VERIFICATION
    WHERE UserId = @UserId AND Channel = @Channel AND Status = 'Issued'
    ORDER BY CreatedAtUtc DESC;
    
    IF @VerId IS NULL OR @Status <> 'Issued' OR @Expires < GETUTCDATE() OR @StoredHash <> @TokenHash
    BEGIN
        IF @VerId IS NOT NULL
        BEGIN
            UPDATE dbo.ACCOUNT_VERIFICATION SET AttemptCount = AttemptCount + 1, UpdatedAtUtc = GETUTCDATE() WHERE Id = @VerId;
        END
        RETURN -1; -- Invalid or expired
    END

    BEGIN TRANSACTION;
    BEGIN TRY
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

CREATE PROCEDURE dbo.PR_IDENTITY_RECORD_LOGIN_ATTEMPT
    @UserId UNIQUEIDENTIFIER,
    @IsSuccess BIT,
    @LockoutMinutes INT = 15,
    @MaxFailures INT = 5,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.LOGIN_ATTEMPT WHERE UserId = @UserId)
    BEGIN
        INSERT INTO dbo.LOGIN_ATTEMPT (UserId, FailedCount, LockoutEndUtc, UpdatedAtUtc)
        VALUES (@UserId, 0, NULL, GETUTCDATE());
    END
    
    IF @IsSuccess = 1
    BEGIN
        UPDATE dbo.LOGIN_ATTEMPT SET FailedCount = 0, LockoutEndUtc = NULL, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
    END
    ELSE
    BEGIN
        DECLARE @NewCount INT;
        SELECT @NewCount = FailedCount + 1 FROM dbo.LOGIN_ATTEMPT WHERE UserId = @UserId;
        DECLARE @LockEnd DATETIME2 = NULL;
        IF @NewCount >= @MaxFailures
        BEGIN
            SET @LockEnd = DATEADD(minute, @LockoutMinutes, GETUTCDATE());
            UPDATE dbo.USER_ACCOUNT SET Status = 'Locked', UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId;
            
            IF @OutboxId IS NOT NULL
            BEGIN
                INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
                VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
            END
        END
        UPDATE dbo.LOGIN_ATTEMPT SET FailedCount = @NewCount, LockoutEndUtc = ISNULL(@LockEnd, LockoutEndUtc), UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
    END
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_SESSION
    @SessionId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenFamilyId VARCHAR(100),
    @DeviceLabel VARCHAR(255),
    @IpAddress VARCHAR(50),
    @RefreshTokenId UNIQUEIDENTIFIER,
    @RefreshTokenHash VARCHAR(255),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.USER_SESSION (Id, UserId, Status, TokenFamilyId, DeviceLabel, IpAddress, CreatedAtUtc, LastActivityAtUtc)
        VALUES (@SessionId, @UserId, 'Active', @TokenFamilyId, @DeviceLabel, @IpAddress, GETUTCDATE(), GETUTCDATE());
        
        INSERT INTO dbo.REFRESH_TOKEN (Id, SessionId, TokenHash, FamilyId, ExpiresAtUtc, IsRevoked, CreatedAtUtc)
        VALUES (@RefreshTokenId, @SessionId, @RefreshTokenHash, @TokenFamilyId, @ExpiresAtUtc, 0, GETUTCDATE());
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_ROTATE_REFRESH_TOKEN
    @OldTokenHash VARCHAR(255),
    @NewTokenId UNIQUEIDENTIFIER,
    @NewTokenHash VARCHAR(255),
    @NewExpiresAtUtc DATETIME2,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SessionId UNIQUEIDENTIFIER;
    DECLARE @FamilyId VARCHAR(100);
    DECLARE @IsRevoked BIT;
    DECLARE @Expires DATETIME2;
    DECLARE @UserId UNIQUEIDENTIFIER;
    
    SELECT TOP 1 @SessionId = r.SessionId, @FamilyId = r.FamilyId, @IsRevoked = r.IsRevoked, @Expires = r.ExpiresAtUtc, @UserId = s.UserId
    FROM dbo.REFRESH_TOKEN r
    INNER JOIN dbo.USER_SESSION s ON r.SessionId = s.Id
    WHERE r.TokenHash = @OldTokenHash;
    
    IF @SessionId IS NULL
    BEGIN
        RETURN -1; -- Not found
    END
    
    IF @IsRevoked = 1 OR @Expires < GETUTCDATE()
    BEGIN
        -- Reuse or expiration detected! Revoke entire token family and session!
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE FamilyId = @FamilyId;
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE Id = @SessionId;
        RETURN -2; -- Revoked due to security violation or expiration
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE(), ReplacedByTokenHash = @NewTokenHash WHERE TokenHash = @OldTokenHash;
        
        INSERT INTO dbo.REFRESH_TOKEN (Id, SessionId, TokenHash, FamilyId, ExpiresAtUtc, IsRevoked, CreatedAtUtc)
        VALUES (@NewTokenId, @SessionId, @NewTokenHash, @FamilyId, @NewExpiresAtUtc, 0, GETUTCDATE());
        
        UPDATE dbo.USER_SESSION SET LastActivityAtUtc = GETUTCDATE() WHERE Id = @SessionId;

        SELECT @UserId AS UserId, @SessionId AS SessionId;

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_REVOKE_SESSION
    @SessionId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.USER_SESSION WHERE Id = @SessionId AND UserId = @UserId)
    BEGIN
        RETURN -1; -- Not owned or not found
    END
    
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE Id = @SessionId;
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE SessionId = @SessionId;
        
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

CREATE PROCEDURE dbo.PR_IDENTITY_REVOKE_ALL_SESSIONS
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND Status = 'Active';
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() 
        WHERE SessionId IN (SELECT Id FROM dbo.USER_SESSION WHERE UserId = @UserId) AND IsRevoked = 0;
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_LIST_SESSIONS
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Id AS SessionId, 
        Status, 
        CreatedAtUtc, 
        RevokedAtUtc, 
        DeviceLabel, 
        IpAddress,
        LastActivityAtUtc
    FROM dbo.USER_SESSION
    WHERE UserId = @UserId
    ORDER BY CreatedAtUtc DESC;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_RECOVERY_REQUEST
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ACCOUNT_RECOVERY SET Status = 'Cancelled' WHERE UserId = @UserId AND Status = 'Created';
    
    INSERT INTO dbo.ACCOUNT_RECOVERY (Id, UserId, TokenHash, Status, AttemptCount, ExpiresAtUtc, CreatedAtUtc)
    VALUES (@Id, @UserId, @TokenHash, 'Created', 0, @ExpiresAtUtc, GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_RESET_PASSWORD
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @NewPasswordHash VARCHAR(255),
    @HashAlgorithm VARCHAR(50),
    @OutboxId UNIQUEIDENTIFIER,
    @OutboxMessageType VARCHAR(255),
    @OutboxPayload NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RecId UNIQUEIDENTIFIER;
    SELECT TOP 1 @RecId = Id FROM dbo.ACCOUNT_RECOVERY 
    WHERE UserId = @UserId AND TokenHash = @TokenHash AND Status = 'Created' AND ExpiresAtUtc > GETUTCDATE();
    
    IF @RecId IS NULL
    BEGIN
        RETURN -1; -- Invalid recovery challenge
    END
    
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.ACCOUNT_RECOVERY SET Status = 'Completed' WHERE Id = @RecId;
        
        INSERT INTO dbo.PASSWORD_HISTORY (UserId, PasswordHash, CreatedAtUtc)
        SELECT UserId, PasswordHash, GETUTCDATE() FROM dbo.USER_CREDENTIAL WHERE UserId = @UserId;
        
        UPDATE dbo.USER_CREDENTIAL SET PasswordHash = @NewPasswordHash, HashAlgorithm = @HashAlgorithm, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
        
        -- Revoke all sessions on password reset
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND Status = 'Active';
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE SessionId IN (SELECT Id FROM dbo.USER_SESSION WHERE UserId = @UserId);
        
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

CREATE PROCEDURE dbo.PR_IDENTITY_CHANGE_PASSWORD
    @UserId UNIQUEIDENTIFIER,
    @OldPasswordHash VARCHAR(255),
    @NewPasswordHash VARCHAR(255),
    @HashAlgorithm VARCHAR(50),
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CurrentHash VARCHAR(255);
    SELECT @CurrentHash = PasswordHash FROM dbo.USER_CREDENTIAL WHERE UserId = @UserId;
    
    IF @CurrentHash <> @OldPasswordHash
    BEGIN
        RETURN -1; -- Incorrect old password
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.PASSWORD_HISTORY (UserId, PasswordHash, CreatedAtUtc)
        VALUES (@UserId, @CurrentHash, GETUTCDATE());
        
        UPDATE dbo.USER_CREDENTIAL SET PasswordHash = @NewPasswordHash, HashAlgorithm = @HashAlgorithm, UpdatedAtUtc = GETUTCDATE() WHERE UserId = @UserId;
        
        -- Revoke sessions except maybe current, or revoke all and re-issue
        UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND Status = 'Active';
        UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE SessionId IN (SELECT Id FROM dbo.USER_SESSION WHERE UserId = @UserId);

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

CREATE PROCEDURE dbo.PR_IDENTITY_GET_PENDING_OUTBOX
    @BatchSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@BatchSize) Id, MessageType, SchemaVersion, Payload, CreatedAtUtc, AttemptCount
    FROM dbo.IDENTITY_OUTBOX
    WHERE IsPublished = 0 AND AttemptCount < 10
    ORDER BY CreatedAtUtc ASC;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDENTITY_OUTBOX SET IsPublished = 1, PublishedAtUtc = GETUTCDATE() WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_MARK_OUTBOX_FAILED
    @Id UNIQUEIDENTIFIER,
    @LastError NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDENTITY_OUTBOX SET AttemptCount = AttemptCount + 1, LastError = @LastError WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_BEGIN_IDEMPOTENT_REQUEST
    @IdempotencyKey VARCHAR(255),
    @Name VARCHAR(255),
    @RequestHash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.IDEMPOTENCY_REQUEST WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT IdempotencyKey, StatusCode, ResponseBody FROM dbo.IDEMPOTENCY_REQUEST WHERE IdempotencyKey = @IdempotencyKey;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.IDEMPOTENCY_REQUEST (IdempotencyKey, Name, RequestHash, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@IdempotencyKey, @Name, @RequestHash, GETUTCDATE(), GETUTCDATE());
    END
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_COMPLETE_IDEMPOTENT_REQUEST
    @IdempotencyKey VARCHAR(255),
    @StatusCode INT,
    @ResponseBody NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.IDEMPOTENCY_REQUEST 
    SET StatusCode = @StatusCode, ResponseBody = @ResponseBody, UpdatedAtUtc = GETUTCDATE()
    WHERE IdempotencyKey = @IdempotencyKey;
END;
GO
