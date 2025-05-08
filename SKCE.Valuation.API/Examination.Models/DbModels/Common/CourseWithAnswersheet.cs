using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    public class CourseWithAnswersheet
    {
        public long CourseId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public long ExaminationId { get; set; }
        public int Count { get; set; }
    }
}
