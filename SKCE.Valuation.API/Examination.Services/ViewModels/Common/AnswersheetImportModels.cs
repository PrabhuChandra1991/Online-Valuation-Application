using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetImportCourseDptDto
    {
        public long ExaminationId { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; }
        public required string DepartmentCode { get; set; }
        public required string DepartmentName { get; set; }
        public required int StudentCount { get; set; }
    }
     
}
