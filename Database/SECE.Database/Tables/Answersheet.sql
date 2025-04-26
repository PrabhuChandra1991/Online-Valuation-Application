CREATE TABLE [dbo].[Answersheet]
(
	[AnswersheetId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[ExaminationId] BIGINT NOT NULL,	 
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
	[ModifiedById] BIGINT NOT NULL,
	CONSTRAINT [FK_Answersheet_Examination] FOREIGN KEY ([ExaminationId]) REFERENCES [dbo].[Examination]([ExaminationId])
)
GO  
ALTER TABLE [dbo].[Answersheet]
ADD CONSTRAINT [UC_Answersheet] UNIQUE([DummyNumber])
GO  