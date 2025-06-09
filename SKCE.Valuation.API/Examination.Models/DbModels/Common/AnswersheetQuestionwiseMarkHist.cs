using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetQuestionwiseMarkHistory", Schema ="dbo")]
    public class AnswersheetQuestionwiseMarkHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetQuestionwiseMarkHistoryId { get; set; }
        public long AnswersheetQuestionwiseMarkId { get; set; }
        public long AnswersheetId { get; set; }
        public int QuestionNumber { get; set; }
        public int QuestionNumberSubNum { get; set; }
        public string QuestionPartName { get; set; } = string.Empty;
        public string QuestionGroupName { get; set; } = string.Empty;
        public decimal MaximumMark { get; set; }
        public decimal ObtainedMark { get; set; }
        public bool IsActive { get; set; }
        public long CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public long ModifiedById { get; set; }
        public DateTime? ModifiedDate { get; set; }

    }
}
