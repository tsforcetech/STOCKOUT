CREATE PROCEDURE dbo.PR_IDENTITY_CLEANUP_EXPIRED_SECURITY_DATA
    @RetentionHours INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Cutoff DATETIME2 = DATEADD(hour, -@RetentionHours, GETUTCDATE());
    
    -- Cleanup expired verification challenges
    DELETE FROM dbo.ACCOUNT_VERIFICATION WHERE ExpiresAtUtc < @Cutoff OR Status IN ('Verified', 'Cancelled', 'Expired');
    
    -- Cleanup expired recovery tokens
    DELETE FROM dbo.ACCOUNT_RECOVERY WHERE ExpiresAtUtc < @Cutoff OR Status IN ('Completed', 'Cancelled', 'Expired');
    
    -- Cleanup aged revoked refresh tokens and sessions
    DELETE FROM dbo.REFRESH_TOKEN WHERE ExpiresAtUtc < @Cutoff OR RevokedAtUtc < @Cutoff;
    DELETE FROM dbo.USER_SESSION WHERE RevokedAtUtc < @Cutoff;
    
    -- Cleanup published outbox records
    DELETE FROM dbo.IDENTITY_OUTBOX WHERE IsPublished = 1 AND PublishedAtUtc < @Cutoff;
    
    -- Cleanup expired idempotency records
    DELETE FROM dbo.IDEMPOTENCY_REQUEST WHERE CreatedAtUtc < @Cutoff;
END;
GO
