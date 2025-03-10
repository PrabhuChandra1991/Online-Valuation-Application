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

        // read and update Institutions
        var institutionCodes = new HashSet<string>();
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string institutionCode = GetCellValue(workbookPart, cells[0]).Trim();

            if (!string.IsNullOrEmpty(institutionCode) && !institutionCodes.Any(ic => ic == institutionCode))
                institutionCodes.Add(institutionCode);
        }
        // Insert unique Institutions
        var existingInstitutions = _dbContext.Institutions.ToList();
        var newInstitutions = new HashSet<Institution>();
        foreach (var institutionCode in institutionCodes)
        {
            if (!existingInstitutions.Any(x => x.Code == institutionCode))
            {
                var newInstitution = new Institution { Name = institutionCode, Code = institutionCode,Email="",MobileNumber=""};
                AuditHelper.SetAuditPropertiesForInsert(newInstitution, 1);
                newInstitutions.Add(newInstitution);
            }
        }
        await _dbContext.Institutions.AddRangeAsync(newInstitutions);


        // read and update departments
        var departments = new HashSet<Department>();
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string departmentName = GetCellValue(workbookPart, cells[4]).Trim();
            string departmentShortName = GetCellValue(workbookPart, cells[5]).Trim();

            if (!string.IsNullOrEmpty(departmentName) && !departments.Any(d => d.Name == departmentName))
            {
                departments.Add(new Department() { Name=departmentName,ShortName= departmentShortName });

            }
        }
        // Insert unique departments
        var existingDepartments = _dbContext.Departments.ToList();
        var newDepartments = new HashSet<Department>();
        foreach (var department in departments)
        {
            if (!existingDepartments.Any(x => x.Name == department.Name))
            {
                AuditHelper.SetAuditPropertiesForInsert(department, 1);
                newDepartments.Add(department);
            }
        }
        await _dbContext.Departments.AddRangeAsync(newDepartments);

        // read and update departments
        var courses = new HashSet<Course>();
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string courseCode = GetCellValue(workbookPart, cells[8]).Trim();
            string courseName = GetCellValue(workbookPart, cells[9]).Trim();

            if (!string.IsNullOrEmpty(courseCode) && !courses.Any(d => d.Code == courseCode))
            {
                courses.Add(new Course() { Name = courseName, Code = courseCode });

            }
        }
        // Insert unique departments
        var existingCourses = _dbContext.Courses.ToList();
        var newCourses = new HashSet<Course>();
        foreach (var course in courses)
        {
            if (!existingCourses.Any(x => x.Code == course.Code))
            {
                AuditHelper.SetAuditPropertiesForInsert(course, 1);
                newCourses.Add(course);
            }
        }
        await _dbContext.Courses.AddRangeAsync(newCourses);
        await _dbContext.SaveChangesAsync();

        var updatedInstitutions = _dbContext.Institutions.ToList();
        var updatedDepartments = _dbContext.Departments.ToList();
        var updatedCourses = _dbContext.Courses.ToList();
        var degreeTypes = _dbContext.DegreeTypes.ToList();
        var examMonths = _dbContext.ExamMonths.ToList();
        var courseDepartMents = new HashSet<CourseDepartment>();
        // Process Institution, Course, Department, StudentCount, DegreeType, Semester, Batch, Regulation, ExamMonth
        foreach (var row in rows)
        {
            var cells = row.Elements<Cell>().ToList();
            string institutionCode = GetCellValue(workbookPart, cells[0]).Trim();
            string regulation = GetCellValue(workbookPart, cells[1]).Trim();
            string batch = GetCellValue(workbookPart, cells[2]).Trim();
            string degreeTypeName = GetCellValue(workbookPart, cells[3]).Trim();
            string departmentName = GetCellValue(workbookPart, cells[4]).Trim();
            string examType = GetCellValue(workbookPart, cells[6]).Trim();
            string semester = GetCellValue(workbookPart, cells[7]).Trim();
            string courseCode = GetCellValue(workbookPart, cells[8]).Trim();
            string studentCount = GetCellValue(workbookPart, cells[10]).Trim();


            var institution = updatedInstitutions.FirstOrDefault(d => d.Code == institutionCode);
            var course = updatedCourses.FirstOrDefault(d => d.Code == courseCode);
            var department = updatedDepartments.FirstOrDefault(d => d.Name == departmentName);
            var degreeType = degreeTypes.FirstOrDefault(d => d.Name == degreeTypeName);
            var examMonth = examMonths.FirstOrDefault(d => d.Semester == long.Parse(semester));


            courseDepartMents.Add(new CourseDepartment()
            {
                InstitutionId = (institution != null) ?institution.InstitutionId:1,
                RegulationYear = regulation,
                BatchYear = batch,
                DegreeTypeId = (degreeType != null) ? degreeType.DegreeTypeId : 1,
                DepartmentId = (department != null) ? department.DepartmentId : 1,
                ExamType = examType,
                Semester = string.IsNullOrEmpty(semester) ? 0 : long.Parse(semester),
                ExamMonthId = (examMonth != null) ? examMonth.ExamMonthId : 1,
                CourseId = (course != null) ? course.CourseId : 1,
                StudentCount = string.IsNullOrEmpty(studentCount) ? 0 : long.Parse(studentCount),
                CreatedById = 1,
                CreatedDate = DateTime.UtcNow,
                ModifiedById = 1,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true,
            });
        }

        await _dbContext.CourseDepartments.AddRangeAsync(courseDepartMents);
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
