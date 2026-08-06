-- Migration 008: Add MFA, Step-up, Service Client credentials/scopes, Security Auditing, and Transaction Hardening (SET XACT_ABORT ON)

ALTER TABLE dbo.USER_ACCOUNT ADD StatusReason NVARCHAR(500) NULL;
ALTER TABLE dbo.USER_ACCOUNT ADD SecurityVersion INT NOT NULL DEFAULT 1;
GO

ALTER TABLE dbo.USER_SESSION ADD SecurityVersion INT NOT NULL DEFAULT 1;
GO

CREATE TABLE dbo.MFA_METHOD (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Type VARCHAR(50) NOT NULL, -- e.g., TOTP
    EncryptedSecret VARCHAR(512) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_MfaMethod_User FOREIGN KEY (UserId) REFERENCES dbo.USER_ACCOUNT(Id) ON DELETE CASCADE
);
CREATE INDEX IX_MFA_METHOD_USER_TYPE ON dbo.MFA_METHOD(UserId, Type);
GO

CREATE TABLE dbo.MFA_RECOVERY_CODE (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CodeHash VARCHAR(255) NOT NULL,
    IsConsumed BIT NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ConsumedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_MfaRecoveryCode_User FOREIGN KEY (UserId) REFERENCES dbo.USER_ACCOUNT(Id) ON DELETE CASCADE
);
CREATE INDEX IX_MFA_RECOVERY_CODE_USER ON dbo.MFA_RECOVERY_CODE(UserId);
GO

CREATE TABLE dbo.STEP_UP_CHALLENGE (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    TokenHash VARCHAR(255) NOT NULL,
    TargetAction VARCHAR(100) NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Issued',
    ExpiresAtUtc DATETIME2 NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_STEP_UP_CHALLENGE_USER ON dbo.STEP_UP_CHALLENGE(UserId, Status);
GO

CREATE TABLE dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ServiceClientId UNIQUEIDENTIFIER NOT NULL,
    KeyId VARCHAR(100) NOT NULL,
    SecretHash VARCHAR(255) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RevokedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_ServiceCred_Client FOREIGN KEY (ServiceClientId) REFERENCES dbo.SERVICE_CLIENT(Id) ON DELETE CASCADE
);
CREATE INDEX IX_SERVICE_CRED_CLIENT ON dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL(ServiceClientId, IsRevoked);
GO

CREATE TABLE dbo.IDENTITY_SERVICE_CLIENT_SCOPE (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ServiceClientId UNIQUEIDENTIFIER NOT NULL,
    Scope VARCHAR(100) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ServiceScope_Client FOREIGN KEY (ServiceClientId) REFERENCES dbo.SERVICE_CLIENT(Id) ON DELETE CASCADE
);
CREATE INDEX IX_SERVICE_SCOPE_CLIENT ON dbo.IDENTITY_SERVICE_CLIENT_SCOPE(ServiceClientId);
GO

