using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswerManagementDto
    {
        public long AnswersheetId { get; set; } = 0;
        public long InstitutionId { get; set; } = 0;
        public string InstitutionName { get; set; } = string.Empty;
        public string RegulationYear { get; set; } = string.Empty;
        public string BatchYear { get; set; } = string.Empty;
        public string DegreeTypeName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int Semester { get; set; }
        public long CourseId { get; set; } = 0;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;                                        
        public string ExamMonth { get; set; } = string.Empty;
        public string ExamYear { get; set; } = string.Empty;
        public string DummyNumber { get; set; } = string.Empty;
        public string? UploadedBlobStorageUrl { get; set; }= string.Empty;
    }
}
