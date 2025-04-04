using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.ViewModels.QPSettings
{
    public class QPTemplateVM:AuditModel
    {
        public long QPTemplateId { get; set; }
        public string QPTemplateName { get; set; }
        public string QPCode { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        public string QPTemplateStatusTypeName { get; set; }
        public long CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public long DegreeTypeId { get; set; }
        public string DegreeTypeName { get; set; }
        public string RegulationYear { get; set; }
        public string BatchYear { get; set; }
        public string ExamYear { get; set; }
        public string ExamMonth { get; set; }
        public string ExamType { get; set; }
        public long Semester { get; set; }
        public long StudentCount { get; set; }
        public long? CourseSyllabusDocumentId { get; set; }
        public string? CourseSyllabusDocumentName { get; set; }
        public string? CourseSyllabusDocumentUrl { get; set; }
        public string? Expert1Name { get; set; }
        public string? Expert1Status { get; set; }
        public string? Expert2Name { get; set; }
        public string? Expert2Status { get; set; }
        public List<QPDocumentVM> QPDocuments { get; set; } = new List<QPDocumentVM>();
    }
    public class QPDocumentVM
    {
        public long QPDocumentId { get; set; }
        public string QPDocumentName { get; set; }
        public long InstitutionId { get; set; }
        public long QPOnlyDocumentId { get; set; }
        public List<QPDocumentUserVM> QPAssignedUsers { get; set; } = new List<QPDocumentUserVM>();
        public List<QPDocumentUserVM> QPScrutinityUsers { get; set; } = new List<QPDocumentUserVM>();
        public List<QPDocumentUserVM> QPSelectedUsers { get; set; } = new List<QPDocumentUserVM>();
    }
    public class QPDocumentUserVM
    {
        public long UserQPTemplateId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long StatusTypeId { get; set; }
        public string StatusTypeName { get; set; } = string.Empty;
        public bool IsQPOnly { get; set; }
        public long InstitutionId { get; set; }
        public long QPTemplateId { get; set; }
        public long SubmittedQPDocumentId { get; set; }
        public string? SubmittedQPDocumentName { get; set; }
        public string? SubmittedQPDocumentUrl { get; set; }
        public long? ParentUserQPTemplateId { get; set; }
    }
    public class QPTemplateDocumentVM : AuditModel
    {
        public long QPTemplateDocumentId { get; set; }
        public long QPTemplateId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public string QPDocumentTypeName { get; set; } = string.Empty;
        public long DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
    }
    public class QPTemplateInstitutionVM : AuditModel
    {
        public long QPTemplateInstitutionId { get; set; }
        public long QPTemplateId { get; set; }
        public long InstitutionId { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionCode { get; set; }
        public long StudentCount { get; set; }
        public List<QPTemplateInstitutionDepartmentVM> Departments { get; set; }
        public List<QPTemplateInstitutionDocumentVM> Documents { get; set; }
        public List<UserQPTemplateVM> UserQPGenerateTemplates { get; set; } = new List<UserQPTemplateVM>();
        public List<UserQPTemplateVM> UserQPScrutinyTemplates { get; set; } = new List<UserQPTemplateVM>();
        public List<UserQPTemplateVM> UserQPSelectionTemplates { get; set; } = new List<UserQPTemplateVM>();
    }
    public class QPTemplateInstitutionDepartmentVM : AuditModel
    {
        public long QPTemplateInstitutionDepartmentId { get; set; }
        public long QPTemplateInstitutionId { get; set; }
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentShortName { get; set; }
        public long StudentCount { get; set; }
    }
    public class QPTemplateInstitutionDocumentVM : AuditModel
    {
        public long QPTemplateInstitutionDocumentId { get; set; }
        public long QPTemplateInstitutionId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public string QPDocumentTypeName { get; set; }
        public long DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentUrl { get; set; }
    }
    public class UserQPTemplateVM : AuditModel
    {
        public string QPTemplateCurrentStateHeader { get; set; } = string.Empty;
        public long UserQPTemplateId { get; set; } 
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public long QPTemplateInstitutionId { get; set; }
        public string QPTemplateName { get; set; } = string.Empty;
        public string QPTemplateCourseName { get; set; } = string.Empty;
        public string QPTemplateCourseCode { get; set; } = string.Empty;
        public string QPTemplateExamYear { get; set; } = string.Empty;
        public string QPTemplateExamMonth { get; set; } = string.Empty;
        public long QPTemplateStatusTypeId { get; set; }    
        public string QPTemplateStatusTypeName { get; set; } = string.Empty;
        public long QPDocumentId { get; set; }
        public bool IsQPOnly { get; set; }
        public long QPTemplateId { get; set; }
        public long InstitutionId { get; set; }
        public string QPDocumentTypeName { get; set; }
        public string QPDocumentName { get; set; }
        public string QPDocumentUrl { get; set; }
        public long CourseSyllabusDocumentId { get; set; }
        public string CourseSyllabusDocumentName { get; set; }
        public string CourseSyllabusDocumentUrl { get; set; }
        public long  SubmittedQPDocumentId { get; set; }
        public string SubmittedQPDocumentName { get; set; }
        public string SubmittedQPDocumentUrl { get; set; }
        public  long? ParentUserQPTemplateId { get; set; }
    }
    public class QPDocumentValidationVM
    {
        
    }
    public class QPAssignmentExpertVM
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsAvailableForQPAssignment { get; set; }
    }
}
