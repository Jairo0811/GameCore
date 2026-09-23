USE GameCoreDB;
GO

CREATE TABLE dbo.AppUsers (
    UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
    Email varchar(150) NOT NULL,
    PasswordHash varchar(250) NOT NULL,
    Role varchar(40) NOT NULL CONSTRAINT DF_AppUsers_Role DEFAULT ('Manager'),
    IsActive bit NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT (1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_AppUsers_Email UNIQUE (Email),
    CONSTRAINT CK_AppUsers_Role CHECK (Role IN ('Admin','Manager','Viewer'))
);
GO

-- Local demo only: admin@gamecore.local / GameCore123!
INSERT INTO dbo.AppUsers (Email, PasswordHash, Role)
VALUES ('admin@gamecore.local','100000.pcnAsewbMeQ9i+iZnkMy2A==.pcnTHGRSHKHoYsfBv0CFvgZVnJr2+lwj1cPG8ooeS+k=','Admin');
GO
