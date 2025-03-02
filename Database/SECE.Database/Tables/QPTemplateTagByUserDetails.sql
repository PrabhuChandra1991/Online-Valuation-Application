CREATE TABLE [dbo].[QPTemplateTagByUserDetails]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[QPTemplateTagId] BIGINT NOT NULL,
	[QPTemplateId] BIGINT NOT NULL,
	[QPTemplateTagValue] NVARCHAR(Max) NOT NULL,
	[UserId] BIGINT NOT NULL,
	[IsActive] BIGINT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedById] BIGINT NULL
)
