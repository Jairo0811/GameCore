USE GameCoreDB;
GO

-- Modern seed data based on the legacy project.
-- Personal identifiers from the original academic submission are intentionally not reused.

INSERT INTO dbo.Companies (Name, Phone, Email)
VALUES (N'GameCore Demo Studios', '809-555-0100', 'contact@gamecore.local');

INSERT INTO dbo.Branches (CompanyId, Name, AddressLine)
VALUES (1, N'Santo Domingo', N'Santo Domingo, República Dominicana');

INSERT INTO dbo.JobPositions (Name)
VALUES (N'Programador'), (N'Diseñador Gráfico'), (N'Recursos Humanos'), (N'Relaciones Internacionales');

INSERT INTO dbo.Employees (BranchId, JobPositionId, FirstName, LastName, Phone, Email)
VALUES
(1, 1, N'Jairo', N'Matías', '809-555-0101', 'jairo.demo@gamecore.local'),
(1, 2, N'Ismael', N'Paredes', '809-555-0102', 'ismael.demo@gamecore.local'),
(1, 3, N'Luis', N'Núñez', '809-555-0103', 'luis.demo@gamecore.local'),
(1, 4, N'Francis', N'Rosario', '809-555-0104', 'francis.demo@gamecore.local');

INSERT INTO dbo.Customers (FirstName, LastName, Phone, Email)
VALUES
(N'Ana', N'Torres', '809-555-0201', 'ana@example.test'),
(N'Carlos', N'Méndez', '809-555-0202', 'carlos@example.test'),
(N'Laura', N'Castillo', '809-555-0203', 'laura@example.test'),
(N'Miguel', N'Santos', '809-555-0204', 'miguel@example.test');

INSERT INTO dbo.AgeRatings (Code, Name)
VALUES ('E', N'Para Todos'), ('T', N'Adolescentes'), ('M', N'Mayores de 17');

INSERT INTO dbo.Genres (Name)
VALUES (N'Peleas'), (N'Aventura'), (N'RPG'), (N'Tiros'), (N'Mundo Abierto'), (N'Carreras');

INSERT INTO dbo.Platforms (Name)
VALUES (N'Nintendo 3DS'), (N'Wii U'), (N'PlayStation 4'), (N'Xbox One'), (N'PC');

-- The five game titles from the original SQL are retained.
INSERT INTO dbo.Games (AgeRatingId, Title, ReleaseDate, UnitPrice)
VALUES
(2, N'Super Smash Bros. for Nintendo 3DS', '2014-10-03', 39.99),
(1, N'Pokémon Sun', '2016-11-18', 39.99),
(3, N'Call of Duty: Black Ops III', '2015-11-06', 59.99),
(3, N'Grand Theft Auto V', '2013-09-17', 59.99),
(2, N'Need for Speed', '2015-11-03', 59.99);

INSERT INTO dbo.GameGenres (GameId, GenreId)
VALUES (1,1),(2,2),(2,3),(3,4),(4,5),(5,6);

INSERT INTO dbo.GamePlatforms (GameId, PlatformId)
VALUES
(1,1),(1,2),
(2,1),
(3,3),(3,4),(3,5),
(4,3),(4,4),(4,5),
(5,3),(5,4),(5,5);

INSERT INTO dbo.Countries (Iso2, Name)
VALUES ('DO', N'República Dominicana'), ('US', N'Estados Unidos'), ('MX', N'México'), ('ES', N'España');

INSERT INTO dbo.Distributions (GameId, CountryId, DistributionDate, Units)
VALUES
(1,1,'2014-10-03',100),
(2,1,'2016-11-18',120),
(3,2,'2015-11-06',500),
(4,3,'2013-09-17',300),
(5,4,'2015-11-03',250);

INSERT INTO dbo.Sales (CustomerId, EmployeeId, SaleDate, Status)
VALUES
(1,1,'2014-10-05','Completed'),
(2,2,'2016-11-20','Completed'),
(3,3,'2015-11-08','Completed'),
(4,4,'2015-11-05','Completed');

INSERT INTO dbo.SaleDetails (SaleId, GameId, Quantity, UnitPrice)
VALUES
(1,1,1,39.99),
(2,2,1,39.99),
(3,3,1,59.99),
(4,5,1,59.99);
GO
