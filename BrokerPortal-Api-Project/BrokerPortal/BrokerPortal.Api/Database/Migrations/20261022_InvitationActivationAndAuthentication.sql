IF COL_LENGTH(N'dbo.PortalUsers', N'PasswordHash') IS NULL
    ALTER TABLE dbo.PortalUsers ADD PasswordHash NVARCHAR(512) NULL;
GO

IF COL_LENGTH(N'dbo.PortalUsers', N'EmailVerifiedAt') IS NULL
    ALTER TABLE dbo.PortalUsers ADD EmailVerifiedAt DATETIME2(0) NULL;
GO

IF COL_LENGTH(N'dbo.PortalUsers', N'ActivatedAt') IS NULL
    ALTER TABLE dbo.PortalUsers ADD ActivatedAt DATETIME2(0) NULL;
GO

IF OBJECT_ID(N'dbo.UserInvitations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserInvitations
    (
        InvitationId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_UserInvitations PRIMARY KEY
            CONSTRAINT DF_UserInvitations_InvitationId DEFAULT NEWSEQUENTIALID(),
        UserId INT NOT NULL,
        TokenHash VARCHAR(64) NOT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL,
        ExpiresAtUtc DATETIME2(0) NOT NULL,
        AcceptedAtUtc DATETIME2(0) NULL,
        RevokedAtUtc DATETIME2(0) NULL,
        InvitedByUserId INT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_UserInvitations_PortalUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.PortalUsers(UserId),
        CONSTRAINT FK_UserInvitations_PortalUsers_InvitedByUserId
            FOREIGN KEY (InvitedByUserId) REFERENCES dbo.PortalUsers(UserId)
    );

    CREATE UNIQUE INDEX UX_UserInvitations_TokenHash
        ON dbo.UserInvitations(TokenHash);
    CREATE INDEX IX_UserInvitations_UserId_CreatedAtUtc
        ON dbo.UserInvitations(UserId, CreatedAtUtc);
END;
GO
