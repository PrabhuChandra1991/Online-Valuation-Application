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
    public class AnswersheetTotalMarkHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetTotalMarkHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task UpdateTotalMarks(long answersheetId, long loggedInUserId)
        {
            var answersheet =
                this._dbContext.Answersheets
                .FirstOrDefault(x => x.AnswersheetId == answersheetId);

            if (answersheet != null)
            {
                var entityItems =
                   this._dbContext.AnswersheetQuestionwiseMarks
                       .Where(x => x.AnswersheetId == answersheetId && x.IsActive).ToList();

                var groupedItems =
                    entityItems
                    .GroupBy(x => new { x.QuestionGroupName, x.QuestionNumber })
                    .Select(x =>
                    new
                    {
                        QuestionGroupName = x.Key.QuestionGroupName,
                        QuestionNumber = x.Key.QuestionNumber,
                        QnwiseObtainedMark = x.Sum(x => x.ObtainedMark)
                    }).ToList();

                var finalItems = groupedItems
                    .GroupBy(x => x.QuestionGroupName)
                    .Select(x => new
                    {
                        GroupName = x.Key,
                        finalMark = x.Max(x => x.QnwiseObtainedMark)
                    });

                decimal totalObtainedMarks = finalItems.Sum(x => x.finalMark);

                answersheet.TotalObtainedMark = totalObtainedMarks;
                answersheet.ModifiedById = loggedInUserId;
                answersheet.ModifiedDate = DateTime.Now;

                this._dbContext.Update(answersheet).State = EntityState.Modified;

                await this._dbContext.SaveChangesAsync();

            }



        }



    } // Class
}
