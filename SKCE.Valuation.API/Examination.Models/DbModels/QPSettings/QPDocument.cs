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

    [Table("QPDocument", Schema = "dbo")]
    public class QPDocument:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPDocumentId { get; set; }
        public long InstitutionId { get; set; }
        public string RegulationYear { get; set; }
        public string DegreeTypeName { get; set; }
        public long DocumentId { get; set; }
        public long DocumentTypeId { get; set; }
    }
}
