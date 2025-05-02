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

        public async Task<Boolean> AllocateAnswersheetsToUserRandomly(AnswersheetAllocateInputModel inputModel, long loggedInUserId)
        {
            var examination = this._dbContext.Examinations.FirstOrDefault(x => x.ExaminationId == inputModel.ExaminationId && x.IsActive);

            if (examination == null)
                return false;

            var unallocatedAnswersheets =
                await this._dbContext.Answersheets
                            .Where(x => x.IsActive
                            && x.ExaminationId == examination.ExaminationId
                            && x.AllocatedToUserId == null).ToListAsync();

            if (unallocatedAnswersheets.Count < inputModel.Noofsheets)
            {
                return false;
            }

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
            }

            var user = await this._dbContext.Users.Where(x => x.UserId == inputModel.UserId).FirstOrDefaultAsync();
            if (user != null)
            {
                user.IsActive = true;
                this._dbContext.Update(user).State = EntityState.Modified;
            }

            await this._dbContext.SaveChangesAsync();

            return true;

        }


        private class AnswersheetRandomNo
        {
            public required Answersheet Answersheet { get; set; }
            public required int RandomNo { get; set; }
        }


    } // Class
}
