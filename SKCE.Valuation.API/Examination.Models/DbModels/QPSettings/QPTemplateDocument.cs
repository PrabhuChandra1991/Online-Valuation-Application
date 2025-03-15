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
    [Table("QPTemplateDocument", Schema = "dbo")]
    public class QPTemplateDocument:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTemplateDocumentId { get; set; }

        [ForeignKey("QPTemplate")]
        public long QPTemplateId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public long DocumentId { get; set; }
    }
}
