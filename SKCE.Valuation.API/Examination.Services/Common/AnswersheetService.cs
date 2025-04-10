using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetService
    {
        private readonly ExaminationDbContext _context;

        public AnswersheetService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Answersheet>> GetAllAnswersheetsAsync()
        {
            return await _context.Answersheets.ToListAsync();
        }

    }
}
