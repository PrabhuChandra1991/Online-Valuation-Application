CREATE TABLE [dbo].[UserQPDocumentBookMark]
(
	[UserQPDocumentBookMarkId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserQPTemplateDocumentId] BIGINT NOT NULL FOREIGN KEY (UserQPTemplateDocumentId) REFERENCES [UserQPTemplateDocument](UserQPTemplateDocumentId),
	[BookMarkName] NVARCHAR(50) NOT NULL,
	[BookMarkText] NVARCHAR(MAX) NOT NULL,
	[IsActive] BIT DEFAULT 1,
	CreatedDate DATETIME DEFAULT GETDATE(),
	CreatedById BIGINT NOT NULL DEFAULT 1,
	ModifiedDate DATETIME DEFAULT GETDATE(),
	ModifiedById BIGINT NOT NULL DEFAULT 1
)
