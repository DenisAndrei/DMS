USE DeviceManagementSystem;
GO

-- MERGE lets us run the seed script again without creating duplicate users.
;WITH SeedUsers AS
(
    SELECT *
    FROM (VALUES
        (N'Alexandra Ionescu', N'Junior Software Engineer', N'Bucharest'),
        (N'Matei Popescu', N'QA Engineer', N'Cluj-Napoca'),
        (N'Ioana Georgescu', N'Project Manager', N'Timisoara')
    ) AS Seed(Name, Role, Location)
)
MERGE dbo.Users AS Target
USING SeedUsers AS Source
ON Target.Name = Source.Name
WHEN MATCHED THEN
    UPDATE SET
        Target.Role = Source.Role,
        Target.Location = Source.Location,
        Target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Name, Role, Location)
    VALUES (Source.Name, Source.Role, Source.Location);
GO

-- Look up the user id from the seeded user name before inserting devices.
;WITH SeedDevices AS
(
    SELECT
        Seed.Name,
        Seed.Manufacturer,
        Seed.Type,
        Seed.OperatingSystem,
        Seed.OsVersion,
        Seed.Processor,
        Seed.RamAmountGb,
        Seed.Description,
        Seed.Location,
        Users.Id AS AssignedUserId
    FROM (VALUES
        (N'iPhone 15 Pro', N'Apple', N'phone', N'iOS', N'17.5', N'A17 Pro', 8, N'Primary test device for iOS business scenarios.', N'Bucharest', N'Alexandra Ionescu'),
        (N'Galaxy Tab S9', N'Samsung', N'tablet', N'Android', N'14', N'Snapdragon 8 Gen 2', 12, N'Android tablet used for regression and demo sessions.', N'Cluj-Napoca', N'Matei Popescu'),
        (N'Pixel 8', N'Google', N'phone', N'Android', N'14', N'Google Tensor G3', 8, N'Clean Android device used for feature validation.', N'Cluj-Napoca', NULL),
        (N'iPad Air', N'Apple', N'tablet', N'iPadOS', N'17.4', N'Apple M2', 8, N'Portable tablet used by the project management team.', N'Timisoara', N'Ioana Georgescu')
    ) AS Seed(Name, Manufacturer, Type, OperatingSystem, OsVersion, Processor, RamAmountGb, Description, Location, AssignedUserName)
    LEFT JOIN dbo.Users AS Users
        ON Users.Name = Seed.AssignedUserName
)
MERGE dbo.Devices AS Target
USING SeedDevices AS Source
ON Target.Name = Source.Name
AND Target.Manufacturer = Source.Manufacturer
AND Target.Type = Source.Type
AND Target.OperatingSystem = Source.OperatingSystem
AND Target.OsVersion = Source.OsVersion
WHEN MATCHED THEN
    UPDATE SET
        Target.Processor = Source.Processor,
        Target.RamAmountGb = Source.RamAmountGb,
        Target.Description = Source.Description,
        Target.Location = Source.Location,
        Target.AssignedUserId = Source.AssignedUserId,
        Target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        Name,
        Manufacturer,
        Type,
        OperatingSystem,
        OsVersion,
        Processor,
        RamAmountGb,
        Description,
        Location,
        AssignedUserId
    )
    VALUES
    (
        Source.Name,
        Source.Manufacturer,
        Source.Type,
        Source.OperatingSystem,
        Source.OsVersion,
        Source.Processor,
        Source.RamAmountGb,
        Source.Description,
        Source.Location,
        Source.AssignedUserId
    );
GO

-- Seed one demo account so Phase 3 login has a local user to work with.
;WITH SeedAccounts AS
(
    SELECT
        Users.Id AS UserId,
        N'alexandra.ionescu@darwin.local' AS Email,
        N'vD1uaiSRhBZ9HdXi7dv7XyHf8vWRDov2whFN/Fy9Ak0=' AS PasswordHash,
        N'3o/nKlhshjEhe5//XGIDaA==' AS PasswordSalt
    FROM dbo.Users AS Users
    WHERE Users.Name = N'Alexandra Ionescu'
)
MERGE dbo.Accounts AS Target
USING SeedAccounts AS Source
ON Target.Email = Source.Email
WHEN MATCHED THEN
    UPDATE SET
        Target.UserId = Source.UserId,
        Target.PasswordHash = Source.PasswordHash,
        Target.PasswordSalt = Source.PasswordSalt,
        Target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (UserId, Email, PasswordHash, PasswordSalt)
    VALUES (Source.UserId, Source.Email, Source.PasswordHash, Source.PasswordSalt);
GO
