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

            var UnallocatedAnswersheets =
                await this._dbContext.Answersheets
                            .Where(x => x.InstitutionId == examination.InstitutionId && x.IsActive
                                        && x.CourseId == examination.CourseId
                                        && x.BatchYear == examination.BatchYear
                                        && x.RegulationYear == examination.RegulationYear
                                        && x.Semester == examination.Semester
                                        && x.DegreeTypeId == examination.DegreeTypeId
                                        && x.ExamType == examination.ExamType
                                        && x.ExamMonth == examination.ExamMonth
                                        && x.ExamYear == examination.ExamYear
                                        && x.AllocatedToUserId == null).ToListAsync();

            if (UnallocatedAnswersheets.Count < inputModel.Noofsheets)
            {
                return false;
            }

            var answersheetWithRandom = new List<AnswersheetRandomNo>();

            Random random = new Random();

            foreach ( var item in UnallocatedAnswersheets)
            {
                answersheetWithRandom.Add(
                    new AnswersheetRandomNo
                    {
                        Answersheet = item,
                        RandomNo = random.Next(1000, 9999)
                    });
            }

            var selectedItems = answersheetWithRandom.OrderBy(x => x.RandomNo).Take(inputModel.Noofsheets);

            foreach ( var item in selectedItems)
            {
                item.Answersheet.AllocatedToUserId = inputModel.UserId;
                item.Answersheet.ModifiedById = loggedInUserId;
                item.Answersheet.ModifiedDate = DateTime.Now;

                this._dbContext.Update(item.Answersheet).State = EntityState.Modified;
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
