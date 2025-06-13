using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetMarkTransHelper
    {
        private readonly ExaminationDbContext _context;

        public AnswersheetMarkTransHelper(ExaminationDbContext context)
        {
            _context = context;
        }


        public async Task<List<AnswersheetQuestionwiseMark>> GetAnswersheetMarkAsync(long answersheetId)
        {
            var markRecord = await _context.AnswersheetQuestionwiseMarks
                .Where(e => e.AnswersheetId == answersheetId &&
                e.IsActive == true)
                .ToListAsync();

            return markRecord;
        }

        public async Task<decimal> SaveAnswersheetMarkAsync(AnswersheetQuestionwiseMark entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
             
            var existingEntity = await _context.AnswersheetQuestionwiseMarks.FirstOrDefaultAsync(e =>
                e.AnswersheetId == entity.AnswersheetId &&
                e.QuestionNumber == entity.QuestionNumber &&
                e.QuestionNumberSubNum == entity.QuestionNumberSubNum &&
                e.IsActive == true);

            if (existingEntity == null)
            {
                entity.IsActive = true;
                entity.CreatedDate = DateTime.Now;
                entity.ModifiedById = entity.CreatedById;
                entity.ModifiedDate = DateTime.Now;

                _context.AnswersheetQuestionwiseMarks.Add(entity);
                await _context.SaveChangesAsync(); 
            }
            else
            {
                if (entity.ObtainedMark > 0 && entity.ObtainedMark != existingEntity.ObtainedMark)
                {
                    existingEntity.ModifiedById = entity.ModifiedById;
                    existingEntity.ModifiedDate = DateTime.Now;
                    existingEntity.ObtainedMark = entity.ObtainedMark;
                    await _context.SaveChangesAsync(); 
                }
                else if (entity.ObtainedMark == 0)
                {
                    _context.AnswersheetQuestionwiseMarks.Remove(existingEntity);
                    await _context.SaveChangesAsync(); 
                } 
            }

            var totalMarkHelper = new AnswersheetTotalMarkHelper(this._context);
            var totalObtainedMarks= await totalMarkHelper.UpdateTotalMarks(entity.AnswersheetId, entity.ModifiedById);

            return totalObtainedMarks;
        }

        public async Task<Boolean> CompleteEvaluationSync(long answersheetId, long evaluatedByUserId)
        {
            var existingEntity = await _context.Answersheets.FirstOrDefaultAsync(e =>
                e.AnswersheetId == answersheetId &&
                e.IsActive == true);

            if (existingEntity != null)
            {
                existingEntity.IsEvaluateCompleted = true;
                existingEntity.EvaluatedByUserId = evaluatedByUserId;
                existingEntity.EvaluatedDateTime = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            else
            {
                return false;
            }

            return true;
        }

        public async Task<bool> RevertEvaluation(long answersheetId)
        {
            var existingEntity = await _context.Answersheets.FirstOrDefaultAsync(e =>
                e.AnswersheetId == answersheetId &&
                e.IsActive == true);

            if (existingEntity != null)
            {
                existingEntity.IsEvaluateCompleted = false;
                existingEntity.EvaluatedDateTime = null;

                await _context.SaveChangesAsync();
            }
            else
            {
                return false;
            }

            return true;
        }

        public async Task<bool> EvaluationHistory(long answersheetId,long questionNumber)
        {
            var evalHist = _context.AnswersheetQuestionwiseMarks.Where(e =>
                e.AnswersheetId == answersheetId && e.QuestionNumber == questionNumber).FirstOrDefault();

            if (evalHist != null)
            {
                    AnswersheetQuestionwiseMarkHistory objAnsHist = new AnswersheetQuestionwiseMarkHistory();
                    objAnsHist.AnswersheetQuestionwiseMarkId = evalHist.AnswersheetQuestionwiseMarkId;
                    objAnsHist.AnswersheetId = evalHist.AnswersheetId;
                    objAnsHist.QuestionNumber = evalHist.QuestionNumber;
                    objAnsHist.QuestionNumberSubNum = evalHist.QuestionNumberSubNum;
                    objAnsHist.QuestionPartName = evalHist.QuestionPartName;
                    objAnsHist.QuestionGroupName = evalHist.QuestionGroupName;
                    objAnsHist.MaximumMark = evalHist.MaximumMark;
                    objAnsHist.ObtainedMark = evalHist.ObtainedMark;
                    objAnsHist.MaximumMark = evalHist.MaximumMark;
                    objAnsHist.IsActive = evalHist.IsActive;
                    objAnsHist.CreatedDate = evalHist.CreatedDate;
                    objAnsHist.CreatedById = evalHist.CreatedById;
                    objAnsHist.ModifiedDate = evalHist.ModifiedDate;
                    objAnsHist.ModifiedById = evalHist.ModifiedById;
                    _context.AnswersheetQuestionwiseMarkHistories.Add(objAnsHist);

                await _context.SaveChangesAsync();
            }
            else
            {
                return false;
            }

            return true;
        }


    } // Class
}
