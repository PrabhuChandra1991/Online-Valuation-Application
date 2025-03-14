﻿CREATE TABLE [dbo].[UserLoginHistory]
(
	[UserLoginHistoryId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] BIGINT NOT NULL,
	[LoginDateTime] DATETIME NOT NULL,
	[Email] NVARCHAR(50) NOT NULL,
	[TempPassword] NVARCHAR(50) NULL,
	[IsSuccessful] BIT NOT NULL DEFAULT 0,
	[IsActive] BIT NOT NULL DEFAULT 1,
	[CreatedById] BIGINT NOT NULL,
	[CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
	[ModifiedById] BIGINT NULL,
	[ModifiedDate] DATETIME NULL,

)
