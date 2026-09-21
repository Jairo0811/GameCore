IF DB_ID(N'GameCoreDB') IS NULL
BEGIN
    CREATE DATABASE GameCoreDB;
END;
GO

USE GameCoreDB;
GO

CREATE TABLE dbo.Companies (
    CompanyId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Companies PRIMARY KEY,
    Name            nvarchar(120) NOT NULL,
    Phone           varchar(20) NULL,
    Email           varchar(150) NULL,
    CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Companies_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Companies_Name UNIQUE (Name)
);
GO

CREATE TABLE dbo.Branches (
    BranchId        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
    CompanyId       int NOT NULL,
    Name            nvarchar(120) NOT NULL,
    AddressLine     nvarchar(250) NULL,
    IsActive        bit NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT (1),
    CONSTRAINT FK_Branches_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId),
    CONSTRAINT UQ_Branches_Company_Name UNIQUE (CompanyId, Name)
);
GO

CREATE TABLE dbo.JobPositions (
    JobPositionId   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobPositions PRIMARY KEY,
    Name            nvarchar(100) NOT NULL CONSTRAINT UQ_JobPositions_Name UNIQUE
);
GO

CREATE TABLE dbo.Employees (
    EmployeeId      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
    BranchId        int NOT NULL,
    JobPositionId   int NOT NULL,
    FirstName       nvarchar(80) NOT NULL,
    LastName        nvarchar(80) NOT NULL,
    Phone           varchar(20) NULL,
    Email           varchar(150) NULL,
    AddressLine     nvarchar(250) NULL,
    IsActive        bit NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Employees_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId),
    CONSTRAINT FK_Employees_JobPositions FOREIGN KEY (JobPositionId) REFERENCES dbo.JobPositions(JobPositionId),
    CONSTRAINT UQ_Employees_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Customers (
    CustomerId      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FirstName       nvarchar(80) NOT NULL,
    LastName        nvarchar(80) NOT NULL,
    Phone           varchar(20) NULL,
    Email           varchar(150) NULL,
    CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Customers_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.AgeRatings (
    AgeRatingId     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgeRatings PRIMARY KEY,
    Code            varchar(20) NOT NULL,
    Name            nvarchar(100) NOT NULL,
    CONSTRAINT UQ_AgeRatings_Code UNIQUE (Code)
);
GO

CREATE TABLE dbo.Games (
    GameId          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Games PRIMARY KEY,
    AgeRatingId     int NULL,
    Title           nvarchar(180) NOT NULL,
    Story           nvarchar(max) NULL,
    ReleaseDate     date NULL,
    UnitPrice       decimal(12,2) NOT NULL CONSTRAINT CK_Games_UnitPrice CHECK (UnitPrice >= 0),
    IsActive        bit NOT NULL CONSTRAINT DF_Games_IsActive DEFAULT (1),
    CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Games_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Games_AgeRatings FOREIGN KEY (AgeRatingId) REFERENCES dbo.AgeRatings(AgeRatingId),
    CONSTRAINT UQ_Games_Title_ReleaseDate UNIQUE (Title, ReleaseDate)
);
GO

CREATE TABLE dbo.Genres (
    GenreId         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Genres PRIMARY KEY,
    Name            nvarchar(80) NOT NULL CONSTRAINT UQ_Genres_Name UNIQUE
);
GO

CREATE TABLE dbo.GameGenres (
    GameId          int NOT NULL,
    GenreId         int NOT NULL,
    CONSTRAINT PK_GameGenres PRIMARY KEY (GameId, GenreId),
    CONSTRAINT FK_GameGenres_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(GameId),
    CONSTRAINT FK_GameGenres_Genres FOREIGN KEY (GenreId) REFERENCES dbo.Genres(GenreId)
);
GO

CREATE TABLE dbo.Platforms (
    PlatformId      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Platforms PRIMARY KEY,
    Name            nvarchar(80) NOT NULL CONSTRAINT UQ_Platforms_Name UNIQUE
);
GO

CREATE TABLE dbo.GamePlatforms (
    GameId          int NOT NULL,
    PlatformId      int NOT NULL,
    CONSTRAINT PK_GamePlatforms PRIMARY KEY (GameId, PlatformId),
    CONSTRAINT FK_GamePlatforms_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(GameId),
    CONSTRAINT FK_GamePlatforms_Platforms FOREIGN KEY (PlatformId) REFERENCES dbo.Platforms(PlatformId)
);
GO

CREATE TABLE dbo.Countries (
    CountryId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Countries PRIMARY KEY,
    Iso2            char(2) NOT NULL,
    Name            nvarchar(100) NOT NULL,
    CONSTRAINT UQ_Countries_Iso2 UNIQUE (Iso2),
    CONSTRAINT UQ_Countries_Name UNIQUE (Name)
);
GO

CREATE TABLE dbo.Distributions (
    DistributionId  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Distributions PRIMARY KEY,
    GameId          int NOT NULL,
    CountryId       int NOT NULL,
    DistributionDate date NOT NULL,
    Units           int NOT NULL CONSTRAINT CK_Distributions_Units CHECK (Units >= 0),
    CONSTRAINT FK_Distributions_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(GameId),
    CONSTRAINT FK_Distributions_Countries FOREIGN KEY (CountryId) REFERENCES dbo.Countries(CountryId),
    CONSTRAINT UQ_Distributions_Game_Country_Date UNIQUE (GameId, CountryId, DistributionDate)
);
GO

CREATE TABLE dbo.Sales (
    SaleId          bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
    CustomerId      int NOT NULL,
    EmployeeId      int NULL,
    SaleDate        datetime2(0) NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT SYSUTCDATETIME(),
    Status          varchar(20) NOT NULL CONSTRAINT DF_Sales_Status DEFAULT ('Completed'),
    CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Sales_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId),
    CONSTRAINT CK_Sales_Status CHECK (Status IN ('Pending','Completed','Cancelled'))
);
GO

CREATE TABLE dbo.SaleDetails (
    SaleDetailId    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleDetails PRIMARY KEY,
    SaleId          bigint NOT NULL,
    GameId          int NOT NULL,
    Quantity        int NOT NULL,
    UnitPrice       decimal(12,2) NOT NULL,
    CONSTRAINT FK_SaleDetails_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId),
    CONSTRAINT FK_SaleDetails_Games FOREIGN KEY (GameId) REFERENCES dbo.Games(GameId),
    CONSTRAINT CK_SaleDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_SaleDetails_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT UQ_SaleDetails_Sale_Game UNIQUE (SaleId, GameId)
);
GO

CREATE INDEX IX_Employees_BranchId ON dbo.Employees(BranchId);
CREATE INDEX IX_Games_AgeRatingId ON dbo.Games(AgeRatingId);
CREATE INDEX IX_Sales_CustomerId_SaleDate ON dbo.Sales(CustomerId, SaleDate DESC);
CREATE INDEX IX_SaleDetails_GameId ON dbo.SaleDetails(GameId);
CREATE INDEX IX_Distributions_CountryId ON dbo.Distributions(CountryId);
GO
