CREATE TABLE [dbo].[UserQPTemplateTag]
(
	[UserQPTemplateTagId] BIGINT NOT NULL PRIMARY KEY IDENTITY,
	[UserQPTemplateId] BIGINT NOT NULL,
	[QPTagId] BIGINT NOT NULL,
	[QPTagValue] NVARCHAR(MAX) NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedById] BIGINT NOT NULL ,
	[CreatedDate] DATETIME NOT NULL,
	[ModifiedById] BIGINT ,
	[ModifiedDate] DATETIME
)