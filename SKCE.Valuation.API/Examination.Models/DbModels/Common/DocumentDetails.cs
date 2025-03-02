using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("DocumentDetails", Schema = "dbo")]
    public  class DocumentDetails:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }
        public required string DocumentName { get; set; }
        public required string DocumentUrl { get; set; }
    }
}
