CREATE TABLE [dbo].[QPTemplate]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(255) NOT NULL,
	[Description] NVARCHAR(255) NOT NULL,
	InstitutionId INT NOT NULL,
	DepartmentId INT NOT NULL,
	CourseId INT NOT NULL,
	QPTemplateDocumentId INT NOT NULL,
	QPTemplateAnswerDocumentId INT NOT NULL ,
	QPTemplateSyalbusDocumentId INT NOT NULL ,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById NVARCHAR(255) NOT NULL,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById NVARCHAR(255) NOT NULL
)
