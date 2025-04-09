CREATE TABLE [dbo].[AnswersheetQuestionMark]
(
	[AnswersheetQuestionMarkId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[AnswersheetId] BIGINT NOT NULL,	 
	[QuestionNumber] NVARCHAR(50) NOT NULL, 
	[MarkFixed] DECIMAL(5,2) NULL,
	[MarkScored] DECIMAL(5,2) NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL
)
