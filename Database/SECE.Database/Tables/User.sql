﻿CREATE TABLE [User] (
    UserId BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(255) NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE CHECK (Email LIKE '%@%'),
    MobileNumber NVARCHAR(15) NULL UNIQUE CHECK (MobileNumber LIKE '[0-9]%'),
    RoleId BIGINT NULL,
    [Qualification] NVARCHAR(255) NULL,
    [WorkExperience] BIGINT CHECK (WorkExperience >= 0),
    Mode AS (CASE 
                WHEN Email LIKE '%@skcet.ac.in' THEN 'Internal' 
                ELSE 'External' 
             END) PERSISTED,
    DepartmentId BIGINT NOT NULL ,
    DesignationId BIGINT NOT NULL,
    CollegeName NVARCHAR(255)  NULL,
    BankAccountName NVARCHAR(255)  NULL,
    BankAccountNumber NVARCHAR(50)  NULL,
    BankName NVARCHAR(255)  NULL,
    BankBranchName NVARCHAR(255)  NULL,
    BankIFSCCode NVARCHAR(20)  NULL,
    IsEnabled BIT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById BIGINT  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById BIGINT  NULL
);