CREATE TABLE [dbo].[UserAreaOfSpecialization]
(
	[UserAreaOfSpecializationId] BIGINT NOT NULL IDENTITY(1,1)  PRIMARY KEY,
	[UserId] BIGINT NOT NULL FOREIGN KEY (UserId) REFERENCES [User](UserId),
	[AreaOfSpecializationName] NVARCHAR(255) NOT NULL,
	IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT  NULL
)
