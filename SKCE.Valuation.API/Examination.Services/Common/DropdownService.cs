using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using SKCE.Examination.Models.DbModels.Common;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class DropdownService
    {
        private readonly ExaminationDbContext _context;

        public DropdownService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetExamYearsAsync()
        {
            return await
                this._context.Examinations
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.ExamYear)
                .Select(x => x.ExamYear).Distinct().ToListAsync();
        }

        public async Task<List<string>> GetExamMonthsAsync()
        {
            return await
                this._context.Examinations
                .Where(x => x.IsActive)
                .Select(x => x.ExamMonth).Distinct().ToListAsync();
        }

        public async Task<List<string>> GetExamTypesAsync()
        {
            return await
                this._context.Examinations
                .Where(x => x.IsActive)
                .Select(x => x.ExamType).Distinct().ToListAsync();
        }

        public async Task<List<string>> GetExamYearMonthsAsync()
        {
            var resultItems = await this._context.Examinations
                .Where(x => x.IsActive)
                .Select(x => new { x.ExamMonth, x.ExamYear }).Distinct().ToListAsync();

            var finalItems = resultItems
                .OrderByDescending(x => x.ExamYear)
                .ThenByDescending(x => x.ExamMonth)
                .Select(x => new { Value = x.ExamYear + ' ' + x.ExamMonth });

            return finalItems.Select(x => x.Value).ToList();

        }

    }
}
