CREATE TABLE [dbo].[UserQualification]
(
	[UserQualificationId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL FOREIGN KEY (UserId) REFERENCES [User](UserId),
	[Title] NVARCHAR(100) NOT NULL,
	[Name] NVARCHAR(100) NOT NULL,
	[Specialization] NVARCHAR(100) NOT NULL,
	[IsCompleted] BIT  NULL,
	IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT  NULL
)
