using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ViewModels.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SKCE.Examination.Services.EntityHelpers
{
    internal class AnswersheetImportHelper
    {
        private readonly ExaminationDbContext _dbContext;
        private readonly BlobStorageHelper _blobStorageHelper;



        public AnswersheetImportHelper(ExaminationDbContext context, BlobStorageHelper blobStorageHelper)
        {
            _dbContext = context;
            _blobStorageHelper = blobStorageHelper;
        }


        private static string FileNameWithLocation(string examYear, string examMonth, string courseCode)
        {
            var curTime = DateTime.Now;
            var strDate = curTime.Year.ToString() + curTime.Month.ToString() + curTime.Day.ToString()
                + curTime.Hour.ToString() + curTime.Minute.ToString() + curTime.Second.ToString();

            return "AnswersheetList\\" + examYear + examMonth.Replace('/', '-').Trim() + "\\" + courseCode + "_" + strDate + ".xlsx";

        }

        public async Task<List<AnswersheetImportDetail>> ImportDummyNoFromExcelByCourse(Stream excelStream,
            long institutionId, string examYear, string examMonth, long courseId)
        {
            var result = new List<AnswersheetImportDetail>();

            try
            {

                var examinations =
                    await (from exam in this._dbContext.Examinations
                           join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                           join inst in this._dbContext.Institutions on exam.InstitutionId equals inst.InstitutionId
                           join degre in this._dbContext.DegreeTypes on exam.DegreeTypeId equals degre.DegreeTypeId
                           join dept in this._dbContext.Departments on exam.DepartmentId equals dept.DepartmentId

                           where exam.InstitutionId == institutionId &&
                           exam.ExamYear == examYear &&
                           exam.ExamMonth == examMonth &&
                           exam.CourseId == courseId &&
                           exam.IsActive

                           select new
                           {
                               exam.ExaminationId,
                               exam.InstitutionId,
                               exam.RegulationYear,
                               exam.BatchYear,
                               exam.DegreeTypeId,
                               exam.DepartmentId,
                               exam.Semester,
                               exam.ExamYear,
                               exam.ExamMonth,
                               exam.ExamType,
                               InstitutionCode = inst.Code,
                               DegreeTypeCode = degre.Code,
                               DepartmentCode = dept.ShortName,
                               CourseCode = course.Code
                           }).ToListAsync();

                if (examinations.Count == 0)
                {
                    return result;
                }

                var fileNameToUpload = FileNameWithLocation(examYear, examMonth, examinations.First().CourseCode);

                var uploadedURL = await this._blobStorageHelper
                    .UploadFileAsync(excelStream, fileNameToUpload, "xlsx"); ;

                if (uploadedURL == null)
                    return result;

                var answersheetImport =
                    new AnswersheetImport
                    {
                        DocumentName = fileNameToUpload,
                        DocumentUrl = uploadedURL,
                        InstitutionId = institutionId,
                        ExamMonth = examMonth,
                        ExamYear = examYear,
                        CourseId = courseId,
                        IsActive = true,
                        CreatedById = 1,
                        CreatedDate = DateTime.Now,
                        ModifiedById = 1,
                        ModifiedDate = DateTime.Now,
                        AnswersheetImportDetails = []
                    };

                this._dbContext.AnswersheetImports.Add(answersheetImport);


                using var spreadsheet = SpreadsheetDocument.Open(excelStream, false);

                var workbookPart = spreadsheet.WorkbookPart;

                Sheet? sheet = workbookPart?.Workbook?.Sheets?.GetFirstChild<Sheet>();

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet?.Id);

                var rows = worksheetPart.Worksheet.Descendants<Row>();

                var headerRow = rows.First();
                var dataRows = worksheetPart.Worksheet.Descendants<Row>().Skip(1).ToList();

                foreach (var dataRow in dataRows)
                {
                    var cells = dataRow.Elements<Cell>().ToList();
                    string cellInstCode = GetCellValue(workbookPart, cells[0]);
                    string cellRegulation = GetCellValue(workbookPart, cells[1]);
                    string cellBatch = GetCellValue(workbookPart, cells[2]);
                    string cellDegreeType = GetCellValue(workbookPart, cells[3]);
                    string cellDepartment = GetCellValue(workbookPart, cells[4]);
                    int cellSemester = int.Parse(GetCellValue(workbookPart, cells[5]).ToString());
                    string cellCourseCode = GetCellValue(workbookPart, cells[6]);
                    string cellExamYear = GetCellValue(workbookPart, cells[7]);
                    string cellExamMonth = GetCellValue(workbookPart, cells[8]);
                    string cellExamType = GetCellValue(workbookPart, cells[9]);
                    string cellDummyNo = GetCellValue(workbookPart, cells[10]);

                    bool isValid = true;
                    List<string> errors = new();

                    if (cellInstCode != examinations.First().InstitutionCode)
                    {
                        errors.Add("College Code");
                        isValid = false;
                    }

                    if (cellExamYear != examinations.First().ExamYear)
                    {
                        errors.Add("Exam Year");
                        isValid = false;
                    }

                    if (cellExamMonth != examinations.First().ExamMonth)
                    {
                        errors.Add("Exam Month");
                        isValid = false;
                    }

                    if (cellCourseCode != examinations.First().CourseCode)
                    {
                        errors.Add("Course Code");
                        isValid = false;
                    }

                    var examination = examinations.FirstOrDefault(x =>
                         x.InstitutionCode == cellInstCode
                         && x.RegulationYear == cellRegulation
                         && x.BatchYear == cellBatch
                         && x.DegreeTypeCode == cellDegreeType
                         && x.DepartmentCode == cellDepartment
                         && x.Semester == cellSemester
                         && x.CourseCode == cellCourseCode
                         && x.ExamYear == cellExamYear
                         && x.ExamMonth == cellExamMonth
                         && x.ExamType == cellExamType);

                    if (examination == null)
                    {
                        errors.Add("Examination data not found");
                        isValid = false;
                    }

                    answersheetImport.AnswersheetImportDetails.Add(
                        new AnswersheetImportDetail
                        {
                            ExaminationId = examination?.ExaminationId ?? 0,
                            InstitutionCode = cellInstCode,
                            RegulationYear = cellRegulation,
                            BatchYear = cellBatch,
                            DegreeType = cellDegreeType,
                            DepartmentShortName = cellDepartment,
                            ExamType = cellExamType,
                            Semester = cellSemester,
                            CourseCode = cellCourseCode,
                            ExamMonth = cellExamMonth,
                            ExamYear = cellExamYear,
                            DummyNumber = cellDummyNo,
                            IsValid = isValid,
                            ErrorMessage = string.Join(", ", errors),
                            IsActive = true,
                            CreatedById = 1,
                            CreatedDate = DateTime.Now,
                            ModifiedById = 1,
                            ModifiedDate = DateTime.Now,
                        });
                }

                await _dbContext.SaveChangesAsync();

                return answersheetImport.AnswersheetImportDetails.ToList();

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                excelStream.Close();
            }
        }

        private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell.CellValue == null) return string.Empty;

            var value = cell.CellValue.InnerText;
            return cell.DataType != null && cell.DataType.Value == CellValues.SharedString
                ? workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(value)).InnerText
                : value;
        }


    } // class 
} // namespace 
