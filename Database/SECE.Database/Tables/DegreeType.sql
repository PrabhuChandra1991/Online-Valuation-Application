﻿CREATE TABLE [dbo].[DegreeType]
(
	[DegreeTypeId] INT NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(100) NULL,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT NOT NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT NOT NULL
)
