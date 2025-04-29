CREATE TABLE [dbo].[AnswersheetImportDetail]
(
	[AnswersheetImportDetailId] BIGINT NOT NULL IDENTITY(1,1),
	[AnswersheetImportId] BIGINT NOT NULL,
	[InstitutionCode] NVARCHAR(10) NOT NULL,
	[RegulationYear] NVARCHAR(5) NOT NULL,
	[BatchYear] NVARCHAR(5) NOT NULL,
	[DegreeType] NVARCHAR(10) NOT NULL,
	[ExamType] NVARCHAR(20) NOT NULL,
	[CourseCode] NVARCHAR(20) NOT NULL,
	[ExamYear] NVARCHAR(5) NOT NULL,
	[ExamMonth] NVARCHAR(20) NOT NULL,
	[DummyNumber] NVARCHAR(50) NOT NULL,	
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL,
	CONSTRAINT [PK_AnswersheetImportDetail] PRIMARY KEY (AnswersheetImportDetailId),
	CONSTRAINT [FK_AnswersheetImportDetail_AnswersheetImport] FOREIGN KEY ([AnswersheetImportId]) REFERENCES [dbo].[AnswersheetImport]([AnswersheetImportId])
)
