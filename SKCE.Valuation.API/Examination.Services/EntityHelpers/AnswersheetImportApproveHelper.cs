using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.EntityHelpers
{
    internal class AnswersheetImportApproveHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetImportApproveHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<bool> CreateAnswerSheetsAndApproveImportedData(
            long answersheetImportId,int absentCount, long loggedInUserId)
        {
            var answersheetImport =
               await this._dbContext.AnswersheetImports
                .FirstOrDefaultAsync(x => x.AnswersheetImportId == answersheetImportId && x.IsActive && x.IsReviewCompleted != true);

            if (answersheetImport == null)
                return false;

            answersheetImport.IsReviewCompleted = true;
            answersheetImport.ReviewCompletedOn = DateTime.Now;
            answersheetImport.ReviewCompletedBy = loggedInUserId;
            answersheetImport.AbsenteesCount = absentCount;
            answersheetImport.ModifiedById = loggedInUserId;
            answersheetImport.ModifiedDate = DateTime.Now;
            this._dbContext.Entry(answersheetImport).State = EntityState.Modified;

            var answersheetImportDetails =
                await this._dbContext.AnswersheetImportDetails
                .Where(x => x.AnswersheetImportId == answersheetImportId && x.IsActive && x.IsValid)
                .ToListAsync();

            foreach (var item in answersheetImportDetails)
            {
                this._dbContext.Answersheets.Add(new Answersheet
                {
                    ExaminationId = item.ExaminationId,
                    DummyNumber = item.DummyNumber,
                    IsActive = true,
                    CreatedById = loggedInUserId,
                    CreatedDate = DateTime.Now,
                    ModifiedById = loggedInUserId,
                    ModifiedDate = DateTime.Now
                });
            }

            await this._dbContext.SaveChangesAsync();

            return true;
        }

    } // class 
} // namespace 
