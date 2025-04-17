using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetConsolidatedDto
    {
        public required long ExaminationId { get; set; }
        public required string InstitutionCode { get; set; }
        public required string CourseCode { get; set; }
        public required string DepartmentShortName { get; set; }
        public required string RegulationYear { get; set; }
        public required string BatchYear { get; set; }
        public required string DegreeType { get; set; }
        public required string ExamType { get; set; }
        public required int Semester { get; set; }
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; }
        public required long  StudentCount { get; set; }
        public required long DummyNumberCount { get; set; }

    }
}
