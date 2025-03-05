
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("ExamMonth", Schema = "dbo")]
    public class ExamMonth : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ExamMonthId { get; set; }
        public required string Code { get; set; }
    }
}

