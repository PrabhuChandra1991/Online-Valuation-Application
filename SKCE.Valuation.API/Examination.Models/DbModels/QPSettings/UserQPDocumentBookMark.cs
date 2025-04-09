using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("UserQPDocumentBookMark", Schema = "dbo")]
    public class UserQPDocumentBookMark: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPDocumentBookMarkId { get; set; }
        public long UserQPTemplateId { get; set; }
        public long DocumentId {  get; set; }
        public string BookMarkName { get; set; }
        public string BookMarkText { get; set; }
    }
}
