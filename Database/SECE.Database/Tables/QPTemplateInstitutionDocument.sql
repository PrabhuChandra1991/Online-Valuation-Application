CREATE TABLE [dbo].[QPTemplateInstitutionDocument]
(
	[QPTemplateInstitutionDocumentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[QPTemplateInstitutionId] INT NOT NULL,
	[QPDocumentTypeId] BIGINT NOT NULL,
	[DocumentId] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL DEFAULT 1,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL DEFAULT 1
)
