CREATE TABLE [dbo].[QPTemplateInstitutionDepartment]
(
	[QPTemplateInstitutionDepartmentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[QPTemplateInstitutionId] INT NOT NULL,
	[DepartmentId] BIGINT NOT NULL,
	[StudentCount] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL DEFAULT 1,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL DEFAULT 1
	
)
