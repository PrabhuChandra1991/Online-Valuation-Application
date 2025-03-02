CREATE TABLE [dbo].[QPTemplateTagByUserDetails]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[QPTemplateTagId] INT NOT NULL,
	[QPTemplateId] INT NOT NULL,
	[QPTemplateTagValue] NVARCHAR(Max) NOT NULL,
	[UserId] INT NOT NULL,
	[IsActive] BIT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedBy] INT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedBy] INT NULL
)
