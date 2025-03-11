CREATE TABLE [dbo].[UserCourse]
(
	[UserCourseId] BIGINT NOT NULL IDENTITY(1,1)  PRIMARY KEY,
	[UserId] BIGINT NOT NULL FOREIGN KEY (UserId) REFERENCES [User](UserId),
	[CourseName] NVARCHAR(255) NOT NULL,
	[NumberOfYearsHandled] BIGINT CHECK (NumberOfYearsHandled >= 0),
	IsHandledInLast2Semester BIT NOT NULL DEFAULT 0,
	[DegreeTypeId] BIGINT NOT NULL FOREIGN KEY (DegreeTypeId) REFERENCES [DegreeType](DegreeTypeId),
	IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT  NULL
)
