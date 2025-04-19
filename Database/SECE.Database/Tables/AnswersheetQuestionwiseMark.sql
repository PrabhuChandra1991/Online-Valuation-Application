CREATE TABLE [dbo].[AnswersheetQuestionwiseMark]
(
	[AnswersheetQuestionwiseMarkId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[AnswersheetId] BIGINT NOT NULL,
	[QuestionNumber] INT NOT NULL,
	[QuestionNumberSubNum] INT NOT NULL,  
	[MaximumMark] DECIMAL(5,2) NULL,
	[ObtainedMark] DECIMAL(5,2) NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL
)
GO
ALTER TABLE [dbo].[AnswersheetQuestionwiseMark]
ADD CONSTRAINT [UC_AnswersheetQuestionwiseMark] UNIQUE([AnswersheetId],[QuestionNumber],[QuestionNumberSubNum])
GO  