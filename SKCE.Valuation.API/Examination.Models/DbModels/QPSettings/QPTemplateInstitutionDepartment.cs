using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("QPTemplateInstitutionDepartment", Schema = "dbo")]
    public class QPTemplateInstitutionDepartment:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QPTemplateInstitutionDepartmentId { get; set; }
        
        [ForeignKey("QPTemplateInstitution")]
        public int QPTemplateInstitutionId { get; set; }
        public long DepartmentId { get; set; }
        public long StudentCount { get; set; }
    }
}
