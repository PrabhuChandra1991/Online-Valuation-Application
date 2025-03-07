using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;

public class ExcelImportHelper
{
    private readonly ExaminationDbContext _dbContext;

    public ExcelImportHelper(ExaminationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> ImportDataFromExcel(Stream excelStream)
    {
        using var spreadsheet = SpreadsheetDocument.Open(excelStream, false);
        var workbookPart = spreadsheet.WorkbookPart;
        var sheet = workbookPart.Workbook.Sheets.GetFirstChild<Sheet>();
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        var rows = worksheetPart.Worksheet.Descendants<Row>().Skip(1); // Skip header

        var degreeTypes = new HashSet<string>();
        var departments = new HashSet<Department>();
        var courses = new List<Course>();

        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string degreeTypeName = GetCellValue(workbookPart, cells[2]).Trim();

            if (!string.IsNullOrEmpty(degreeTypeName))
                degreeTypes.Add(degreeTypeName);
        }

        // Insert unique DegreeTypes
        var existingDegreeTypes = _dbContext.DegreeTypes.ToList();
        var newDegreeTypes = new HashSet<DegreeType>();
        foreach (var dt in degreeTypes)
        {
            if (!existingDegreeTypes.Any(x => x.Name == dt))
            {
                var newDegreeType = new DegreeType { Name = dt, Code = dt };
                AuditHelper.SetAuditPropertiesForInsert(newDegreeType, 1);
                newDegreeTypes.Add(newDegreeType);
            }
        }
        await _dbContext.DegreeTypes.AddRangeAsync(newDegreeTypes);
        await _dbContext.SaveChangesAsync(); 


        var updatedDegreeTypes = await _dbContext.DegreeTypes.ToListAsync();
        var updatedDepartments = await _dbContext.Departments.ToListAsync();
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string degreeTypeName = GetCellValue(workbookPart, cells[2]).Trim();
            string departmentName = GetCellValue(workbookPart, cells[3]).Trim();
            string departmentShortName = GetCellValue(workbookPart, cells[3]).Trim();

            var degreeType = updatedDegreeTypes.FirstOrDefault(d => d.Name == degreeTypeName);

            if (degreeType == null)
            {
                return $"Error: Invalid DegreeType or Department at row {rows.ToList().IndexOf(row) + 2}";
            }
            if (!updatedDepartments.Any(x => x.Name == departmentName))
            {
                var department = new Department
                {
                    Name = departmentName,
                    ShortName = departmentShortName,
                    DegreeTypeId = degreeType.DegreeTypeId
                };
                AuditHelper.SetAuditPropertiesForInsert(department, 1);
                departments.Add(department);

            }
        }
        await _dbContext.Departments.AddRangeAsync(departments);
        await _dbContext.SaveChangesAsync(); // Commit Master Data


        var examMonths = await _dbContext.ExamMonths.ToListAsync();
        // Process Courses with Foreign Keys
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string regulation = GetCellValue(workbookPart, cells[0]).Trim();
            string batch = GetCellValue(workbookPart, cells[1]).Trim();
            string degreeTypeName = GetCellValue(workbookPart, cells[2]).Trim();
            string departmentName = GetCellValue(workbookPart, cells[3]).Trim();
            string semester = GetCellValue(workbookPart, cells[4]).Trim();
            string courseCode = GetCellValue(workbookPart, cells[5]).Trim();
            string courseName = GetCellValue(workbookPart, cells[6]).Trim();
            string studentCount = GetCellValue(workbookPart, cells[7]).Trim();


            var degreeType = updatedDegreeTypes.FirstOrDefault(d => d.Name == degreeTypeName);
            var department = updatedDepartments.FirstOrDefault(d => d.Name == departmentName);

            if (degreeType == null || department == null)
            {
                return $"Error: Invalid DegreeType or Department at row {rows.ToList().IndexOf(row) + 2}";
            }

            courses.Add(new Course
            {
                Name = courseName,
                Code = courseCode,
                RegulationYear = regulation,
                BatchYear = batch,
                Semester = string.IsNullOrEmpty(semester) ? (long?)null : long.Parse(semester),
                StudentCount = string.IsNullOrEmpty(studentCount) ? (long?)null : long.Parse(studentCount),
                DegreeTypeId = degreeType.DegreeTypeId,
                DepartmentId = department.DepartmentId,
                CreatedById = 1,
                CreatedDate = DateTime.UtcNow,
                ModifiedById = 1,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
                ExamMonthId = (examMonths != null)? (long)(examMonths.FirstOrDefault(x => x.Semester == long.Parse(semester))?.ExamMonthId):1
            });
        }

        await _dbContext.Courses.AddRangeAsync(courses);
        await _dbContext.SaveChangesAsync();

        return "Courses imported successfully! Imported Count is  "+ courses.Count;
    }

    private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        if (cell.CellValue == null) return string.Empty;

        var value = cell.CellValue.InnerText;
        return cell.DataType != null && cell.DataType.Value == CellValues.SharedString
            ? workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(value)).InnerText
            : value;
    }
}
