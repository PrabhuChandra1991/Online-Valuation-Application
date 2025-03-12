CREATE TABLE [dbo].[CourseDepartment]
(
	[CourseDepartmentId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[InstitutionId] BIGINT NOT NULL,
	[CourseId] BIGINT NOT NULL,
	[DepartmentId] BIGINT NOT NULL,
	[RegulationYear] NVARCHAR(50) NOT NULL,
	[BatchYear] NVARCHAR(50) NOT NULL,
	[DegreeTypeId] BIGINT NOT NULL,
	[ExamType] NVARCHAR(50) NOT NULL,
	[Semester] BIGINT NULL,
	[ExamMonth] NVARCHAR(50) NOT NULL,
	[ExamYear] NVARCHAR(50) NOT NULL,
	[StudentCount] BIGINT  NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL
)
