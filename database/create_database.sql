-- Safe to run more than once.
IF DB_ID(N'DeviceManagementSystem') IS NULL
BEGIN
    CREATE DATABASE DeviceManagementSystem;
END
GO

USE DeviceManagementSystem;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Role NVARCHAR(120) NOT NULL,
        Location NVARCHAR(120) NOT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Users_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.Devices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devices
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Devices PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        Manufacturer NVARCHAR(120) NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        OperatingSystem NVARCHAR(120) NOT NULL,
        OsVersion NVARCHAR(50) NOT NULL,
        Processor NVARCHAR(120) NOT NULL,
        RamAmountGb INT NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        Location NVARCHAR(120) NOT NULL,
        AssignedUserId INT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Devices_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Devices_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Devices_AssignedUser FOREIGN KEY (AssignedUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Devices_Type CHECK (Type IN (N'phone', N'tablet')),
        CONSTRAINT CK_Devices_RamAmountGb CHECK (RamAmountGb > 0)
    );
END
GO

-- Login data is stored separately from the inventory data.
IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
        UserId INT NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        PasswordHash NVARCHAR(128) NOT NULL,
        PasswordSalt NVARCHAR(128) NOT NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Accounts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Accounts_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Accounts_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UX_Accounts_UserId UNIQUE (UserId),
        CONSTRAINT UX_Accounts_Email UNIQUE (Email)
    );
END
GO

-- Keep this index in sync with the duplicate check in the API.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Devices')
      AND name = N'IX_Devices_AssignedUserId'
)
BEGIN
    CREATE INDEX IX_Devices_AssignedUserId ON dbo.Devices (AssignedUserId);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Devices')
      AND name = N'UX_Devices_Name_Manufacturer_Type_OperatingSystem_OsVersion'
)
BEGIN
    CREATE UNIQUE INDEX UX_Devices_Name_Manufacturer_Type_OperatingSystem_OsVersion
        ON dbo.Devices (Name, Manufacturer, Type, OperatingSystem, OsVersion);
END
GO
