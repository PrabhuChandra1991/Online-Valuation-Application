
using SKCE.Examination.Models.DbModels;

namespace SKCE.Examination.Services.ViewModels.QPSettings
{
    public class ImportHistoryVM:AuditModel
    {
        public long ImportHistoryId { get; set; }
        public long DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentUrl { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long TotalCount { get; set; }
        public long CoursesCount { get; set; }
        public string Courses { get; set; }
        public long InstitutionsCount { get; set; }
        public long DepartmentsCount { get; set; }
    }
}
