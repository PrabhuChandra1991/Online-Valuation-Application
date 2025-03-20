CREATE TABLE [dbo].[CourseSyllabusDocument]
(
	[CourseSyllabusDocumentId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[CourseId] BIGINT NOT NULL,
	[DocumentId] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL
)