CREATE TABLE dbo.SECURITY_EVENT (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    EventType VARCHAR(100) NOT NULL,
    Actor VARCHAR(255) NOT NULL,
    TargetUserId UNIQUEIDENTIFIER NULL,
    Reason NVARCHAR(500) NULL,
    RequestId VARCHAR(100) NULL,
    CorrelationId VARCHAR(100) NULL,
    OccurredAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_SECURITY_EVENT_TARGET ON dbo.SECURITY_EVENT(TargetUserId, OccurredAtUtc);
GO

-- Stored Procedures for new features
CREATE PROCEDURE dbo.PR_IDENTITY_SAVE_MFA_METHOD
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @Type VARCHAR(50),
    @EncryptedSecret VARCHAR(512),
    @IsEnabled BIT,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.MFA_METHOD WHERE UserId = @UserId AND Type = @Type)
        BEGIN
            UPDATE dbo.MFA_METHOD 
            SET EncryptedSecret = @EncryptedSecret, IsEnabled = @IsEnabled, UpdatedAtUtc = GETUTCDATE()
            WHERE UserId = @UserId AND Type = @Type;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.MFA_METHOD (Id, UserId, Type, EncryptedSecret, IsEnabled, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@Id, @UserId, @Type, @EncryptedSecret, @IsEnabled, GETUTCDATE(), GETUTCDATE());
        END

        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_MFA_METHOD
    @UserId UNIQUEIDENTIFIER,
    @Type VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, Type, EncryptedSecret, IsEnabled, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.MFA_METHOD
    WHERE UserId = @UserId AND Type = @Type;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_DELETE_MFA_METHOD
    @UserId UNIQUEIDENTIFIER,
    @Type VARCHAR(50),
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DELETE FROM dbo.MFA_METHOD WHERE UserId = @UserId AND Type = @Type;
        
        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_SAVE_RECOVERY_CODE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @CodeHash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    INSERT INTO dbo.MFA_RECOVERY_CODE (Id, UserId, CodeHash, IsConsumed, CreatedAtUtc)
    VALUES (@Id, @UserId, @CodeHash, 0, GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_RECOVERY_CODES
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, CodeHash, IsConsumed, CreatedAtUtc, ConsumedAtUtc
    FROM dbo.MFA_RECOVERY_CODE
    WHERE UserId = @UserId AND IsConsumed = 0;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CONSUME_RECOVERY_CODE
    @Id UNIQUEIDENTIFIER,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.MFA_RECOVERY_CODE SET IsConsumed = 1, ConsumedAtUtc = GETUTCDATE() WHERE Id = @Id;
        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARCHAR(255),
    @TargetAction VARCHAR(100),
    @ExpiresAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    INSERT INTO dbo.STEP_UP_CHALLENGE (Id, UserId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc)
    VALUES (@Id, @UserId, @TokenHash, @TargetAction, 'Issued', @ExpiresAtUtc, GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, TokenHash, TargetAction, Status, ExpiresAtUtc, CreatedAtUtc
    FROM dbo.STEP_UP_CHALLENGE
    WHERE Id = @Id AND UserId = @UserId;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_UPDATE_STEPUP_CHALLENGE
    @Id UNIQUEIDENTIFIER,
    @Status VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE dbo.STEP_UP_CHALLENGE SET Status = @Status WHERE Id = @Id;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_CREATE_SERVICE_CLIENT_WITH_CREDENTIAL
    @ClientId UNIQUEIDENTIFIER,
    @ClientName VARCHAR(255),
    @CredentialId UNIQUEIDENTIFIER,
    @KeyId VARCHAR(100),
    @SecretHash VARCHAR(255),
    @ExpiresAtUtc DATETIME2,
    @ScopeId UNIQUEIDENTIFIER,
    @Scope VARCHAR(100),
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.SERVICE_CLIENT (Id, ClientId, ClientSecretHash, Status, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@ClientId, @ClientName, @SecretHash, 'Active', GETUTCDATE(), GETUTCDATE());
        
        INSERT INTO dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL (Id, ServiceClientId, KeyId, SecretHash, ExpiresAtUtc, IsRevoked, CreatedAtUtc)
        VALUES (@CredentialId, @ClientId, @KeyId, @SecretHash, @ExpiresAtUtc, 0, GETUTCDATE());
        
        IF @Scope IS NOT NULL AND @Scope <> ''
        BEGIN
            INSERT INTO dbo.IDENTITY_SERVICE_CLIENT_SCOPE (Id, ServiceClientId, Scope, CreatedAtUtc)
            VALUES (ISNULL(@ScopeId, NEWID()), @ClientId, @Scope, GETUTCDATE());
        END

        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_ROTATE_SERVICE_CLIENT_CREDENTIAL
    @CredentialId UNIQUEIDENTIFIER,
    @ServiceClientId UNIQUEIDENTIFIER,
    @KeyId VARCHAR(100),
    @NewSecretHash VARCHAR(255),
    @ExpiresAtUtc DATETIME2,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL (Id, ServiceClientId, KeyId, SecretHash, ExpiresAtUtc, IsRevoked, CreatedAtUtc)
        VALUES (@CredentialId, @ServiceClientId, @KeyId, @NewSecretHash, @ExpiresAtUtc, 0, GETUTCDATE());
        
        UPDATE dbo.SERVICE_CLIENT SET ClientSecretHash = @NewSecretHash, UpdatedAtUtc = GETUTCDATE() WHERE Id = @ServiceClientId;

        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_REVOKE_SERVICE_CLIENT_CREDENTIAL
    @CredentialId UNIQUEIDENTIFIER,
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL 
        SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() 
        WHERE Id = @CredentialId;

        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_SERVICE_CLIENT_CREDENTIAL
    @SecretHash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ServiceClientId, KeyId, SecretHash, ExpiresAtUtc, IsRevoked, CreatedAtUtc, RevokedAtUtc
    FROM dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL
    WHERE SecretHash = @SecretHash AND IsRevoked = 0 AND ExpiresAtUtc > GETUTCDATE();
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_LIST_SERVICE_CLIENT_CREDENTIALS
    @ServiceClientId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ServiceClientId, KeyId, SecretHash, ExpiresAtUtc, IsRevoked, CreatedAtUtc, RevokedAtUtc
    FROM dbo.IDENTITY_SERVICE_CLIENT_CREDENTIAL
    WHERE ServiceClientId = @ServiceClientId;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_GET_SERVICE_CLIENT_SCOPES
    @ServiceClientId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Scope
    FROM dbo.IDENTITY_SERVICE_CLIENT_SCOPE
    WHERE ServiceClientId = @ServiceClientId;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_UPDATE_USER_STATUS
    @UserId UNIQUEIDENTIFIER,
    @Status VARCHAR(50),
    @Reason NVARCHAR(500),
    @Actor VARCHAR(255),
    @OutboxId UNIQUEIDENTIFIER = NULL,
    @OutboxMessageType VARCHAR(255) = NULL,
    @OutboxPayload NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.USER_ACCOUNT 
        SET Status = @Status, StatusReason = @Reason, UpdatedAtUtc = GETUTCDATE()
        WHERE Id = @UserId;

        IF @Status IN ('Locked', 'Suspended', 'Closed')
        BEGIN
            UPDATE dbo.USER_SESSION SET Status = 'Revoked', RevokedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND Status = 'Active';
            UPDATE dbo.REFRESH_TOKEN SET IsRevoked = 1, RevokedAtUtc = GETUTCDATE() WHERE SessionId IN (SELECT Id FROM dbo.USER_SESSION WHERE UserId = @UserId);
        END

        INSERT INTO dbo.SECURITY_EVENT (Id, EventType, Actor, TargetUserId, Reason, OccurredAtUtc)
        VALUES (NEWID(), 'USER_STATUS_CHANGE', @Actor, @UserId, @Reason, GETUTCDATE());

        IF @OutboxId IS NOT NULL
        BEGIN
            INSERT INTO dbo.IDENTITY_OUTBOX (Id, MessageType, SchemaVersion, Payload, IsPublished, CreatedAtUtc, AttemptCount)
            VALUES (@OutboxId, @OutboxMessageType, '1.0.0', @OutboxPayload, 0, GETUTCDATE(), 0);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.PR_IDENTITY_SAVE_SECURITY_EVENT
    @Id UNIQUEIDENTIFIER,
    @EventType VARCHAR(100),
    @Actor VARCHAR(255),
    @TargetUserId UNIQUEIDENTIFIER,
    @Reason NVARCHAR(500),
    @RequestId VARCHAR(100),
    @CorrelationId VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    INSERT INTO dbo.SECURITY_EVENT (Id, EventType, Actor, TargetUserId, Reason, RequestId, CorrelationId, OccurredAtUtc)
    VALUES (@Id, @EventType, @Actor, @TargetUserId, @Reason, @RequestId, @CorrelationId, GETUTCDATE());
END;
GO

-- Harden existing state-changing procedures by altering them to include SET XACT_ABORT ON
ALTER PROCEDURE dbo.PR_IDENTITY_REGISTER_USER
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
    SET XACT_ABORT ON;
    
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
