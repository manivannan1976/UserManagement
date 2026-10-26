-- Local stand-in for Duck Creek User Admin until a Duck Creek environment is available.
IF OBJECT_ID(N'dbo.DuckCreekUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DuckCreekUsers
    (
        DuckCreekUserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DuckCreekUsers PRIMARY KEY,
        UserName NVARCHAR(256) NOT NULL,
        FullName NVARCHAR(256) NOT NULL,
        PasswordHash NVARCHAR(512) NOT NULL,
        EntityId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DuckCreekUsers_IsActive DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DuckCreekUsers_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX IX_DuckCreekUsers_UserName ON dbo.DuckCreekUsers(UserName);
END;
GO

IF COL_LENGTH(N'dbo.PortalUsers', N'DuckCreekUserName') IS NULL
    ALTER TABLE dbo.PortalUsers ADD DuckCreekUserName NVARCHAR(256) NULL;
GO

IF COL_LENGTH(N'dbo.Agencies', N'DuckCreekEntityId') IS NULL
    ALTER TABLE dbo.Agencies ADD DuckCreekEntityId INT NULL;
GO

-- Sample entity ids for local development. Amwins US is 10021 in the Duck Creek collection;
-- the others are placeholders to replace with real entity ids.
UPDATE dbo.Agencies SET DuckCreekEntityId = 10021 WHERE Name = N'Amwins' AND DuckCreekEntityId IS NULL;
UPDATE dbo.Agencies SET DuckCreekEntityId = 10022 WHERE Name = N'Acme Brokerage' AND DuckCreekEntityId IS NULL;
UPDATE dbo.Agencies SET DuckCreekEntityId = 10023 WHERE Name = N'Northwind Insurance Group' AND DuckCreekEntityId IS NULL;
GO
