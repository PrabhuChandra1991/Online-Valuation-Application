CREATE TABLE [dbo].[AnswersheetImportHistory]
(
	[AnswersheetImportHistoryId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(255) NOT NULL,
	[Url] NVARCHAR(Max) NOT NULL,
	[IsActive] BIT DEFAULT 1,
	[CreatedDate] DATETIME DEFAULT GETDATE(),
	[CreatedById] BIGINT NOT NULL,
	[ModifiedDate] DATETIME DEFAULT GETDATE(),
	[ModifiedById] BIGINT NOT NULL
)
