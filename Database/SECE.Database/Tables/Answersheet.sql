CREATE TABLE [dbo].[Answersheet]
(
	[AnswersheetId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[InstitutionId] BIGINT NOT NULL,
	[CourseId] BIGINT NOT NULL,
	[BatchYear] NVARCHAR(50) NOT NULL,
	[RegulationYear] NVARCHAR(50) NOT NULL,
	[Semester] INT NOT NULL,
	[DegreeTypeId]  INT NOT NULL,
	[ExamType] NVARCHAR(20) NOT NULL,
	[ExamMonth] NVARCHAR(10) NOT NULL,
	[ExamYear] NVARCHAR(10) NOT NULL,
	[DummyNumber] NVARCHAR(100) NOT NULL,
	[UploadedBlobStorageUrl] NVARCHAR(1000) NULL,
	
	[ScriptIdentity] NVARCHAR(50) NULL,	
	[AllocatedToUserId] BIGINT NULL,
	[AllocatedDateTime] DATETIME NULL,
	[IsAllocationMailSent] BIT NULL,
	[IsEvaluateCompleted] BIT NULL,
	[EvaluatedByUserId] BIGINT NULL,
	[EvaluatedDateTime] DATETIME NULL,
	[TotalObtainedMark] DECIMAL(7,2) NOT NULL DEFAULT 0,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL 
)
GO  
ALTER TABLE [dbo].[Answersheet]
ADD CONSTRAINT [UC_Answersheet] UNIQUE([DummyNumber])
GO  