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
    [Table("QPTemplates", Schema = "dbo")]
    public class QPTemplates: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public long InstitutionId { get; set; }
        public long DepartmentId { get; set; }
        public long CourseId { get; set; }
        public long QPTemplateDocumentId { get; set; }
        public long QPTemplateAnswerDocumentId { get; set; }
        public long QPTemplateSyallbusDocumentId { get; set; }
        
    }
}
