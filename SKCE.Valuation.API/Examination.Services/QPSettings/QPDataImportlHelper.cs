using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspose.Words;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ViewModels.QPSettings;
using Document = Aspose.Words.Document;

public class QPDataImportHelper
{
    private readonly ExaminationDbContext _dbContext;
    private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
    public QPDataImportHelper(ExaminationDbContext dbContext, AzureBlobStorageHelper azureBlobStorageHelper)
    {
        _dbContext = dbContext;
        _azureBlobStorageHelper = azureBlobStorageHelper;
    }

    public async Task<string> ImportQPDataByExcel(Stream excelStream, IFormFile file)
    {
        try
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
                    var newInstitution = new Institution { Name = institutionCode, Code = institutionCode, Email = "", MobileNumber = "" };
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
                    departments.Add(new Department() { Name = departmentName, ShortName = departmentShortName });

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
                string examMonth = GetCellValue(workbookPart, cells[11]).Trim();
                string examYear = GetCellValue(workbookPart, cells[12]).Trim();

                var institution = updatedInstitutions.FirstOrDefault(d => d.Code == institutionCode);
                var course = updatedCourses.FirstOrDefault(d => d.Code == courseCode);
                var department = updatedDepartments.FirstOrDefault(d => d.Name == departmentName);
                var degreeType = degreeTypes.FirstOrDefault(d => d.Name == degreeTypeName);


                courseDepartMents.Add(new CourseDepartment()
                {
                    InstitutionId = (institution != null) ? institution.InstitutionId : 1,
                    RegulationYear = regulation,
                    BatchYear = batch,
                    DegreeTypeId = (degreeType != null) ? degreeType.DegreeTypeId : 1,
                    DepartmentId = (department != null) ? department.DepartmentId : 1,
                    ExamType = examType,
                    Semester = string.IsNullOrEmpty(semester) ? 0 : long.Parse(semester),
                    ExamMonth = examMonth,
                    ExamYear = examYear,
                    CourseId = (course != null) ? course.CourseId : 1,
                    StudentCount = string.IsNullOrEmpty(studentCount) ? 0 : long.Parse(studentCount),
                    CreatedById = 1,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedById = 1,
                    ModifiedDate = DateTime.UtcNow,
                    IsActive = true,
                });
            }
            var existingCourseDepartments = _dbContext.CourseDepartments.ToList();
            var newCourseDepartments = new HashSet<CourseDepartment>();
            foreach (var courseDepartment in courseDepartMents)
            {
                if (!existingCourseDepartments.Any(x => x.InstitutionId == courseDepartment.InstitutionId && x.RegulationYear == courseDepartment.RegulationYear && x.BatchYear == courseDepartment.BatchYear && x.DegreeTypeId == courseDepartment.DegreeTypeId && x.DepartmentId == courseDepartment.DepartmentId && x.ExamType == courseDepartment.ExamType && x.Semester == courseDepartment.Semester && x.ExamMonth == courseDepartment.ExamMonth && x.ExamYear == courseDepartment.ExamYear && x.CourseId == courseDepartment.CourseId))
                {
                    AuditHelper.SetAuditPropertiesForInsert(courseDepartment, 1);
                    newCourseDepartments.Add(courseDepartment);
                }
            }
            await _dbContext.CourseDepartments.AddRangeAsync(newCourseDepartments);
            await _dbContext.SaveChangesAsync();
            long? documentId = await _azureBlobStorageHelper.UploadFileAsync(excelStream, file.FileName, file.ContentType);

