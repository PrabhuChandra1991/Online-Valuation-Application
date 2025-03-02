CREATE TABLE [dbo].[QPTemplateTagDetails]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[QPTemplateId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[QPTemplate]([Id]),
	[QPTagId] INT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedById] INT NOT NULL ,
	[CreatedDate] DATETIME NOT NULL,
	[ModifiedById] INT ,
	[ModifiedDate] DATETIME
)
