CREATE TABLE [dbo].[UserDesignation]
(
	[UserDesignationId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[DesignationId] BIGINT NOT NULL,
	[UserId] BIGINT NOT NULL FOREIGN KEY (UserId) REFERENCES [User](UserId),
	[Experience] BIGINT NOT NULL,
	[CollegeName] NVARCHAR(255) NULL,
	[Department] NVARCHAR(255) NULL,
	[IsCurrent] BIT  NULL,
	IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT  NULL
)
