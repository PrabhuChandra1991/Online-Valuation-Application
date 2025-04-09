using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetQuestionMark", Schema = "dbo")]
    public class AnswersheetQuestionMark : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetQuestionMarkId { get; set; }
        public long AnswersheetId { get; set; } 
        public required string QuestionNumber { get; set; }
        public decimal?  MarkFixed { get; set; }
        public decimal?  MarkScored { get; set; }
    }
}
