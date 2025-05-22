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
    public class AnswersheetAllocateHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetAllocateHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<List<long>> AllocateAnswersheetsToUserRandomly(AnswersheetAllocateInputModel inputModel, long loggedInUserId)
        {
            var answersheetIds = new List<long>();

            var unallocatedAnswersheets =
                await (from anshet in this._dbContext.Answersheets
                       join exam in this._dbContext.Examinations on anshet.ExaminationId equals exam.ExaminationId
                       where exam.ExamYear == inputModel.ExamYear
                       && exam.ExamMonth == inputModel.ExamMonth
                       && exam.ExamType == inputModel.ExamType
                       && exam.CourseId == inputModel.CourseId
                       && anshet.AllocatedToUserId == null
                       && anshet.IsActive && exam.IsActive
                       select anshet).ToListAsync();

            if (unallocatedAnswersheets.Count == 0)
                return answersheetIds;
              
            var answersheetWithRandom = new List<AnswersheetRandomNo>();

            Random random = new Random();

            foreach (var item in unallocatedAnswersheets)
            {
                answersheetWithRandom.Add(
                    new AnswersheetRandomNo
                    {
                        Answersheet = item,
                        RandomNo = random.Next(1000, 9999)
                    });
            }

            var selectedItems = answersheetWithRandom.OrderBy(x => x.RandomNo).Take(inputModel.Noofsheets);

            foreach (var item in selectedItems)
            {
                item.Answersheet.AllocatedToUserId = inputModel.UserId;
                item.Answersheet.ModifiedById = loggedInUserId;
                item.Answersheet.ModifiedDate = DateTime.Now;
                this._dbContext.Update(item.Answersheet).State = EntityState.Modified;

                answersheetIds.Add(item.Answersheet.AnswersheetId);

            }

            var user = await this._dbContext.Users.Where(x => x.UserId == inputModel.UserId).FirstOrDefaultAsync();
            if (user != null)
            {
                user.IsActive = true;
                this._dbContext.Update(user).State = EntityState.Modified;
            }

            await this._dbContext.SaveChangesAsync();

            return answersheetIds;

        }


        private class AnswersheetRandomNo
        {
            public required Answersheet Answersheet { get; set; }
            public required int RandomNo { get; set; }
        }


    } // Class
}
