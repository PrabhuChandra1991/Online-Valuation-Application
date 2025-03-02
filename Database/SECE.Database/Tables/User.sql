CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE CHECK (Email LIKE '%@%'),
    MobileNumber NVARCHAR(15) NULL UNIQUE CHECK (MobileNumber LIKE '[0-9]%'),
    RoleId INT NULL,
    Experience INT CHECK (Experience >= 0),
    Mode AS (CASE 
                WHEN Email LIKE '%@skcet.ac.in' THEN 'Internal' 
                ELSE 'External' 
             END) PERSISTED,
    DepartmentId INT ,
    DesignationId INT  NULL,
    CollegeName NVARCHAR(255)  NULL,
    BankAccountName NVARCHAR(255)  NULL,
    BankAccountNumber NVARCHAR(50)  NULL,
    BankName NVARCHAR(255)  NULL,
    BankBranchName NVARCHAR(255)  NULL,
    BankIFSCCode NVARCHAR(20)  NULL,
    IsEnabled BIT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedById NVARCHAR(255)  NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedById NVARCHAR(255)  NULL
);