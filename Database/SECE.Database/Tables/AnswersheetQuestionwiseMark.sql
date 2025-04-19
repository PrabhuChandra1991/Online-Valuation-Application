CREATE TABLE [dbo].[AnswersheetQuestionwiseMark]
(
	[AnswersheetQuestionwiseMarkId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[AnswersheetId] BIGINT NOT NULL,
	[QuestionNumber] INT NOT NULL,
	[QuestionNumberSubNum] INT NOT NULL,
	[QuestionPartName] NVARCHAR(10) NOT NULL,
	[QuestionGroupName] NVARCHAR(10) NOT NULL, 
	[MaximumMark] DECIMAL(5,2) NOT NULL,
	[ObtainedMark] DECIMAL(5,2) NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL, 
	CONSTRAINT [FK_Answersheet] FOREIGN KEY ([AnswersheetId]) REFERENCES [dbo].[Answersheet]([AnswersheetId])
)
GO
ALTER TABLE [dbo].[AnswersheetQuestionwiseMark]
ADD CONSTRAINT [UC_AnswersheetQuestionwiseMark] UNIQUE([AnswersheetId],[QuestionNumber],[QuestionNumberSubNum])
GO  