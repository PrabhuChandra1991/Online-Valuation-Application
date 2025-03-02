CREATE TABLE [dbo].[QPTemplateTagDetails]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[QPTemplateId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[QPTemplate]([Id]),
	[QPTagId] INT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedBy] INT NOT NULL ,
	[CreatedDate] DATETIME NOT NULL,
	[ModifiedBy] INT ,
	[ModifiedDate] DATETIME
)
