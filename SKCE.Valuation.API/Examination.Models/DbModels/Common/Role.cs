using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examination.Models.DbModels.Common
{
   public class Role:AuditModel   
    {
        public int Id { get; set; }
        public required string Name { get; set; }

    }
}
