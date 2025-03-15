CREATE TABLE [dbo].[UserQPTemplate]
(
	[UserQPTemplateId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL,
	[QPTemplateId] BIGINT NOT NULL FOREIGN KEY (QPTemplateId) REFERENCES [QPTemplate](QPTemplateId),
	[QPTemplateStatusTypeId] BIGINT NOT NULL,
	[IsActive] BIGINT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedById] BIGINT NULL
)
