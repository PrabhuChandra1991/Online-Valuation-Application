﻿CREATE TABLE [dbo].[QPTemplate]
(
	[QPTemplateId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(255) NOT NULL,
	[Description] NVARCHAR(255) NOT NULL,
	[InstitutionId] BIGINT NOT NULL,
	[DepartmentId] BIGINT NOT NULL,
	[CourseId] BIGINT NOT NULL,
	[QPCode] NVARCHAR(255) NOT NULL,
	[QPTemplateStatusTypeId] BIGINT NOT NULL,
	[QPTemplateSyallbusDocumentId] BIGINT NOT NULL ,
	[QPDocumentId] BIGINT NOT NULL,
	[QPAnswerDocumentId] BIGINT NOT NULL ,
	[QPPrintDocumentId] BIGINT NOT NULL,
	[QPAnswerPrintDocumentId] BIGINT NOT NULL ,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL
)
