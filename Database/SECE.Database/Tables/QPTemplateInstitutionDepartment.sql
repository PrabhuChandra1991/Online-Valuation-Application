CREATE TABLE [dbo].[QPTemplateInstitutionDepartment]
(
	[QPTemplateInstitutionDepartmentId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[QPTemplateInstitutionId] BIGINT NOT NULL FOREIGN KEY (QPTemplateInstitutionId) REFERENCES [QPTemplateInstitution](QPTemplateInstitutionId),
	[DepartmentId] BIGINT NOT NULL,
	[StudentCount] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL DEFAULT 1,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL DEFAULT 1
	
)
