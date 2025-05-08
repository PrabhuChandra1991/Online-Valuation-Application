using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetConsolidatedDto
    {
        public required long CourseId { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; } 
        public required long  StudentTotalCount { get; set; }
        public required long AnswerSheetTotalCount { get; set; }
        public required long AnswerSheetAllocatedCount { get; set; }
        public required long AnswerSheetNotAllocatedCount { get; set; }

    }
}
