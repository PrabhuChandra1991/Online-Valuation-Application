using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.EntityHelpers
{
    internal class AnswersheetHelper
    {
        private readonly ExaminationDbContext _dbContext;
        public AnswersheetHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<DummyNumberImportResponse> ImportDummyNumberByExcel(Stream excelStream, long loggedInUserId)
        {
            var response = new DummyNumberImportResponse();

            try
            {

                using var spreadsheet = SpreadsheetDocument.Open(excelStream, false);

                var workbookPart = spreadsheet.WorkbookPart;

                Sheet? sheet = workbookPart.Workbook.Sheets.GetFirstChild<Sheet>();

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet?.Id);

                var rows = worksheetPart.Worksheet.Descendants<Row>();

                var headerRow = rows.First();
                var dataRows = worksheetPart.Worksheet.Descendants<Row>().Skip(1).ToList();
                 
                var existingDummyNumbers = _dbContext.Answersheets.Select(x => x.DummyNumber).ToList();

                foreach (var dataRow in dataRows)
                {
                    var cells = dataRow.Elements<Cell>().ToList();

                    string cellInstCode = GetCellValue(workbookPart, cells[0]);
                    string cellRegulation = GetCellValue(workbookPart, cells[1]);
                    string cellBatch = GetCellValue(workbookPart, cells[2]);
                    string cellDegreeType = GetCellValue(workbookPart, cells[3]);
                    string cellExamType = GetCellValue(workbookPart, cells[4]);
                    int cellSemester = int.Parse(GetCellValue(workbookPart, cells[5]).ToString());
                    string cellCourseCode = GetCellValue(workbookPart, cells[6]);
                    string cellExamYear = GetCellValue(workbookPart, cells[7]);
                    string cellExamMonth = GetCellValue(workbookPart, cells[8]);
                    string cellDummyNo = GetCellValue(workbookPart, cells[9]);


                    var examinationId =
                      (from exam in this._dbContext.Examinations
                       join inst in this._dbContext.Institutions on exam.InstitutionId equals inst.InstitutionId
                       join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                       join dgreType in this._dbContext.DegreeTypes on exam.DegreeTypeId equals dgreType.DegreeTypeId
                       where
                       exam.IsActive == true
                       && inst.Code == cellInstCode
                       && exam.RegulationYear == cellRegulation
                       && exam.BatchYear == cellBatch
                       && dgreType.Code == cellDegreeType
                       && exam.ExamType == cellExamType
                       && exam.Semester == cellSemester
                       && course.Code == cellCourseCode
                       && exam.ExamYear == cellExamYear
                       && exam.ExamMonth == cellExamMonth
                       select exam.ExaminationId).FirstOrDefault();

                    if (examinationId != 0)
                    {
                        if (existingDummyNumbers.Any(x => x == cellDummyNo))
                        {
                            response.InvalidCount++;
                            response.AlreadyExistingNos.Add(cellDummyNo); 
                        }
                        else
                        {
                            _dbContext.Answersheets.Add(new Answersheet
                            {
                                ExaminationId = examinationId,
                                DummyNumber = cellDummyNo,
                                IsActive = true,
                                CreatedById = loggedInUserId,
                                CreatedDate = DateTime.Now,
                                ModifiedById = loggedInUserId,
                                ModifiedDate = DateTime.Now
                            });
                            response.SuccessCount++;
                        }
                    }
                    else
                    {
                        response.InvalidCount++;
                    }
                }

                await _dbContext.SaveChangesAsync();

                return response;

            }
            catch (Exception ex)
            {
                throw ex;
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

        private static bool IsValidString(string? val)
        {
            return val != null && val != string.Empty;
        }

    } // class 
} // namespace 
