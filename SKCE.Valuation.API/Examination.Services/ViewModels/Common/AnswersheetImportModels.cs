using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetImportCourseDto
    {
        public long CourseId { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; } 
        public required int StudentCount { get; set; }
    }

    public class AnswersheetImportDto
    {
        public long AnswersheetImportId { get; set; }
        public required string DocumentName { get; set; } = string.Empty;
        public required string DocumentUrl { get; set; } = string.Empty;
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; }
        public required long InstitutionId { get; set; } = 0;
        public required long CourseId { get; set; } = 0;
        public required int  RecordCount { get; set; } = 0;
        public bool? IsReviewCompleted { get; set; } = null;
        public DateTime? ReviewCompletedOn { get; set; } = null;
        public long? ReviewCompletedBy { get; set; } = null;
    }


}
