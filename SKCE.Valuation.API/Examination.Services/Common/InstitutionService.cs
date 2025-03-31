using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class InstitutionService
    {
        private readonly ExaminationDbContext _context;

        public InstitutionService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Institution>> GetAllInstitutionsAsync()
        {
            return await _context.Institutions.ToListAsync();
        }
    }
}
