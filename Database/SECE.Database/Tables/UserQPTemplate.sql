CREATE TABLE [dbo].[UserQPTemplate]
(
	[UserQPTemplateId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL,
	[QPTemplateInstitutionId] BIGINT NOT NULL FOREIGN KEY (QPTemplateInstitutionId) REFERENCES [QPTemplateInstitution](QPTemplateInstitutionId),
	[QPTemplateStatusTypeId] BIGINT NOT NULL,
	[QPDocumentId] BIGINT NOT NULL FOREIGN KEY (QPDocumentId) REFERENCES [QPDocument](QPDocumentId),
	[IsQPOnly] BIT NOT NULL DEFAULT 0,
	[IsActive] BIT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedById] BIGINT NULL
)
