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
    [Table("QPTag", Schema = "dbo")]
    public class QPTag: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTagId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Tag { get; set; }
        public long TagDataTypeId { get; set; }
        public bool IsQuestion { get; set; }
    }
    
}
