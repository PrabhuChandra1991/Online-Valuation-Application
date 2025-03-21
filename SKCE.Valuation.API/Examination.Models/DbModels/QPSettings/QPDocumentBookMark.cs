using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("QPDocumentBookMark", Schema = "dbo")]
    public class QPDocumentBookMark : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPDocumentBookMarkId { get; set; }
        public long QPDocumentId { get; set; }
        public string BookMarkName { get; set; }
        public string BookMarkText { get; set; }
    }
}
