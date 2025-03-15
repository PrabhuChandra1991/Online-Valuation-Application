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
        public List<QPTemplateDocumentVM> Documents { get; set; }
        public List<QPTemplateInstitutionVM> Institutions { get; set; }
        public long UserId { get; set; }
        public List<QPTemplateDocumentVM> UserDocuments { get; set; }
    }
    public class QPTemplateDocumentVM : AuditModel
    {
        public long QPTemplateDocumentId { get; set; }
        public long QPTemplateId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public string QPDocumentTypeName { get; set; }
        public long DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentUrl { get; set; }
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
}
