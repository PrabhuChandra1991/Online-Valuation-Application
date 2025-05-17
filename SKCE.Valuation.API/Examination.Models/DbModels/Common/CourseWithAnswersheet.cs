using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    public class CourseWithAnswersheet
    {
        public long CourseId { get; set; } = 0;
        public string Code { get; set; }=string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }
}
