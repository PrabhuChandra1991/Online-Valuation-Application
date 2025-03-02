CREATE TABLE [dbo].[QPTemplateTagDetails]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY,
	[QPTemplateId] BIGINT NOT NULL,
	[QPTagId] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedById] BIGINT NOT NULL ,
	[CreatedDate] DATETIME NOT NULL,
	[ModifiedById] BIGINT ,
	[ModifiedDate] DATETIME
)
