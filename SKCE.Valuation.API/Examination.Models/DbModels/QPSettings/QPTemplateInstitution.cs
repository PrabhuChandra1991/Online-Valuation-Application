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
    [Table("QPTemplateInstitution", Schema = "dbo")]
    public class QPTemplateInstitution:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTemplateInstitutionId { get; set; }
        
        [ForeignKey("QPTemplate")]
        public long QPTemplateId { get; set; }
        public long InstitutionId { get; set; }
        public long StudentCount { get; set; }
        public virtual ICollection<QPTemplateInstitutionDocument> Documents { get; set; } = new List<QPTemplateInstitutionDocument>();
        public virtual ICollection<QPTemplateInstitutionDepartment> Departments { get; set; } = new List<QPTemplateInstitutionDepartment>();
    }
}
