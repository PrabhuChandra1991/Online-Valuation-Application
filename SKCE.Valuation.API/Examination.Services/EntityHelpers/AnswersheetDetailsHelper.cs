using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetDetailsHelper
    {
        private readonly ExaminationDbContext _context;
        public readonly string _blobBaseUrl;
        public readonly string _containerName;
        public readonly IConfiguration _configuration;

        public AnswersheetDetailsHelper(ExaminationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _blobBaseUrl = configuration["AzureBlobStorage:BaseUrl"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
        }

        public async Task<IEnumerable<AnswerManagementDto>> GetAnswersheetDetailsAsync(
          string? examYear = null, string? examMonth = null, string? examType = null, long? courseId = null,
          long? allocatedToUserId = null, long? answersheetId = null, bool showDummyNo = false)
        {
            try
            {
                var resultItems = await (from answersheet in _context.Answersheets

                                         join examination in this._context.Examinations
                                         on answersheet.ExaminationId equals examination.ExaminationId

                                         join institute in _context.Institutions
                                         on examination.InstitutionId equals institute.InstitutionId

                                         join course in _context.Courses
                                         on examination.CourseId equals course.CourseId

                                         join dtype in _context.DegreeTypes
                                         on examination.DegreeTypeId equals dtype.DegreeTypeId

                                         join allocatedUser in this._context.Users
                                         on answersheet.AllocatedToUserId equals allocatedUser.UserId into allocatedUserResults
                                         from allocatedUserResult in allocatedUserResults.DefaultIfEmpty()

                                         where
                                         answersheet.IsActive == true
                                         && (examination.ExamYear == examYear || examYear == null)
                                         && (examination.ExamMonth == examMonth || examMonth == null)
                                         && (examination.ExamType == examType || examType == null)
                                         && (examination.CourseId == courseId || courseId == null)
                                         && (answersheet.AllocatedToUserId == allocatedToUserId || allocatedToUserId == null)
                                         && (answersheet.AnswersheetId == answersheetId || answersheetId == null)

                                         select new AnswerManagementDto
                                         {
                                             AnswersheetId = answersheet.AnswersheetId,
                                             InstitutionId = examination.InstitutionId,
                                             InstitutionName = institute.Name,
                                             RegulationYear = examination.RegulationYear,
                                             BatchYear = examination.BatchYear,
                                             DegreeTypeName = dtype.Name,
                                             ExamType = examination.ExamType,
                                             Semester = (int)examination.Semester,
                                             CourseId = examination.CourseId,
                                             CourseCode = course.Code,
                                             CourseName = course.Name,
                                             ExamMonth = examination.ExamMonth,
                                             ExamYear = examination.ExamYear,
                                             DummyNumber = answersheet.DummyNumber,
                                             UploadedBlobStorageUrl = null,
                                             AllocatedUserName = (allocatedUserResult != null ? allocatedUserResult.Name : string.Empty),
                                             TotalObtainedMark = answersheet.TotalObtainedMark,
                                             IsEvaluateCompleted = answersheet.IsEvaluateCompleted
                                         }).ToListAsync();


                foreach (var item in resultItems)
                {
                    var dummyNo = item.DummyNumber.Trim();
                    item.UploadedBlobStorageUrl = GetDummyNumberBlobStorageUrl(item.CourseCode, dummyNo);
                    if (allocatedToUserId != null || answersheetId != null)
                    {
                        item.DummyNumber = showDummyNo ? dummyNo : GetDummyNumberMasked(dummyNo.Trim());
                    }
                }

                return resultItems
                    .OrderByDescending(x => x.TotalObtainedMark)
                    .OrderBy(x => x.IsEvaluateCompleted).ToList();

            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GetDummyNumberMasked(string dummyNo)
        {
            return string.Concat(new String('x', 10), dummyNo.AsSpan(dummyNo.Length - 6));
        }

        private string GetDummyNumberBlobStorageUrl(string courseCode, string dummyNumber)
        {
            courseCode = courseCode.Replace('/', '_');

            //Sample url
            //https://skceuatdocuments.blob.core.windows.net/skcedocumentcontainerdev/ANSWERSHEET/23AD201/833825040813032020q52224315223.pdf

            return $"{this._blobBaseUrl}/{this._containerName}/ANSWERSHEET/{courseCode}/{dummyNumber}.pdf";
        }


    }
}
