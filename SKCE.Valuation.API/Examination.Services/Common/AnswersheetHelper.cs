using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.Common
{
    internal class AnswersheetHelper
    {
        private readonly ExaminationDbContext _dbContext;
        public AnswersheetHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<string> ImportDummyNumberByExcel(Stream excelStream, long loggedInUserId)
        {
            int countExists = 0;
            int countSuccess = 0;
            int countError = 0;

            try
            {

                using var spreadsheet = SpreadsheetDocument.Open(excelStream, false);

                var workbookPart = spreadsheet.WorkbookPart;

                Sheet? sheet = workbookPart.Workbook.Sheets.GetFirstChild<Sheet>();

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);

                var rows = worksheetPart.Worksheet.Descendants<Row>();

                var headerRow = rows.First();
                var dataRows = worksheetPart.Worksheet.Descendants<Row>().Skip(1).ToList();

                var institions = this._dbContext.Institutions.ToList();
                var courses = this._dbContext.Courses.ToList();
                var degreeTypes = this._dbContext.DegreeTypes.ToList();
                var existingDummyNumbers = this._dbContext.Answersheets.Select(x => x.DummyNumber).ToList();

                foreach (var dataRow in dataRows)
                {
                    var cells = dataRow.Elements<Cell>().ToList();

                    string cellInstCode = GetCellValue(workbookPart, cells[0]);
                    string cellRegulation = GetCellValue(workbookPart, cells[1]);
                    string cellBatch = GetCellValue(workbookPart, cells[2]);
                    string cellDegreeType = GetCellValue(workbookPart, cells[3]);
                    string cellExamType = GetCellValue(workbookPart, cells[4]);
                    string cellSemester = GetCellValue(workbookPart, cells[5]);
                    string cellCourseCode = GetCellValue(workbookPart, cells[6]);
                    string cellExamYear = GetCellValue(workbookPart, cells[7]);
                    string cellExamMonth = GetCellValue(workbookPart, cells[8]);
                    string cellDummyNo = GetCellValue(workbookPart, cells[9]);

                    var instition = institions.FirstOrDefault(x => x.Code == cellInstCode);
                    var course = courses.FirstOrDefault(x => x.Code == cellCourseCode);
                    var degreeType = degreeTypes.FirstOrDefault(x => x.Code == cellDegreeType);

                    var IsValid = IsValidString(cellInstCode) && IsValidString(cellRegulation)
                        && IsValidString(cellBatch) && IsValidString(cellDegreeType)
                        && IsValidString(cellExamType) && IsValidString(cellSemester)
                        && IsValidString(cellCourseCode) && IsValidString(cellExamYear)
                        && IsValidString(cellExamMonth) && IsValidString(cellDummyNo)
                        && instition != null && course != null && degreeType != null;

                    if (IsValid)
                    {
                        if (existingDummyNumbers.Any(x => x == cellDummyNo))
                        {
                            countExists++;
                        }
                        else
                        {
                            this._dbContext.Answersheets.Add(new Answersheet
                            {
                                InstitutionId = instition != null ? instition.InstitutionId : 0,
                                CourseId = course != null ? course.CourseId : 0,
                                BatchYear = cellBatch,
                                RegulationYear = cellRegulation,
                                Semester = int.Parse(cellSemester),
                                DegreeTypeId = degreeType != null ? (int)degreeType.DegreeTypeId : 0,
                                ExamType = cellExamType,
                                ExamMonth = cellExamMonth,
                                ExamYear = cellExamYear,
                                DummyNumber = cellDummyNo,
                                IsActive = true,
                                CreatedById = loggedInUserId,
                                CreatedDate = DateTime.Now,
                                ModifiedById = loggedInUserId,
                                ModifiedDate = DateTime.Now
                            });
                            countSuccess++;
                        }
                    }
                    else
                    {
                        countError++;
                    }
                }

                await this._dbContext.SaveChangesAsync();

                return $"Data Imported Sucessfully. \n Already Exists : {countExists.ToString()}  \n Success imported : {countSuccess.ToString()}\n Validation Error : " + countError.ToString();

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
