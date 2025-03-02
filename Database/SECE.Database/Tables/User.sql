CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE CHECK (Email LIKE '%@%'),
    MobileNumber NVARCHAR(15) NOT NULL UNIQUE CHECK (MobileNumber LIKE '[0-9]%'),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(Id),
    Experience INT CHECK (Experience >= 0),
    Mode AS (CASE 
                WHEN Email LIKE '%@yourdomain.com' THEN 'Internal' 
                ELSE 'External' 
             END) PERSISTED,
    DepartmentId INT NOT NULL FOREIGN KEY REFERENCES Departments(Id),
    DesignationId INT NOT NULL FOREIGN KEY REFERENCES Designations(Id),
    CollegeName NVARCHAR(255) NOT NULL,
    BankAccountName NVARCHAR(255) NOT NULL,
    BankAccountNumber NVARCHAR(50) NOT NULL,
    BankName NVARCHAR(255) NOT NULL,
    BankBranchName NVARCHAR(255) NOT NULL,
    BankIFSCCode NVARCHAR(20) NOT NULL,
    IsEnabled BIT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy NVARCHAR(255) NOT NULL,
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedBy NVARCHAR(255) NOT NULL
);