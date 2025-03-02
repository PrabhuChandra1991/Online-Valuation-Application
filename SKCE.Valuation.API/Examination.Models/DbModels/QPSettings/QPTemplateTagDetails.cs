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
    [Table("QPTemplateTagDetails", Schema = "dbo")]
    public class QPTemplateTagDetails: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long QPTemplateId { get; set; }
        public long QPTagId { get; set; }
    }
}
