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
    [Table("QPTemplate", Schema = "dbo")]
    public class QPTemplate:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTemplateId { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public long InstitutionId { get; set; }
        public long DepartmentId { get; set; }
        public long CourseId { get; set; }
        public string QPCode { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        public long QPTemplateSyallbusDocumentId { get; set; }
        public long QPDocumentId { get; set; }
        public long QPAnswerDocumentId { get; set; }
        public long QPPrintDocumentId { get; set; }
        public long QPAnswerPrintDocumentId { get; set; }
    }
}
