using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.QPSettings
{
   public class QPSubmissionVM
    {
        public bool? IsGraphsRequired { get; set; }
        public string? GraphName { get; set; }
        public bool? IsTablesAllowed { get; set; }
        public string? TableName { get; set; }
    }
}
