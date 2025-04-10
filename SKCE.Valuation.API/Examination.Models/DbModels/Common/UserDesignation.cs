using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("UserDesignation", Schema = "dbo")]
    public class UserDesignation:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserDesignationId { get; set; }
        public long DesignationId { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public long Experience { get; set; }
        public bool IsCurrent { get; set; }
        public string? Department { get; set; }
        public string? CollegeName { get; set; }
    }
}
