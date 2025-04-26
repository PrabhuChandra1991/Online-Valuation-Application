using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class DummyNumberImportResponse
    {
        public int SuccessCount { get; set; } = 0;
        public int InvalidCount { get; set; } = 0;
        public List<string> AlreadyExistingNos { get; set; } = []; 
    }
}
