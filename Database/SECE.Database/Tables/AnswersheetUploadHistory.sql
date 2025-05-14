CREATE TABLE [dbo].[AnswersheetUploadHistory]
(
	[AnswersheetUploadHistoryId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[CourseCode] NVARCHAR(50) NOT NULL,	 
	[DummyNumber] NVARCHAR(100) NOT NULL,
	[BlobURL] NVARCHAR(1000) NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL
)
GO  
ALTER TABLE [dbo].[AnswersheetUploadHistory]
ADD CONSTRAINT [UC_AnswersheetUploadHistory] UNIQUE([DummyNumber])
GO  