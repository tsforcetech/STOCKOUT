CREATE PROCEDURE dbo.PR_ORGANIZATION_CREATE
    @Id UNIQUEIDENTIFIER,
    @EntityType INT,
    @DisplayName VARCHAR(255),
    @LegalName VARCHAR(255),
    @Status INT,
    @OwnerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ORGANIZATION (Id, EntityType, DisplayName, LegalName, Status, OwnerUserId, CreatedAtUtc)
    VALUES (@Id, @EntityType, @DisplayName, @LegalName, @Status, @OwnerUserId, GETUTCDATE());
END;
GO

CREATE PROCEDURE dbo.PR_ORGANIZATION_ASSIGN_CAPABILITY
    @OrganizationId UNIQUEIDENTIFIER,
    @CapabilityCode INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.ORGANIZATION_CAPABILITY WHERE OrganizationId = @OrganizationId AND CapabilityCode = @CapabilityCode)
    BEGIN
        INSERT INTO dbo.ORGANIZATION_CAPABILITY (OrganizationId, CapabilityCode, CreatedAtUtc)
        VALUES (@OrganizationId, @CapabilityCode, GETUTCDATE());
    END
END;
GO

CREATE PROCEDURE dbo.PR_ORGANIZATION_GET_BY_ID
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Id,
        EntityType,
        DisplayName,
        LegalName,
        Status,
        OwnerUserId,
        CreatedAtUtc,
        UpdatedAtUtc
    FROM dbo.ORGANIZATION
    WHERE Id = @Id;

    SELECT 
        OrganizationId,
        CapabilityCode,
        CreatedAtUtc
    FROM dbo.ORGANIZATION_CAPABILITY
    WHERE OrganizationId = @Id;
END;
GO
