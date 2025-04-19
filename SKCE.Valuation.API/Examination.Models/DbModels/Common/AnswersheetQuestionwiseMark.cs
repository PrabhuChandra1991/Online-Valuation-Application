using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetQuestionwiseMark", Schema = "dbo")]
    public class AnswersheetQuestionwiseMark : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetQuestionwiseMarkId { get; set; }
        public long AnswersheetId { get; set; }
        public int QuestionNumber { get; set; }
        public int QuestionNumberSubNum { get; set; }
        public string QuestionPartName { get; set; } = string.Empty;
        public string QuestionGroupName { get; set; } = string.Empty;
        public decimal MaximumMark { get; set; }
        public decimal ObtainedMark { get; set; }
    }
}
