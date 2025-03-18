CREATE TABLE [dbo].[ImportHistory]
(
	[ImportHistoryId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[DocumentId] BIGINT NOT NULL,
	[UserId] BIGINT NOT NULL,
	[TotalCount] BIGINT NOT NULL,
	[CoursesCount] BIGINT NOT NULL,
	[InstitutionsCount] BIGINT NOT NULL,
	[DepartmentsCount] BIGINT NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL
)
