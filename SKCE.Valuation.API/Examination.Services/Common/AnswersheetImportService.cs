using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityHelpers;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetImportService
    {
        private readonly ExaminationDbContext _context;
        private readonly BlobStorageHelper _blobStorageHelper;

        public AnswersheetImportService(ExaminationDbContext context, BlobStorageHelper blobStorageHelper)
        {
            _context = context;
            _blobStorageHelper = blobStorageHelper;

        }

        public async Task<List<AnswersheetImportCourseDto>>
            GetExaminationCourseInfoAsync(long institutionId, string examYear, string examMonth)
        {
            var resultItems = new List<AnswersheetImportCourseDto>();

            var items = await (from exam in this._context.Examinations
                               join course in this._context.Courses on exam.CourseId equals course.CourseId
                               where exam.IsActive
                               && exam.InstitutionId == institutionId
                               && exam.ExamYear == examYear
                               && exam.ExamMonth == examMonth
                               select new
                               {
                                   exam.CourseId,
                                   course.Code,
                                   course.Name,
                                   exam.StudentCount
                               }).ToListAsync();

            var groupItems = items.GroupBy(x => x.CourseId);

            foreach (var item in groupItems)
            {
                resultItems.Add(
                    new AnswersheetImportCourseDto
                    {
                        CourseId = item.First().CourseId,
                        CourseCode = item.First().Code,
                        CourseName = item.First().Name,
                        StudentCount = item.Sum(x => (int)x.StudentCount)
                    });
            }


            return resultItems.OrderBy(x => x.CourseCode).ToList();
        }


        //POST
        public async Task<List<AnswersheetImportDetail>> ImportDummyNoFromExcelByCourse(Stream excelStream,
            long institutionId, string examYear, string examMonth, long courseId)
        {
            var helper = new AnswersheetImportHelper(this._context, this._blobStorageHelper);

            var result = await helper.ImportDummyNoFromExcelByCourse(excelStream,
                institutionId, examYear, examMonth, courseId);

            return result;
        }


        public async Task<List<AnswersheetImportDto>> GetAnswersheetImports(
            long institutionId, string examYear, string examMonth, long courseId)
        {
            return await this._context.AnswersheetImports
                .Where(x => x.IsActive
                && x.InstitutionId == institutionId
                && x.ExamYear == examYear
                && x.ExamMonth == examMonth
                && x.CourseId == courseId)
                .OrderByDescending(x => x.AnswersheetImportId)
                .Select(x => new AnswersheetImportDto
                {
                    AnswersheetImportId = x.AnswersheetImportId,
                    DocumentName = x.DocumentName,
                    DocumentUrl = x.DocumentUrl,
                    ExamMonth = x.ExamMonth,
                    ExamYear = x.ExamYear,
                    InstitutionId = x.InstitutionId,
                    CourseId = x.CourseId,
                    RecordCount = x.AnswersheetImportDetails.Count(),
                    IsReviewCompleted = x.IsReviewCompleted,
                    ReviewCompletedOn = x.ReviewCompletedOn,
                    ReviewCompletedBy = x.ReviewCompletedBy
                }).ToListAsync();
        }

        public async Task<List<AnswersheetImportDetailDto>> GetAnswersheetImportDetails(long answersheetImportId)
        {
            return await this._context.AnswersheetImportDetails
                .Where(x => x.AnswersheetImportId == answersheetImportId)
                .OrderBy(x => x.IsValid)
                .Select(x => new AnswersheetImportDetailDto
                {
                    AnswersheetImportDetailId = x.AnswersheetImportDetailId,
                    AnswersheetImportId = x.AnswersheetImportId,
                    ExaminationId = x.ExaminationId,
                    InstitutionCode = x.InstitutionCode,
                    RegulationYear = x.RegulationYear,
                    BatchYear = x.BatchYear,
                    DegreeType = x.DegreeType,
                    DepartmentShortName = x.DepartmentShortName,
                    ExamType = x.ExamType,
                    Semester = x.Semester,
                    CourseCode = x.CourseCode,
                    ExamMonth = x.ExamMonth,
                    ExamYear = x.ExamYear,
                    DummyNumber = x.DummyNumber,
                    IsAnswerSheetUploaded =
                        this._context.AnswersheetUploadHistories
                        .Any(y => y.DummyNumber == x.DummyNumber),
                    IsValid = x.IsValid,
                    ErrorMessage = x.ErrorMessage
                }).ToListAsync();
        }

        public async Task<bool> CreateAnswerSheetsAndApproveImportedData(
            long answersheetImportId, int absentCount, long loggedInUserId)
        {
            var helper = new AnswersheetImportApproveHelper(this._context);
            return await helper.CreateAnswerSheetsAndApproveImportedData(
                answersheetImportId, absentCount, loggedInUserId);
        }

        public async Task<bool> DeleteAnswersheetImport(long answersheetImportId, long loggedInUserId)
        {
            var entity = await this._context.AnswersheetImports.FirstOrDefaultAsync(x => x.AnswersheetImportId == answersheetImportId);
            if (entity != null)
            {
                entity.IsActive = false;
                entity.ModifiedById = loggedInUserId;
                entity.ModifiedDate = DateTime.Now;
                this._context.Entry(entity).State = EntityState.Modified;
                await this._context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        //POST
        public async Task<string> UploadAnswersheetAsync(string courseCode, string dummyNumber, Stream file)
        {
            if (dummyNumber.IndexOf(".pdf") == -1)
            {
                dummyNumber = dummyNumber + ".pdf";
            }

            var existEntity = _context.AnswersheetUploadHistories
                .Where(x => x.CourseCode == courseCode && x.DummyNumber == dummyNumber && x.IsActive);
            if (existEntity.Any())
            {
                return "ALREADY-EXISTS";
            }

            var fileName = "ANSWERSHEET/" + courseCode + "/" + dummyNumber;
            var uploadedURL = await _blobStorageHelper.UploadFileAsync(file, fileName, "pdf");
            if (uploadedURL != null)
            {
                _context.AnswersheetUploadHistories.Add(new AnswersheetUploadHistory
                {
                    CourseCode = courseCode,
                    DummyNumber = dummyNumber,
                    BlobURL = uploadedURL,
                    IsActive = true,
                    CreatedById = 1,
                    CreatedDate = DateTime.Now,
                    ModifiedById = 1,
                    ModifiedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return "UPLOAD-SUCCESS";
            }
            else
            {
                return "UPLOAD-FAILED";
            }
        }

    }
}
