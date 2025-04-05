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
        public required string QPTemplateName { get; set; }
        public string QPCode { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        public long CourseId { get; set; }
        public required string RegulationYear { get; set; }
        public required string BatchYear { get; set; }
        public required long DegreeTypeId { get; set; }
        public required string ExamYear { get; set; }
        public required string ExamMonth { get; set; }
        public required string ExamType { get; set; }
        public required long Semester { get; set; }
        public required long StudentCount { get; set; }
        public  long CourseSyllabusDocumentId { get; set; }
        public long? PrintedDocumentId {  get; set; }

    }
}
