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
        public List<QPDocumentVM> qpDocumentVMs { get; set; }
        public List<InstitutionDepartmentVM> Institutions { get; set; }
        public long UserId { get; set; }
        public List<QPDocumentVM> qpUserDocumentVMs { get; set; }
    }
    public class InstitutionDepartmentVM: AuditModel
    {
        public long InstitutionId { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionCode { get; set; }
        public long StudentCount { get; set; }
        public List<DepartmentVM> Departments { get; set; }
        public List<QPDocumentVM> qpDocumentVMs { get; set; }
    }
    public class DepartmentVM: AuditModel
    {
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentShortName { get; set; }
        public long StudentCount { get; set; }
    }

    public class QPDocumentVM: AuditModel
    { 
        public long QPDocumentId { get; set; }
        public long QPTemplateId { get; set; }
        public int QPDocumentTypeId { get; set; }
        public string QPDocumentTypeName { get; set; }
        public long DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentUrl { get; set; }

    }
}
