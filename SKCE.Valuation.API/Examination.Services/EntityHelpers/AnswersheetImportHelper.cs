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

        public async Task<List<AnswersheetImportDetail>>
            ImportDummyNoFromExcelByCourse(Stream excelStream, long examinationId, string contentType)
        {
            var result = new List<AnswersheetImportDetail>();

            try
            {

                var examination =
                    await (from exam in this._dbContext.Examinations
                           join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                           join inst in this._dbContext.Institutions on exam.InstitutionId equals inst.InstitutionId
                           where exam.ExaminationId == examinationId
                           select new
                           {
                               exam.ExaminationId,
                               exam.ExamYear,
                               exam.ExamMonth,
                               CourseCode = course.Code,
                               exam.InstitutionId,
                               InstitutionCode = inst.Code,
                           }).FirstOrDefaultAsync();

                if (examination == null)
                {
                    return result;
                }

                var fileNameToUpload = FileNameWithLocation(examination.ExamYear, examination.ExamMonth, examination.CourseCode);

                var uploadedURL = await this._blobStorageHelper.UploadFileAsync(excelStream, fileNameToUpload, contentType);

                if (uploadedURL == null)
                    return result;

                var answersheetImport =
                    new AnswersheetImport
                    {
                        DocumentName = fileNameToUpload,
                        DocumentUrl = uploadedURL,
                        InstitutionId = examination.InstitutionId,
                        ExamMonth = examination.ExamMonth,
                        ExamYear = examination.ExamYear,
                        ExaminationId = examination.ExaminationId,
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

                Sheet? sheet = workbookPart.Workbook.Sheets.GetFirstChild<Sheet>();

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

                    if (cellCourseCode != examination.CourseCode)
                    {
                        errors.Add("Course Code");
                        isValid = false;
                    }

                    if (cellExamYear != examination.ExamYear)
                    {

                        errors.Add("Exam Year");
                        isValid = false;
                    }

                    if (cellExamMonth != examination.ExamMonth)
                    {
                        errors.Add("Exam Month");
                        isValid = false;
                    }

                    if(cellInstCode != examination.InstitutionCode)
                    {
                        errors.Add("College Code");
                        isValid = false;
                    }

                        answersheetImport.AnswersheetImportDetails.Add(
                        new AnswersheetImportDetail
                        {
                            InstitutionCode = cellInstCode,
                            RegulationYear = cellRegulation,
                            BatchYear = cellBatch,
                            DegreeType = cellDegreeType,
                            DepartmentShortName= cellDepartment,
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
