CREATE TABLE [dbo].[UserQPTemplate]
(
	[UserQPTemplateId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL,
	[QPTemplateInstitutionId] BIGINT NOT NULL FOREIGN KEY (QPTemplateInstitutionId) REFERENCES [QPTemplateInstitution](QPTemplateInstitutionId),
	[QPTemplateStatusTypeId] BIGINT NOT NULL,
	[QPDocumentId] BIGINT NOT NULL FOREIGN KEY (QPDocumentId) REFERENCES [QPDocument](QPDocumentId),
	[IsActive] BIT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedById] BIGINT NULL
)