            _dbContext.ImportHistories.Add(new ImportHistory
            {
                DocumentId = documentId ?? 0,
                TotalCount = courseDepartMents.Count,
                CoursesCount = newCourses.Count,
                DepartmentsCount = newDepartments.Count,
                InstitutionsCount = newInstitutions.Count,
                UserId = 1,
                CreatedById = 1,
                CreatedDate = DateTime.UtcNow,
                ModifiedById = 1,
                ModifiedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _dbContext.SaveChangesAsync();
            return ($"Imported successfully! \n Course Count is " + newCourses.Count + " \n Departmets Count is " + newDepartments.Count);
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

    public async Task<List<ImportHistoryVM>> GetImportHistories()
    {
        var documents = await _dbContext.Documents.ToListAsync();
        var users = await _dbContext.Users.ToListAsync();
        var importHistories = await _dbContext.ImportHistories
        .Select(i => new ImportHistoryVM
        {
            ImportHistoryId = i.ImportHistoryId,
            DocumentId = i.DocumentId,
            TotalCount = i.TotalCount,
            CoursesCount = i.CoursesCount,
            DepartmentsCount = i.DepartmentsCount,
            InstitutionsCount = i.InstitutionsCount,
            UserId = i.UserId,
            CreatedDate = i.CreatedDate
        }).ToListAsync();

        foreach (var importHistory in importHistories)
        {
            importHistory.UserName = users.FirstOrDefault(u => u.UserId == importHistory.UserId)?.Name ?? string.Empty;
            importHistory.DocumentName = documents.FirstOrDefault(d => d.DocumentId == importHistory.DocumentId)?.Name ?? string.Empty;
            importHistory.DocumentUrl = documents.FirstOrDefault(d => d.DocumentId == importHistory.DocumentId)?.Url ?? string.Empty;
        }
        return importHistories;
    }

    public async Task<string> ImportSyllabusDocuments(List<IFormFile> files)
    {
        var documentMissingCourses = "Syllabus documents are missing for ";
        var courses = _dbContext.Courses.ToList();
        var courseSyllabusDocuments = _dbContext.CourseSyllabusDocuments.ToList();
        files.ForEach(file =>
        {
            var documentId = _azureBlobStorageHelper.UploadFileAsync(file.OpenReadStream(), file.FileName, file.ContentType);
            var courseId = courses.FirstOrDefault(c => file.FileName.Contains(c.Code))?.CourseId;
            if (courseId != null)
            {
                if (!courseSyllabusDocuments.Any(cs => cs.CourseId == courseId.Value))
                {
                    var courseSyllabusDocument = new CourseSyllabusDocument
                    {
                        CourseId = courseId.Value,
                        DocumentId = documentId.Result,
                    };
                    AuditHelper.SetAuditPropertiesForInsert(courseSyllabusDocument, 1);
                    _dbContext.CourseSyllabusDocuments.AddAsync(courseSyllabusDocument);
                }
                else
                {
                    var existingCourseSyllabusDocument = _dbContext.CourseSyllabusDocuments.FirstOrDefault(cs => cs.CourseId == courseId.Value);
                    if (existingCourseSyllabusDocument != null)
                    {
                        existingCourseSyllabusDocument.DocumentId = documentId.Result;
                        AuditHelper.SetAuditPropertiesForUpdate(existingCourseSyllabusDocument, 1);
                    }
                }
            }
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.Courses.ToList().ForEach(c =>
         {
             if (!_dbContext.CourseSyllabusDocuments.Any(cs => cs.CourseId == c.CourseId))
             {
                 documentMissingCourses += c.Code + ", ";
             }
         });

        return documentMissingCourses;
    }

    public async Task<bool> ImportQPDocuments(List<IFormFile> files, List<QPDocumentValidationVM> qPDocumentValidationVMs)
    {
        var courseDepartments = _dbContext.CourseDepartments.ToList();
        var institutions = _dbContext.Institutions.ToList();
        var qpDocuments = _dbContext.QPDocuments.ToList();
        files.ForEach(file =>
        {
            var documentId = _azureBlobStorageHelper.UploadFileAsync(file.OpenReadStream(), file.FileName, file.ContentType).Result;
            var institution = institutions.FirstOrDefault(i => file.FileName.Split('_')[0].Equals(i.Code));
            var regulation = file.FileName.Split('_')[1];
            var degreeTypeName = file.FileName.Split('_')[2];
            var documetTypeId = file.FileName.Split('_')[3].Equals("QP.docx") ? 2 : 3;
            if (institution != null)
            {
                if (qpDocuments.Any(qp => qp.InstitutionId == institution.InstitutionId && qp.RegulationYear == regulation && qp.DegreeTypeName == degreeTypeName && qp.DocumentTypeId == documetTypeId))
                {
                    var existingQPDocument = _dbContext.QPDocuments.FirstOrDefault(qp => qp.InstitutionId == institution.InstitutionId && qp.RegulationYear == regulation && qp.DegreeTypeName == degreeTypeName && qp.DocumentTypeId == documetTypeId);
                    if (existingQPDocument != null)
                    {
                        existingQPDocument.DocumentId = documentId;
                        AuditHelper.SetAuditPropertiesForUpdate(existingQPDocument, 1);
                    }
                }
                else
                {
                    var qpDocument = new QPDocument
                    {
                        InstitutionId = institution.InstitutionId,
                        RegulationYear = regulation,
                        DegreeTypeName = degreeTypeName,
                        DocumentTypeId = documetTypeId,
                        DocumentId = documentId,
                        QPDocumentName= file.FileName.Trim().Split('.')[0]
                    };
                    AuditHelper.SetAuditPropertiesForInsert(qpDocument, 1);
                    _dbContext.QPDocuments.Add(qpDocument);
                }
            }
        });
        await _dbContext.SaveChangesAsync();
        var qPDocuments = _dbContext.QPDocuments.ToList();
        foreach (var qpDocument in qPDocuments) {
            if (qpDocument.DocumentTypeId != 3) continue;
            if(!_dbContext.QPDocumentBookMarks.Any(qpb=>qpb.QPDocumentId == qpDocument.QPDocumentId))
            {
                var qpSelectedDocument = _dbContext.Documents.FirstOrDefault(d => d.DocumentId == qpDocument.DocumentId);
                if (qpSelectedDocument != null)
                {
                    Aspose.Words.Document sourceDoc = _azureBlobStorageHelper.DownloadWordDocumentFromBlob(qpSelectedDocument.Name).Result;
                    // Iterate through all bookmarks in the source document
                    var bookmarkNames = sourceDoc.Range.Bookmarks.Select(b => b.Name).ToList();
                    var qpDocumentBookMarks = new List<QPDocumentBookMark>();
                    foreach (Aspose.Words.Bookmark bookmark in sourceDoc.Range.Bookmarks)
                    {
                        var qPDocumentBookMark = new QPDocumentBookMark
                        {
                            QPDocumentId = qpDocument.QPDocumentId,
                            BookMarkName = bookmark.Name,
                            BookMarkText = ConvertToBase64(ConvertBookmarkRangeToHtml(bookmark.BookmarkStart, bookmark.BookmarkEnd))
                        };
                        AuditHelper.SetAuditPropertiesForInsert(qPDocumentBookMark, 1);
                        qpDocumentBookMarks.Add(qPDocumentBookMark);
                    }
                    _dbContext.QPDocumentBookMarks.AddRange(qpDocumentBookMarks);
                }
            }
        }
        await _dbContext.SaveChangesAsync();
        return true;
    }
    private static string ConvertToBase64(string text)
    {
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(textBytes);
    }
    private static string ConvertBookmarkRangeToHtml(Node startNode, Node endNode)
    {
        // Create a temporary document with just the bookmark range
        Document tempDoc = new Document();
        DocumentBuilder builder = new DocumentBuilder(tempDoc);

        //Node currentNode = startNode;
        Node currentNode = startNode.NextSibling;
        while (currentNode != null && currentNode != endNode)
        {
            Node importedNode = tempDoc.ImportNode(currentNode, true, ImportFormatMode.KeepSourceFormatting);
            builder.InsertNode(importedNode);
            //tempDoc.FirstSection.Body.AppendChild(importedNode);
            currentNode = currentNode.NextSibling;
        }
        Aspose.Words.Saving.HtmlSaveOptions htmlSaveOptions = new Aspose.Words.Saving.HtmlSaveOptions();
        htmlSaveOptions.ExportImagesAsBase64 = true;
        htmlSaveOptions.ExportHeadersFootersMode = Aspose.Words.Saving.ExportHeadersFootersMode.PerSection;
        htmlSaveOptions.SaveFormat = Aspose.Words.SaveFormat.Html;
        htmlSaveOptions.PrettyFormat = true;

        // Convert the document range to HTML
        using (MemoryStream ms = new MemoryStream())
        {
            tempDoc.Save(ms, htmlSaveOptions);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
