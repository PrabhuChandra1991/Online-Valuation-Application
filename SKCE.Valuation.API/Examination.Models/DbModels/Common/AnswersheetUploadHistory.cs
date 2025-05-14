using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetUploadHistory", Schema = "dbo")]
    public class AnswersheetUploadHistory : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetUploadHistoryId { get; set; }
        public string CourseCode { get; set; }
        public string DummyNumber { get; set; }
        public string BlobURL { get; set; }
    }
}
