CREATE TABLE [dbo].[AnswersheetImport]
(
	[AnswersheetImportId] BIGINT NOT NULL IDENTITY(1,1),
	[DocumentName] NVARCHAR(100) NOT NULL,
	[DocumentUrl] NVARCHAR(1000) NOT NULL,
	[InstitutionId] BIGINT NOT NULL,
	[ExamYear] NVARCHAR(5) NOT NULL,
	[ExamMonth] NVARCHAR(20) NOT NULL,
	[ExaminationId] BIGINT NOT NULL,	 
	[IsReviewCompleted] BIT DEFAULT 0 NULL,
	[ReviewCompletedOn] DATETIME NULL,
	[ReviewCompletedBy] BIGINT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL,
	CONSTRAINT [PK_AnswersheetImport] PRIMARY KEY (AnswersheetImportId),
	CONSTRAINT [FK_AnswersheetImport_Examination] FOREIGN KEY ([ExaminationId]) REFERENCES [dbo].[Examination]([ExaminationId])
)
