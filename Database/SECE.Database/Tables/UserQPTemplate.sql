CREATE TABLE [dbo].[UserQPTemplate]
(
	[UserQPTemplateId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL,
	[QPTemplateStatusTypeId] BIGINT NOT NULL,
	[QPTemplateId] BIGINT NOT NULL FOREIGN KEY (QPTemplateId) REFERENCES [QPTemplate](QPTemplateId),
	[InstitutionId] BIGINT NOT NULL FOREIGN KEY (InstitutionId) REFERENCES [Institution](InstitutionId),
	[IsQPOnly] BIT NOT NULL DEFAULT 0,
	[QPDocumentId] BIGINT NOT NULL,
	[SubmittedQPDocumentId] BIGINT NULL,
	[ParentUserQPTemplateId] BIGINT NULL,
	[IsGraphsRequired] BIT NULL DEFAULT 0,
	[IsTablesAllowed] BIT NULL DEFAULT 0,
	[GraphName] NVARCHAR(255)  NULL,
	[TableName] NVARCHAR(255)  NULL,
	[QPCode] NVARCHAR(255)  NULL,
	[IsQPSelected] BIT NULL DEFAULT 0,
	[UserQPDocumentId] BIGINT NULL DEFAULT 0,
	[IsActive] BIT NOT NULL,
	[CreatedDate] DATETIME NOT NULL,
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NULL,
	[ModifiedById] BIGINT NULL
)
