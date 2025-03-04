﻿CREATE TABLE Department (
    DepartmentId BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [ShortName] NVARCHAR(100) NULL,
    [DegreeTypeId] NVARCHAR(100) NULL,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT NOT NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT NOT NULL
);
