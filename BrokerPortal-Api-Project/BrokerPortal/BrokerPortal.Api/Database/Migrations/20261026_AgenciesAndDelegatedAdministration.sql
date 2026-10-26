IF OBJECT_ID(N'dbo.Agencies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Agencies
    (
        AgencyId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Agencies PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        DuckCreekAgencyId NVARCHAR(100) NULL,
        AcceptedDomains NVARCHAR(1000) NOT NULL,
        Products NVARCHAR(1000) NOT NULL,
        MaxActiveUsers INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Agencies_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Agencies_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX IX_Agencies_Name ON dbo.Agencies(Name);
END;
GO

IF COL_LENGTH(N'dbo.PortalUsers', N'AgencyId') IS NULL
    ALTER TABLE dbo.PortalUsers ADD AgencyId INT NULL
        CONSTRAINT FK_PortalUsers_Agencies_AgencyId REFERENCES dbo.Agencies(AgencyId);
GO

IF COL_LENGTH(N'dbo.PortalUsers', N'DeactivatedAt') IS NULL
    ALTER TABLE dbo.PortalUsers ADD DeactivatedAt DATETIME2(0) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PortalUsers_AgencyId')
    CREATE INDEX IX_PortalUsers_AgencyId ON dbo.PortalUsers(AgencyId);
GO

-- Sample Duck Creek agency directory for local development.
IF NOT EXISTS (SELECT 1 FROM dbo.Agencies)
    INSERT INTO dbo.Agencies (Name, DuckCreekAgencyId, AcceptedDomains, Products, MaxActiveUsers)
    VALUES
        (N'Amwins', N'DC-1001', N'amwins.com', N'PROPERTY,CASUALTY,PROFESSIONAL', 2000),
        (N'Acme Brokerage', N'DC-1002', N'acmebrokerage.com', N'PROPERTY,CASUALTY', 250),
        (N'Northwind Insurance Group', N'DC-1003', N'northwindins.com,northwind.co.uk', N'PROFESSIONAL', NULL);
GO
