﻿using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.QPSettings;
using AutoMapper;
using SKCE.Examination.Services.Helpers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Spire.Doc;
using Document = Spire.Doc.Document;
using Spire.Doc.Documents;
using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
//using Syncfusion.DocIO.DLS;
//using Syncfusion.DocIO;
using Spire.Doc.Collections;
using Spire.Doc.Fields;
//using Syncfusion.Licensing;
using Spire.Xls.Core.Spreadsheet;
using BookmarksNavigator = Spire.Doc.Documents.BookmarksNavigator;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using DocumentFormat.OpenXml;
using static Spire.Xls.Core.Spreadsheet.HTMLOptions;
using DocumentFormat.OpenXml.Wordprocessing;
using Paragraph = Spire.Doc.Documents.Paragraph;
namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService
    {
        private readonly ExaminationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly BookmarkProcessor _bookmarkProcessor;
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
        private readonly string _containerName;
        private static readonly Dictionary<long, string> QPDocumentTypeDictionary = new Dictionary<long, string>
        {
            { 1, "Course syllabus document" },
            { 2, "Preview QP document for print" },
            { 3, "Preview QP Answer document for print" },
            { 4, "For QP Generation" },
            { 5, "Generated QP" },
            { 6, "For QP Scrutiny" },
            { 7, "Scrutinized QP" },
            { 8, "For QP Selection" },
            { 9, "Selected QP" }
        };
        public QpTemplateService(ExaminationDbContext context, IMapper mapper, IConfiguration configuration, BookmarkProcessor bookmarkProcessor, AzureBlobStorageHelper azureBlobStorageHelper, EmailService emailService)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _bookmarkProcessor = bookmarkProcessor;
            _context = context;
            _emailService = emailService;
            _azureBlobStorageHelper = azureBlobStorageHelper;
            _configuration = configuration;
        }
        public async Task<QPTemplateVM?> GetQPTemplateByCourseIdAsync(long courseId)
        {
            var documents = _context.Documents.ToList();
            var qPTemplate = await _context.Courses.Where(cd => cd.CourseId == courseId)
                .Select(c => new QPTemplateVM
                {
                    CourseId = c.CourseId,
                    CourseCode = c.Code,
                    CourseName = c.Name,
                    QPTemplateId = 0,
                    QPTemplateName = "",
                    QPCode = "",
                    QPTemplateStatusTypeId = 1,
                }).FirstOrDefaultAsync();

            if (qPTemplate == null) return null;
            qPTemplate.QPDocuments = new List<QPDocumentVM>();
            qPTemplate.QPTemplateStatusTypeName = _context.QPTemplateStatusTypes.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
            var courseDepartments = _context.Examinations.Where(cd => cd.CourseId == courseId).ToList();
            if (courseDepartments.Any())
            {
                qPTemplate.RegulationYear = courseDepartments.First().RegulationYear;
                qPTemplate.BatchYear = courseDepartments.First().BatchYear;
                qPTemplate.ExamYear = courseDepartments.First().ExamYear;
                qPTemplate.ExamMonth = courseDepartments.First().ExamMonth;
                qPTemplate.ExamType = courseDepartments.First().ExamType;
                qPTemplate.Semester = courseDepartments.First().Semester;
                qPTemplate.DegreeTypeId = courseDepartments.First().DegreeTypeId;
                qPTemplate.StudentCount = courseDepartments.Sum(cd => cd.StudentCount);
            }
            qPTemplate.DegreeTypeName = _context.DegreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
            qPTemplate.QPTemplateName = $"{qPTemplate.CourseCode} for {qPTemplate.RegulationYear}-{qPTemplate.ExamYear}-{qPTemplate.ExamMonth}-{qPTemplate.DegreeTypeName}-{qPTemplate.ExamType}";

            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);

            var qpTemplateId = _context.QPTemplates.FirstOrDefault(qp => qp.CourseId == courseId
                            && qp.RegulationYear == qPTemplate.RegulationYear
                            && qp.BatchYear == qPTemplate.BatchYear
                            && qp.ExamYear == qPTemplate.ExamYear
                            && qp.ExamMonth == qPTemplate.ExamMonth
                            && qp.ExamType == qPTemplate.ExamType
                            && qp.DegreeTypeId == qPTemplate.DegreeTypeId)?.QPTemplateId ?? 0;


            var syllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(d => d.CourseId == qPTemplate.CourseId);
            if (syllabusDocument != null)
            {
                qPTemplate.CourseSyllabusDocumentId = syllabusDocument.DocumentId;
                qPTemplate.CourseSyllabusDocumentName = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.DocumentId)?.Name ?? string.Empty;
                qPTemplate.CourseSyllabusDocumentUrl = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.DocumentId)?.Url ?? string.Empty;
            }
            else
            {
                qPTemplate = await ProcessExcelAndGeneratePdfAsync(qPTemplate);
            }
            if (qpTemplateId > 0)
                return await GetQPTemplateAsync(qpTemplateId);
            List<long> institutionIds = courseDepartments.Select(cd => cd.InstitutionId).Distinct().ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();

            foreach (var institutionId in institutionIds)
            {
                var institution = institutions.FirstOrDefault(i => i.InstitutionId == institutionId);
                if (institution != null)
                {
                    var qpDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear == qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId == 2 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    var qpAkDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear == qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId == 3 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    if (qpAkDocument != null)
                    {
                        var qpDocumentForGeneration = new QPDocumentVM
                        {
                            QPDocumentId = qpAkDocument.QPDocumentId,
                            InstitutionId = qpAkDocument.InstitutionId,
                            QPDocumentName = qpAkDocument.QPDocumentName,
                            QPOnlyDocumentId = qpDocument?.QPDocumentId ?? 0,
                        };
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { InstitutionId = institutionId, QPTemplateId = 0, UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { InstitutionId = institutionId, QPTemplateId = 0, UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                        qPTemplate.QPDocuments.Add(qpDocumentForGeneration);
                    }
                }
            }

            return qPTemplate;
        }
        public async Task<QPTemplateVM> ProcessExcelAndGeneratePdfAsync(QPTemplateVM qPTemplate)
        {
            var pdfFileName = qPTemplate.CourseCode + ".pdf";
            var docFileName = qPTemplate.CourseCode + ".docx";

            var documentId = _context.CourseSyllabusMasters.FirstOrDefault()?.DocumentId;
            var syllabusDocumentMaster = _context.Documents.FirstOrDefault(d => d.DocumentId == documentId);

            // 🔹 Step 1: Download Excel from Azure Blob Storage
            byte[] excelData = await DownloadFileFromBlobAsync(syllabusDocumentMaster.Name);

            // 🔹 Step 2: Read Specific Row Data from Excel
            var rowData = ReadSpecificRowFromExcel(excelData, qPTemplate.CourseCode);

            if (rowData.Count == 0) return qPTemplate;

            // 3. Replace bookmarks in Word document
            var (updatedPdfPath, updatedwordPath) = ReplaceBookmarks(rowData, qPTemplate);

            // 🔹 Step 5: Upload PDF to Azure Blob Storage
            long syllabuspdfDocumentId = await UploadFileToBlobAsync(pdfFileName, updatedPdfPath);

            // 🔹 Step 6: Upload PDF to Azure Blob Storage
            long syllabusDocumentId = await UploadFileToBlobAsync(docFileName, updatedwordPath);

            var courseSyllabusDocuments = _context.CourseSyllabusDocuments.ToList();
            if (!courseSyllabusDocuments.Any(cs => cs.CourseId == qPTemplate.CourseId))
            {
                var courseSyllabusDocument = new CourseSyllabusDocument
                {
                    CourseId = qPTemplate.CourseId,
                    DocumentId = syllabuspdfDocumentId,
                    WordDocumentId = syllabusDocumentId
                };
                AuditHelper.SetAuditPropertiesForInsert(courseSyllabusDocument, 1);
                await _context.CourseSyllabusDocuments.AddAsync(courseSyllabusDocument);
                await _context.SaveChangesAsync();
                qPTemplate.CourseSyllabusDocumentId = courseSyllabusDocument.DocumentId;
            }
            else
            {
                var existingCourseSyllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(cs => cs.CourseId == qPTemplate.CourseId);
                if (existingCourseSyllabusDocument != null)
                {
                    existingCourseSyllabusDocument.DocumentId = syllabuspdfDocumentId;
                    existingCourseSyllabusDocument.WordDocumentId = syllabusDocumentId;
                    AuditHelper.SetAuditPropertiesForUpdate(existingCourseSyllabusDocument, 1);
                    qPTemplate.CourseSyllabusDocumentId = existingCourseSyllabusDocument.DocumentId;
                }
                await _context.SaveChangesAsync();
            }
            var documents = _context.Documents.Where(d => d.DocumentId == syllabuspdfDocumentId);
            qPTemplate.CourseSyllabusDocumentName = documents.FirstOrDefault(di => di.DocumentId == syllabuspdfDocumentId)?.Name ?? string.Empty;
            qPTemplate.CourseSyllabusDocumentUrl = documents.FirstOrDefault(di => di.DocumentId == syllabuspdfDocumentId)?.Url ?? string.Empty;
            return qPTemplate;
        }
        private async Task<byte[]> DownloadFileFromBlobAsync(string blobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_azureBlobStorageHelper._connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_azureBlobStorageHelper._containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using (var memoryStream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
        private async Task<long> UploadFileToBlobAsync(string blobName, string filePath)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_azureBlobStorageHelper._connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_azureBlobStorageHelper._containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            using FileStream fileStream = File.OpenRead(filePath);

            await blobClient.UploadAsync(fileStream, overwrite: true);
            // Save file metadata in the database
            var document = new SKCE.Examination.Models.DbModels.Common.Document
            {
                Name = blobClient.Name,
                Url = blobClient.Uri.ToString()
            };
            AuditHelper.SetAuditPropertiesForInsert(document, 1);
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document.DocumentId; // Return the generated Document ID
        }
        private Dictionary<string, string> ReadSpecificRowFromExcel(byte[] excelData, string searchValue)
        {
            Dictionary<string, string> rowData = new Dictionary<string, string>();

            using (var memoryStream = new MemoryStream(excelData))
            {
                Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                workbook.LoadFromStream(memoryStream);

                Spire.Xls.Worksheet sheet = workbook.Worksheets[0]; // Read from first sheet
                int rowCount = sheet.Rows.Length;
                int columnCount = sheet.Columns.Length;

                for (int i = 1; i <= rowCount; i++) // Assuming first row is header
                {
                    string secondColumnValue = sheet.Range[i, 2].Text; // Column 2 = Search Column

                    if (secondColumnValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        for (int j = 1; j <= columnCount; j++)
                        {
                            string columnName = sheet.Range[1, j].Text; // Assuming first row is column names
                            string columnValue = sheet.Range[i, j].Text;

                            if (!string.IsNullOrEmpty(columnName) && !rowData.ContainsKey(columnName))
                            {
                                columnName = columnName.Trim().Replace(" ", "");
                                rowData[columnName] = columnValue;
                            }
                        }
                        break; // Stop searching after finding the first match
                    }
                }
            }

            return rowData;
        }
        private (string, string) ReplaceBookmarks(Dictionary<string, string> rowData, QPTemplateVM qPTemplate)
        {
            Spire.Doc.Document doc = new Spire.Doc.Document();
            var wordTemplatePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), $"SyllabusDocumentTemplate\\{qPTemplate.DegreeTypeName}_Syllabustemplate.docx");
            doc.LoadFromFile(wordTemplatePath);
            foreach (Spire.Doc.Bookmark bookmark in doc.Bookmarks)
            {
                Spire.Doc.Documents.BookmarksNavigator navigator = new Spire.Doc.Documents.BookmarksNavigator(doc);
                navigator.MoveToBookmark(bookmark.Name);
                if (bookmark.Name.Contains("CourseObjectives") || bookmark.Name.Contains("CourseOutcomesHead")
                || bookmark.Name.Contains("CourseOutcomes") || bookmark.Name.Contains("RBT")
                || bookmark.Name.Contains("CourseModule") || bookmark.Name.Contains("CourseHours")
                || bookmark.Name.Contains("TextBook") || bookmark.Name.Contains("WebResources")
                || bookmark.Name.Contains("CourseContent") || bookmark.Name.Contains("ReferenceBook"))
                {
                    if (rowData[bookmark.Name.Remove(bookmark.Name.Length - 1, 1)].Split("[").Length > Convert.ToInt32(bookmark.Name.Substring(bookmark.Name.Length - 1)))
                        navigator.ReplaceBookmarkContent(rowData[bookmark.Name.Remove(bookmark.Name.Length - 1, 1)].Split("[")[Convert.ToInt32(bookmark.Name.Substring(bookmark.Name.Length - 1))].Replace("]", ""), true);
                    else
                        navigator.ReplaceBookmarkContent(string.Empty, true);
                }
                else
                {
                    navigator.ReplaceBookmarkContent(rowData[bookmark.Name], true);
                }
            }

            string updatedFilePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "UpdatedSyllabusDocument.docx");
            doc.Watermark = null;
            doc.ProtectionType = ProtectionType.AllowOnlyReading;
            doc.SaveToFile(updatedFilePath, FileFormat.Docx);
            //// Remove evaluation watermark from the output document By OpenXML
            RemoveTextFromDocx(updatedFilePath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");

            // Convert DOCX to PDF for preview By Syncfusion
            string outputPdfPathBySyncfusion = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "UpdatedSyllabusDocument.pdf");
            //ConvertToPdfBySyncfusion(updatedFilePath, outputPdfPathBySyncfusion);

            return (outputPdfPathBySyncfusion, updatedFilePath);
        }
        private void RemoveTextFromDocx(string filePath, string textToRemove)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                foreach (var textElement in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().ToList())
                {
                    if (textElement.Text.Contains(textToRemove))
                    {
                        textElement.Text = textElement.Text.Replace(textToRemove, ""); // Remove text
                    }
                }

                doc.MainDocumentPart.Document.Save(); // Save changes
            }
        }
        //static void ConvertToPdfBySyncfusion(string docxPath, string pdfPath)
        //{
        //    // Load the Word document
        //    WordDocument document = new WordDocument(docxPath, FormatType.Docx);
        //    //// Convert Word to PDF
        //    Syncfusion.Pdf.PdfDocument pdfDocument = new Syncfusion.Pdf.PdfDocument();
        //    Syncfusion.DocIORenderer.DocIORenderer renderer = new Syncfusion.DocIORenderer.DocIORenderer();
        //    pdfDocument = renderer.ConvertToPDF(document);
        //    //ApplyPdfSecurity(pdfDocument);
        //    // Save the PDF file
        //    pdfDocument.Save(pdfPath);
        //    document.Close();
        //    //OpenPdfInBrowser(pdfPath);
        //    //System.Console.WriteLine("DOCX to PDF conversion By Syncfusion completed.");
        //}
        public async Task<QPTemplate> CreateQpTemplateAsync(QPTemplateVM qPTemplateVM)
        {
            if (qPTemplateVM.QPTemplateId > 0) return await UpdateQpTemplateAsync(qPTemplateVM);
            // Create template
            var qPTemplate = new QPTemplate
            {
                QPTemplateName = qPTemplateVM.QPTemplateName,
                QPCode = qPTemplateVM.QPCode,
                QPTemplateStatusTypeId = qPTemplateVM.QPTemplateStatusTypeId,
                CourseId = qPTemplateVM.CourseId,
                RegulationYear = qPTemplateVM.RegulationYear,
                BatchYear = qPTemplateVM.BatchYear,
                DegreeTypeId = qPTemplateVM.DegreeTypeId,
                ExamYear = qPTemplateVM.ExamYear,
                ExamMonth = qPTemplateVM.ExamMonth,
                ExamType = qPTemplateVM.ExamType,
                Semester = qPTemplateVM.Semester,
                StudentCount = qPTemplateVM.StudentCount,
                CourseSyllabusDocumentId = qPTemplateVM.CourseSyllabusDocumentId.Value
            };
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplateVM.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qPTemplateVM.DegreeTypeId);
            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);
            _context.QPTemplates.Add(qPTemplate);
            await _context.SaveChangesAsync();

            foreach (var i in qPTemplateVM.QPDocuments)
            {
                foreach (var u in i.QPAssignedUsers)
                {
                    if (u.UserId > 0)
                    {
                        // Dictionary of bookmarks and their new values
                        Dictionary<string, string> bookmarkUpdates = new Dictionary<string, string>
                        {
                            { "EXAMMONTH", "" },
                            { "EXAMYEAR", "" },
                            { "EXAMTYPE", "" },
                            { "REGULATIONYEAR", "" },
                            { "PROGRAMME","" },
                            { "SEMESTER", "" },
                            { "COURSECODE", "" },
                            { "COURSETITLE", "" },
                        };
                        // Iterate through all bookmarks in the source document
                        foreach (var bookmark in bookmarkUpdates)
                        {
                            string bookmarkName = bookmark.Key;
                            string bookmarkHtml = string.Empty;
                            if (bookmarkName == "PROGRAMME")
                            {
                                var examinations = _context.Examinations.Where(cd => cd.CourseId == qPTemplate.CourseId).ToList();
                                var departments = _context.Departments.ToList();
                                var departmentVMs = examinations.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId,
                                    (cd, d) => new { cd, d })
                                .Where(cd => cd.cd.InstitutionId == u.InstitutionId)
                                .Select(cd => new
                                {
                                    DepartmentId = cd.d.DepartmentId,
                                    DepartmentName = cd.d.Name,
                                    DepartmentShortName = cd.d.ShortName,
                                }).ToList();

                                var departmentIds = departmentVMs.Select(q => q.DepartmentId).ToList();

                                if (departmentIds.Any() && departmentIds.Count > 2)
                                {
                                    bookmarkHtml = System.String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.ShortName).ToList());
                                }
                                else
                                    bookmarkHtml = System.String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.Name).ToList());
                            }
                            else if (bookmarkName == "COURSECODE")
                            {
                                bookmarkHtml = _context.Courses.FirstOrDefault(c =>
                                c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                            }
                            else if (bookmarkName == "COURSETITLE")
                            {
                                bookmarkHtml = _context.Courses.FirstOrDefault(c =>
                                c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                            }
                            else if (bookmarkName == "SEMESTER")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qPTemplate.QPTemplateId)?.Semester.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMMONTH")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qPTemplate.QPTemplateId)?.ExamMonth.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMYEAR")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qPTemplate.QPTemplateId)?.ExamYear.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMTYPE")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qPTemplate.QPTemplateId)?.ExamType.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "REGULATIONYEAR")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qPTemplate.QPTemplateId)?.RegulationYear.ToString() ?? string.Empty;
                            }
                            bookmarkUpdates[bookmark.Key] = bookmarkHtml;
                        }
                        await AssignTemplateForQPGenerationAsync(u, i, qPTemplate, bookmarkUpdates);
                        var emailUser = await _context.Users.FirstOrDefaultAsync(us => us.UserId == u.UserId);
                        //send email
                        if (emailUser != null)
                        {
                            emailUser.IsActive = true;
                            _emailService.SendEmailAsync(emailUser.Email, "CONFIDENTIAL – Appointment as Question Paper Setter – Sri Krishna Institutions",
                              $"Dear {emailUser.Name}," +
                              $"\n\nGreetings from Sri Krishna Institutions!" +
                              $"\n\nWe are pleased to inform you that you have been appointed as a Question Paper Setter for the upcoming End Semester Theory Examinations scheduled in {qPTemplate.ExamMonth}, {qPTemplate.ExamYear} under the autonomous scheme of our institution." +
                              $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                              $"\n\nDetails regarding the assigned course(s), syllabus and question paper format, are available in the following link. \nPortal Link:  {_configuration["LoginUrl"]} -Kindly login using your email id and OTP received to your mail" +
                              $"\n\nAfter preparing the question paper, kindly re-login using the above link and upload the question paper along with Answer Key within 7 days from the date of receipt of this email." +
                              $"\n\nNote: Use only the question paper format available on the portal. Do not modify or delete any unused rows in the format." +
                              $"\n\nPlease treat this communication as confidential. If you have any queries or require assistance, feel free to contact us." +
                              $"\n\nThank you for your valued contribution." +
                              $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
            return qPTemplate;
        }
        public async Task<QPTemplate?> UpdateQpTemplateAsync(QPTemplateVM qPTemplateVM)
        {
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(q => q.QPTemplateId == qPTemplateVM.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateName = qPTemplateVM.QPTemplateName;
            qpTemplate.QPCode = qPTemplateVM.QPCode;
            qpTemplate.QPTemplateStatusTypeId = qPTemplateVM.QPTemplateStatusTypeId;
            qpTemplate.CourseId = qPTemplateVM.CourseId;
            qpTemplate.RegulationYear = qPTemplateVM.RegulationYear;
            qpTemplate.BatchYear = qPTemplateVM.BatchYear;
            qpTemplate.DegreeTypeId = qPTemplateVM.DegreeTypeId;
            qpTemplate.ExamYear = qPTemplateVM.ExamYear;
            qpTemplate.ExamMonth = qPTemplateVM.ExamMonth;
            qpTemplate.ExamType = qPTemplateVM.ExamType;
            qpTemplate.Semester = qPTemplateVM.Semester;
            qpTemplate.StudentCount = qPTemplateVM.StudentCount;
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplateVM.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qPTemplateVM.DegreeTypeId);
            foreach (var qpDocument in qPTemplateVM.QPDocuments)
            {
                foreach (var assignedUser in qpDocument.QPAssignedUsers)
                {
                    var userQPTemplate = _context.UserQPTemplates.FirstOrDefault(uqp => uqp.UserQPTemplateId == assignedUser.UserQPTemplateId && uqp.IsActive);

                    if (userQPTemplate == null)
                    {
                        // Dictionary of bookmarks and their new values
                        Dictionary<string, string> bookmarkUpdates = new Dictionary<string, string>
                        {
                            { "EXAMMONTH", "" },
                            { "EXAMYEAR", "" },
                            { "EXAMTYPE", "" },
                            { "REGULATIONYEAR", "" },
                            { "PROGRAMME","" },
                            { "SEMESTER", "" },
                            { "COURSECODE", "" },
                            { "COURSETITLE", "" },
                        };
                        // Iterate through all bookmarks in the source document
                        foreach (var bookmark in bookmarkUpdates)
                        {
                            string bookmarkName = bookmark.Key;
                            string bookmarkHtml = string.Empty;
                            if (bookmarkName == "PROGRAMME")
                            {
                                var examinations = _context.Examinations.Where(cd => cd.CourseId == qpTemplate.CourseId).ToList();
                                var departments = _context.Departments.ToList();
                                var departmentVMs = examinations.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId,
                                    (cd, d) => new { cd, d })
                                .Where(cd => cd.cd.InstitutionId == assignedUser.InstitutionId)
                                .Select(cd => new
                                {
                                    DepartmentId = cd.d.DepartmentId,
                                    DepartmentName = cd.d.Name,
                                    DepartmentShortName = cd.d.ShortName,
                                }).ToList();

                                var departmentIds = departmentVMs.Select(q => q.DepartmentId).ToList();

                                if (departmentIds.Any() && departmentIds.Count > 2)
                                {
                                    bookmarkHtml = System.String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.ShortName).ToList());
                                }
                                else
                                    bookmarkHtml = System.String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.Name).ToList());
                            }
                            else if (bookmarkName == "COURSECODE")
                            {
                                bookmarkHtml = _context.Courses.FirstOrDefault(c =>
                                c.CourseId == qpTemplate.CourseId)?.Code ?? string.Empty;
                            }
                            else if (bookmarkName == "COURSETITLE")
                            {
                                bookmarkHtml = _context.Courses.FirstOrDefault(c =>
                                c.CourseId == qpTemplate.CourseId)?.Name ?? string.Empty;
                            }
                            else if (bookmarkName == "SEMESTER")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qpTemplate.QPTemplateId)?.Semester.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMMONTH")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qpTemplate.QPTemplateId)?.ExamMonth.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMYEAR")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qpTemplate.QPTemplateId)?.ExamYear.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "EXAMTYPE")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qpTemplate.QPTemplateId)?.ExamType.ToString() ?? string.Empty;
                            }
                            else if (bookmarkName == "REGULATIONYEAR")
                            {
                                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                                c.QPTemplateId == qpTemplate.QPTemplateId)?.RegulationYear.ToString() ?? string.Empty;
                            }
                            bookmarkUpdates[bookmark.Key] = bookmarkHtml;
                        }

                        var newAssignedUser = new UserQPTemplate
                        {
                            IsQPOnly = assignedUser.IsQPOnly,
                            InstitutionId = assignedUser.InstitutionId,
                            QPTemplateId = assignedUser.QPTemplateId,
                            UserId = assignedUser.UserId,
                            QPTemplateStatusTypeId = 8,
                            QPDocumentId = assignedUser.IsQPOnly ? qpDocument.QPOnlyDocumentId : qpDocument.QPDocumentId
                        };
                        var qpAkDocument = _context.QPDocuments.FirstOrDefault(u => u.QPDocumentId == newAssignedUser.QPDocumentId);
                        var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == qpAkDocument.DocumentId);
                        newAssignedUser.UserQPDocumentId = await GetUpdatedExpertQPDocumentByOpenXML(qpTemplate, newAssignedUser, qpToPrintDocument.Name, newAssignedUser.IsQPOnly ? "QP" : "QPAK", bookmarkUpdates);
                        //newAssignedUser.UserQPDocumentId = qpToPrintDocument.DocumentId;
                        AuditHelper.SetAuditPropertiesForInsert(newAssignedUser, 1);
                        _context.UserQPTemplates.Add(newAssignedUser);
                        _context.SaveChanges();
                        var emailUser = _context.Users.FirstOrDefault(us => us.UserId == assignedUser.UserId);
                        //send email
                        if (emailUser != null)
                        {
                            emailUser.IsActive = true;
                            _emailService.SendEmailAsync(emailUser.Email, "CONFIDENTIAL – Appointment as Question Paper Setter – Sri Krishna Institutions",
                              $"Dear {emailUser.Name}," +
                              $"\n\nGreetings from Sri Krishna Institutions!" +
                              $"\n\nWe are pleased to inform you that you have been appointed as a Question Paper Setter for the upcoming End Semester Theory Examinations scheduled in {qpTemplate.ExamMonth}, {qpTemplate.ExamYear} under the autonomous scheme of our institution." +
                              $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                              $"\n\nDetails regarding the assigned course(s), syllabus and question paper format, are available in the following link. \nPortal Link:  {_configuration["LoginUrl"]} -Kindly login using your email id and OTP received to your mail" +
                              $"\n\nAfter preparing the question paper, kindly re-login using the above link and upload the question paper along with Answer Key within 7 days from the date of receipt of this email." +
                              $"\n\nNote: Use only the question paper format available on the portal. Do not modify or delete any unused rows in the format." +
                              $"\n\nPlease treat this communication as confidential. If you have any queries or require assistance, feel free to contact us." +
                              $"\n\nThank you for your valued contribution." +
                              $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
                        }
                    }

                }
            }
            await _context.SaveChangesAsync();
            return qpTemplate;
        }
        public async Task<List<QPTemplateVM>> GetQPTemplatesAsync(long institutionId)
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            var users = _context.Users.ToList();
            qPTemplates = await _context.QPTemplates.Select(qpt => new QPTemplateVM
            {
                QPTemplateId = qpt.QPTemplateId,
                QPTemplateName = qpt.QPTemplateName,
                QPCode = qpt.QPCode,
                QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
                CourseId = qpt.CourseId,
                RegulationYear = qpt.RegulationYear,
                BatchYear = qpt.BatchYear,
                DegreeTypeId = qpt.DegreeTypeId,
                ExamYear = qpt.ExamYear,
                ExamMonth = qpt.ExamMonth,
                ExamType = qpt.ExamType,
                Semester = qpt.Semester,
                StudentCount = qpt.StudentCount,
            }).ToListAsync();

            foreach (var qPTemplate in qPTemplates)
            {
                qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
                qPTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                qPTemplate.CourseCode = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                qPTemplate.CourseName = courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                var userQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateId == qPTemplate.QPTemplateId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9) && u.IsActive)
                    .Select(u => new UserQPTemplateVM
                    {
                        UserQPTemplateId = u.UserQPTemplateId,
                        UserId = u.UserId,
                        InstitutionId = u.InstitutionId,
                        CourseCode = qPTemplate.CourseCode,
                        CourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                        QPTemplateName = qPTemplate.QPTemplateName,
                        QPDocumentId = u.QPDocumentId,
                        SubmittedQPDocumentId = u.SubmittedQPDocumentId,
                        IsQPOnly = u.IsQPOnly,
                        IsQPSelected = u.IsQPSelected,
                        ParentUserQPTemplateId = u.ParentUserQPTemplateId
                    }).ToList();
                foreach (var userTemplate in userQPGenerateTemplates)
                {
                    userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                    userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                    var submittedDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == userTemplate.SubmittedQPDocumentId);
                    if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                    {
                        var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                        if (qpGenerationDocument != null)
                        {
                            var qpOnlyGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd =>
                                                              qpd.InstitutionId == qpGenerationDocument.InstitutionId
                                                           && qpd.RegulationYear == qpGenerationDocument.RegulationYear
                                                           && qpd.DegreeTypeName == qpGenerationDocument.DegreeTypeName
                                                           && qpd.ExamType == qpGenerationDocument.ExamType
                                                           && qpd.DocumentTypeId == 2);
                            qPTemplate.QPDocuments.Add(new QPDocumentVM
                            {
                                QPDocumentId = qpGenerationDocument.QPDocumentId,
                                InstitutionId = qpGenerationDocument.InstitutionId,
                                QPDocumentName = qpGenerationDocument.QPDocumentName,
                                QPOnlyDocumentId = qpOnlyGenerationDocument?.QPDocumentId ?? 0
                            });
                        }
                    }
                    qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPAssignedUsers.Add(
                        new QPDocumentUserVM
                        {
                            UserQPTemplateId = userTemplate.UserQPTemplateId,
                            IsQPOnly = userTemplate.IsQPOnly,
                            UserId = userTemplate.UserId,
                            UserName = userTemplate.UserName,
                            StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                            StatusTypeName = userTemplate.QPTemplateStatusTypeName,
                            InstitutionId = userTemplate.InstitutionId,
                            ParentUserQPTemplateId = userTemplate.ParentUserQPTemplateId,
                            QPTemplateId = userTemplate.QPTemplateId,
                            SubmittedQPDocumentName = (submittedDocument != null) ? submittedDocument.Name : string.Empty,
                            SubmittedQPDocumentUrl = (submittedDocument != null) ? submittedDocument.Url : string.Empty,
                        });
                }
                // QP Scrutiny template details
                var userQPScrutinyTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateId == qPTemplate.QPTemplateId && (u.QPTemplateStatusTypeId == 10 || u.QPTemplateStatusTypeId == 11) && u.InstitutionId == institutionId && u.IsActive)
                    .Select(u => new UserQPTemplateVM
                    {
                        UserQPTemplateId = u.UserQPTemplateId,
                        UserId = u.UserId,
                        InstitutionId = u.InstitutionId,
                        CourseCode = qPTemplate.CourseCode,
                        CourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                        QPTemplateName = qPTemplate.QPTemplateName,
                        QPDocumentId = u.QPDocumentId,
                        SubmittedQPDocumentId = u.SubmittedQPDocumentId,
                        IsQPOnly = u.IsQPOnly,
                        ParentUserQPTemplateId = u.ParentUserQPTemplateId
                    }).ToList();

                foreach (var userTemplate in userQPScrutinyTemplates)
                {
                    userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                    userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                    var submittedDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == userTemplate.SubmittedQPDocumentId);
                    qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPScrutinityUsers.Add(
                    new QPDocumentUserVM
                    {
                        UserQPTemplateId = userTemplate.UserQPTemplateId,
                        IsQPOnly = userTemplate.IsQPOnly,
                        UserId = userTemplate.UserId,
                        UserName = userTemplate.UserName,
                        StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                        StatusTypeName = userTemplate.QPTemplateStatusTypeName,
                        InstitutionId = userTemplate.InstitutionId,
                        SubmittedQPDocumentId = userTemplate.SubmittedQPDocumentId,
                        ParentUserQPTemplateId = userTemplate.ParentUserQPTemplateId,
                        QPTemplateId = userTemplate.QPTemplateId,
                        IsQPSelected = userTemplate.IsQPSelected,
                        SubmittedQPDocumentName = (submittedDocument != null) ? submittedDocument.Name : string.Empty,
                        SubmittedQPDocumentUrl = (submittedDocument != null) ? submittedDocument.Url : string.Empty,
                    });
                }

                foreach (var qPDocument in qPTemplate.QPDocuments)
                {
                    if (_context.UserQPTemplates.Count(qp => qp.QPTemplateId == qPTemplate.QPTemplateId && (qp.QPTemplateStatusTypeId == 8 || qp.QPTemplateStatusTypeId == 9)) < 2)
                        qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });

                    foreach (var user in qPDocument.QPAssignedUsers.Where(a => a.InstitutionId == institutionId))
                    {
                        if (user.UserId == qPDocument.QPAssignedUsers[0].UserId)
                        {
                            qPTemplate.Expert1Name = user.UserName;
                            qPTemplate.Expert1Status = user.StatusTypeName;
                        }
                        else
                        {
                            qPTemplate.Expert2Name = user.UserName;
                            qPTemplate.Expert2Status = user.StatusTypeName;
                        }
                    }
                }
            }
            return qPTemplates;
        }
        public async Task<QPTemplateVM?> GetQPTemplateAsync(long qpTemplateId)
        {
            try
            {
                var degreeTypes = _context.DegreeTypes.ToList();
                var institutions = _context.Institutions.ToList();
                var departments = _context.Departments.ToList();
                var courses = _context.Courses.ToList();
                var documents = _context.Documents.ToList();
                var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
                var users = _context.Users.ToList();
                var qPTemplates = new List<QPTemplateVM>();
                qPTemplates = await _context.QPTemplates.Where(q => q.QPTemplateId == qpTemplateId).Select(qpt => new QPTemplateVM
                {
                    QPTemplateId = qpt.QPTemplateId,
                    QPTemplateName = qpt.QPTemplateName,
                    QPCode = qpt.QPCode,
                    QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
                    CourseId = qpt.CourseId,
                    RegulationYear = qpt.RegulationYear,
                    BatchYear = qpt.BatchYear,
                    DegreeTypeId = qpt.DegreeTypeId,
                    ExamYear = qpt.ExamYear,
                    ExamMonth = qpt.ExamMonth,
                    ExamType = qpt.ExamType,
                    Semester = qpt.Semester,
                    StudentCount = qpt.StudentCount,
                }).ToListAsync();
                foreach (var qPTemplate in qPTemplates)
                {
                    qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
                    qPTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                    var syllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(d => d.CourseId == qPTemplate.CourseId);
                    if (syllabusDocument != null)
                    {
                        qPTemplate.CourseSyllabusDocumentId = syllabusDocument.WordDocumentId;
                        qPTemplate.CourseSyllabusDocumentName = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.WordDocumentId)?.Name ?? string.Empty;
                        qPTemplate.CourseSyllabusDocumentUrl = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.WordDocumentId)?.Url ?? string.Empty;
                    }
                    else
                    {
                        await ProcessExcelAndGeneratePdfAsync(qPTemplate);
                    }

                    var userQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateId == qPTemplate.QPTemplateId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9) && u.IsActive)
                    .Select(u => new UserQPTemplateVM
                    {
                        UserQPTemplateId = u.UserQPTemplateId,
                        UserId = u.UserId,
                        InstitutionId = u.InstitutionId,
                        CourseCode = qPTemplate.CourseCode,
                        CourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                        QPTemplateName = qPTemplate.QPTemplateName,
                        QPDocumentId = u.QPDocumentId,
                        SubmittedQPDocumentId = u.SubmittedQPDocumentId,
                        IsQPOnly = u.IsQPOnly,
                        IsQPSelected = u.IsQPSelected,
                        ParentUserQPTemplateId = u.ParentUserQPTemplateId
                    }).ToList();
                    foreach (var userTemplate in userQPGenerateTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        var submittedDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == userTemplate.SubmittedQPDocumentId);
                        var finalSelectedQP = _context.SelectedQPDetails.FirstOrDefault(sqp => sqp.UserQPTemplateId == userTemplate.UserQPTemplateId);
                        SKCE.Examination.Models.DbModels.Common.Document finalSelectedQPDocument = null;
                        if (finalSelectedQP != null)
                        {
                            finalSelectedQPDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == finalSelectedQP.FinalQPPrintedWordDocumentId);
                        }
                        if (!qPTemplate.QPDocuments.Any(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId))
                        {
                            var qpGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId);
                            if (qpGenerationDocument != null)
                            {
                                var qpOnlyGenerationDocument = _context.QPDocuments.FirstOrDefault(qpd =>
                                                                  qpd.InstitutionId == qpGenerationDocument.InstitutionId
                                                               && qpd.RegulationYear == qpGenerationDocument.RegulationYear
                                                               && qpd.DegreeTypeName == qpGenerationDocument.DegreeTypeName
                                                               && qpd.ExamType == qpGenerationDocument.ExamType
                                                               && qpd.DocumentTypeId == 2);
                                qPTemplate.QPDocuments.Add(new QPDocumentVM
                                {
                                    QPDocumentId = qpGenerationDocument.QPDocumentId,
                                    InstitutionId = qpGenerationDocument.InstitutionId,
                                    QPDocumentName = qpGenerationDocument.QPDocumentName,
                                    QPOnlyDocumentId = qpOnlyGenerationDocument?.QPDocumentId ?? 0
                                });
                            }
                        }
                        qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPAssignedUsers.Add(
                            new QPDocumentUserVM
                            {
                                UserQPTemplateId = userTemplate.UserQPTemplateId,
                                IsQPOnly = userTemplate.IsQPOnly,
                                UserId = userTemplate.UserId,
                                UserName = userTemplate.UserName,
                                StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                                StatusTypeName = userTemplate.QPTemplateStatusTypeName,
                                InstitutionId = userTemplate.InstitutionId,
                                ParentUserQPTemplateId = userTemplate.ParentUserQPTemplateId,
                                QPTemplateId = userTemplate.QPTemplateId,
                                IsQPSelected = userTemplate.IsQPSelected,
                                SubmittedQPDocumentName = (submittedDocument != null) ? submittedDocument.Name : string.Empty,
                                SubmittedQPDocumentUrl = (submittedDocument != null) ? submittedDocument.Url : string.Empty,
                                FinalQPPrintedWordDocumentId = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.DocumentId : 0,
                                FinalQPPrintedWordDocumentName = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.Name : string.Empty,
                                FinalQPPrintedWordDocumentUrl = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.Url : string.Empty,
                            });
                    }
                    // QP Scrutiny template details
                    var userQPScrutinyTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateId == qPTemplate.QPTemplateId && (u.QPTemplateStatusTypeId == 10 || u.QPTemplateStatusTypeId == 11) && u.IsActive)
                        .Select(u => new UserQPTemplateVM
                        {
                            UserQPTemplateId = u.UserQPTemplateId,
                            UserId = u.UserId,
                            InstitutionId = u.InstitutionId,
                            CourseCode = qPTemplate.CourseCode,
                            CourseName = qPTemplate.CourseName,
                            QPTemplateExamMonth = qPTemplate.ExamMonth,
                            QPTemplateExamYear = qPTemplate.ExamYear,
                            QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                            QPTemplateName = qPTemplate.QPTemplateName,
                            QPDocumentId = u.QPDocumentId,
                            SubmittedQPDocumentId = u.SubmittedQPDocumentId,
                            IsQPOnly = u.IsQPOnly,
                            IsQPSelected = u.IsQPSelected,
                            ParentUserQPTemplateId = u.ParentUserQPTemplateId
                        }).ToList();

                    foreach (var userTemplate in userQPScrutinyTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                        var submittedDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == userTemplate.SubmittedQPDocumentId);
                        var finalSelectedQP = _context.SelectedQPDetails.FirstOrDefault(sqp => sqp.UserQPTemplateId == userTemplate.UserQPTemplateId);
                        SKCE.Examination.Models.DbModels.Common.Document finalSelectedQPDocument = null;
                        if (finalSelectedQP != null)
                        {
                            finalSelectedQPDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == finalSelectedQP.FinalQPPrintedWordDocumentId);
                        }
                        qPTemplate.QPDocuments.FirstOrDefault(qpd => qpd.QPDocumentId == userTemplate.QPDocumentId)?.QPScrutinityUsers.Add(
                        new QPDocumentUserVM
                        {
                            UserQPTemplateId = userTemplate.UserQPTemplateId,
                            IsQPOnly = userTemplate.IsQPOnly,
                            UserId = userTemplate.UserId,
                            UserName = userTemplate.UserName,
                            StatusTypeId = userTemplate.QPTemplateStatusTypeId,
                            StatusTypeName = userTemplate.QPTemplateStatusTypeName,
                            InstitutionId = userTemplate.InstitutionId,
                            SubmittedQPDocumentId = userTemplate.SubmittedQPDocumentId,
                            ParentUserQPTemplateId = userTemplate.ParentUserQPTemplateId,
                            QPTemplateId = userTemplate.QPTemplateId,
                            IsQPSelected = userTemplate.IsQPSelected,
                            SubmittedQPDocumentName = (submittedDocument != null) ? submittedDocument.Name : string.Empty,
                            SubmittedQPDocumentUrl = (submittedDocument != null) ? submittedDocument.Url : string.Empty,
                            FinalQPPrintedWordDocumentId = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.DocumentId : 0,
                            FinalQPPrintedWordDocumentName = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.Name : string.Empty,
                            FinalQPPrintedWordDocumentUrl = (finalSelectedQPDocument != null) ? finalSelectedQPDocument.Url : string.Empty,
                        });
                    }

                    foreach (var qPDocument in qPTemplate.QPDocuments)
                    {
                        if (_context.UserQPTemplates.Count(qp => qp.QPTemplateId == qPTemplate.QPTemplateId && (qp.QPTemplateStatusTypeId == 8 || qp.QPTemplateStatusTypeId == 9)) < 2)
                            qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });

                        foreach (var user in qPDocument.QPAssignedUsers)
                        {
                            if (user.UserId == qPDocument.QPAssignedUsers[0].UserId)
                            {
                                qPTemplate.Expert1Name = user.UserName;
                                qPTemplate.Expert1Status = user.StatusTypeName;
                            }
                            else
                            {
                                qPTemplate.Expert2Name = user.UserName;
                                qPTemplate.Expert2Status = user.StatusTypeName;
                            }
                        }
                    }
                }
                return qPTemplates.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<List<QPTemplateVM>> GetQPTemplateByStatusIdAsync(long statusId)
        {
            var degreeTypes = _context.DegreeTypes.ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
            var users = _context.Users.ToList();
            var qPTemplates = new List<QPTemplateVM>();
            qPTemplates = await _context.QPTemplates.Where(q => q.QPTemplateStatusTypeId == statusId).Select(qpt => new QPTemplateVM
            {
                QPTemplateId = qpt.QPTemplateId,
                QPTemplateName = qpt.QPTemplateName,
                QPCode = qpt.QPCode,
                QPTemplateStatusTypeId = qpt.QPTemplateStatusTypeId,
                CourseId = qpt.CourseId,
                RegulationYear = qpt.RegulationYear,
                BatchYear = qpt.BatchYear,
                DegreeTypeId = qpt.DegreeTypeId,
                ExamYear = qpt.ExamYear,
                ExamMonth = qpt.ExamMonth,
                ExamType = qpt.ExamType,
                Semester = qpt.Semester,
                StudentCount = qpt.StudentCount,
            }).ToListAsync();

            foreach (var qPTemplate in qPTemplates)
            {
                qPTemplate.DegreeTypeName = degreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
                qPTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == qPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
                foreach (var qPDocument in qPTemplate.QPDocuments)
                {
                    if (qPDocument.QPAssignedUsers.Count < 2)
                        qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                }
            }
            return qPTemplates;
        }
        public async Task<List<UserQPTemplateVM>> GetQPTemplatesByUserIdAsync(long userId)
        {
            return await GetUserQPTemplatesAsync(userId);
        }
        public async Task<bool?> AssignTemplateForQPGenerationAsync(QPDocumentUserVM qPDocumentUserVM, QPDocumentVM qPDocument, QPTemplate qPTemplate, Dictionary<string, string> bookmarkUpdates)
        {
            var qpTemplate = _context.QPTemplates.FirstOrDefault(qp => qp.QPTemplateId == qPTemplate.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 2; //QP Generation Allocated
            var qpDocumentId = qPDocumentUserVM.IsQPOnly ? qPDocument.QPOnlyDocumentId : qPDocument.QPDocumentId;

            var userQPTemplate = new UserQPTemplate()
            {
                InstitutionId = qPDocument.InstitutionId,
                QPTemplateId = qpTemplate.QPTemplateId,
                UserId = qPDocumentUserVM.UserId,
                QPTemplateStatusTypeId = 8,//Generation QP InProgress
                QPDocumentId = qPDocumentUserVM.IsQPOnly ? qPDocument.QPOnlyDocumentId : qPDocument.QPDocumentId,
                IsQPOnly = qPDocumentUserVM.IsQPOnly,
                IsQPSelected = false,
            };
            var qpAkDocument = _context.QPDocuments.FirstOrDefault(u => u.QPDocumentId == userQPTemplate.QPDocumentId);
            var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == qpAkDocument.DocumentId);
            userQPTemplate.UserQPDocumentId = await GetUpdatedExpertQPDocumentByOpenXML(qPTemplate, userQPTemplate, qpToPrintDocument.Name, qPDocumentUserVM.IsQPOnly ? "QP" : "QPAK", bookmarkUpdates);
            //userQPTemplate.UserQPDocumentId = qpToPrintDocument.DocumentId;
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
            _context.SaveChanges();
            return true;
        }
        public async Task<(string message, bool inValidForSubmission)> ValidateGeneratedQPAsync(string courseCode, long userQPTemplateId, Document doc)
        {
            string text = doc.GetText();
            string keyword = "COURSE CODE";
            int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                string after = text.Substring(index + keyword.Length).Trim();
                string[] parts = after.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    if (parts[3] != courseCode)
                        return ("Course Code mismatched with document", true);
                }
            }

            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserQPTemplateId == userQPTemplateId);
            if (userQPTemplate == null)
            {
                return ("User QP assignment is missing.", true);
            }
            var qpDocument = await _context.QPDocuments.FirstOrDefaultAsync(qpd => qpd.QPDocumentId == userQPTemplate.QPDocumentId);
            if (qpDocument == null)
            {
                return ("User QP documemt is missing.", true);
            }
            if (qpDocument.DegreeTypeName == "UG")
            {
                var ugValidations = await UGQPValidationAsync(userQPTemplate, doc);
                return (ugValidations.Item1, ugValidations.Item2);
            }
            else if (qpDocument.DegreeTypeName == "PG")
            {
                var pgValidations = await PGQPValidationAsync(userQPTemplate, doc);
                return (pgValidations.Item1, pgValidations.Item2);
            }
            return (string.Empty, true);
        }
        public async Task<string> PreviewGeneratedQP(long userQPTemplateId, long generatedDocumentId)
        {
            var userQPtemplate = _context.UserQPTemplates.FirstOrDefault(u => u.UserQPTemplateId == userQPTemplateId);
            if (userQPtemplate == null) return string.Empty;

            var qPtemplate = _context.QPTemplates.FirstOrDefault(u => u.QPTemplateId == userQPtemplate.QPTemplateId);
            if (qPtemplate == null) return string.Empty;

            var userQPDocument = _context.QPDocuments.FirstOrDefault(u => u.QPDocumentId == userQPtemplate.QPDocumentId);
            if (userQPDocument == null) return string.Empty;

            var qpSelectedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == generatedDocumentId);
            var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == userQPDocument.DocumentId);
            if (qpSelectedDocument == null || qpToPrintDocument == null) return string.Empty;
            return await _bookmarkProcessor.ProcessBookmarksAndPrint(qPtemplate, userQPtemplate, qpSelectedDocument.Name, qpToPrintDocument.Name, false, generatedDocumentId);
        }
        private async Task<(string, bool)> UGQPValidationAsync(UserQPTemplate userQPTemplate, Document doc)
        {
            List<string> validationResults = new List<string>();
            Dictionary<string, int> coMarks = new Dictionary<string, int>();
            Dictionary<string, int> btMarks = new Dictionary<string, int>();

            HashSet<string> allCOs = new HashSet<string> { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" }; // Predefined COs
            HashSet<string> allBTs = new HashSet<string> { "U", "AP", "AN" }; // Predefined BTs

            int totalMarks = 0;
            int expectedMarks = 20; // Expected marks for Part A
            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 2 };

            // Sample marks allocation for each CO-BT combination
            Dictionary<(string CO, string BT), int> marksDistribution = new Dictionary<(string, string), int>();
            Dictionary<string, int> coTotals = new Dictionary<string, int>(); // Total per CO
            Dictionary<string, int> btTotals = new Dictionary<string, int>(); // Total per BT
            int grandTotal = 0; // Total of all marks

            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part A: Question Validation</h2>");
            //htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            //htmlTable.Append("<tr><th>Question</th><th>CO</th><th>BT</th><th>Marks</th></tr>");

            for (int qNo = 1; qNo <= 10; qNo++)
            {
                string questionBookmark = $"Q{qNo}";
                string coBookmark = $"Q{qNo}CO";
                string btBookmark = $"Q{qNo}BT";
                string marksBookmark = $"Q{qNo}MARKS";

                string co = ExtractBookmarkText(doc, coBookmark);
                string bt = ExtractBookmarkText(doc, btBookmark);
                int marks = ExtractMarksFromBookmark(doc, marksBookmark);

                totalMarks += marks;

                var key = (co, bt);
                if (marksDistribution.ContainsKey(key))
                {
                    marksDistribution[key] += marks;
                }
                else
                {
                    marksDistribution[key] = marks;
                }

                // Add row to HTML table
                // htmlTable.Append($"<tr><td>Q{qNo}</td><td>{co}</td><td>{bt}</td><td>{marks}</td></tr>");

                if (!string.IsNullOrEmpty(co) && allCOs.Contains(co))
                {
                    if (!coMarks.ContainsKey(co)) coMarks[co] = 0;
                    coMarks[co] += marks;
                }

                if (!string.IsNullOrEmpty(bt) && allBTs.Contains(bt))
                {
                    if (!btMarks.ContainsKey(bt)) btMarks[bt] = 0;
                    btMarks[bt] += marks;
                }
                // Validations
                if (string.IsNullOrEmpty(co) || !validCOs.Contains(co)) errors.Add($"❌ Q{qNo}  CO is invalid.");
                if (string.IsNullOrEmpty(bt) || !validBTs.Contains(bt)) errors.Add($"❌ Q{qNo}  BT is invalid.");
                if (!validMarks.Contains(marks)) errors.Add($"❌ Q{qNo} Marks should be 2.");

                if (marks != 2) errors.Add($"❌ Q{qNo} Total Marks = {marks} (should be 2).");
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(co))
                    coTotals[co] += marks;
                else
                    coTotals[co] = marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(bt))
                    btTotals[bt] += marks;
                else
                    btTotals[bt] = marks;

                // Update grand total
                grandTotal += marks;
                if (qNo == 10)
                {
                    // Add Total row to HTML table
                    //htmlTable.Append($"<tr><td></td><td></td><td>Total</td><td>{totalMarks}</td></tr>");
                }
            }

            htmlTable.Append("</table>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<th>CO</th>");
            foreach (var bt in allBTs)
            {
                htmlTable.Append($"<th>{bt}</th>");
            }
            htmlTable.Append("<th>Total</th></tr>"); // Add total column

            // Populate table rows
            foreach (var co in allCOs)
            {
                htmlTable.Append("<tr>");
                htmlTable.Append($"<td><b>{co}</b></td>");
                int coTotal = 0;

                foreach (var bt in allBTs)
                {
                    int marks = marksDistribution.ContainsKey((co, bt)) ? marksDistribution[(co, bt)] : 0;
                    coTotal += marks;
                    htmlTable.Append($"<td>{marks}</td>");
                }

                // Append row total
                htmlTable.Append($"<td><b>{coTotal}</b></td>");
                htmlTable.Append("</tr>");
            }

            // Add totals row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<td>Total</td>");

            foreach (var bt in allBTs)
            {
                int btTotal = btTotals.ContainsKey(bt) ? btTotals[bt] : 0;
                htmlTable.Append($"<td>{btTotal}</td>");
            }

            // Append grand total
            htmlTable.Append($"<td><b>{grandTotal}</b></td>");
            htmlTable.Append("</tr>");

            htmlTable.Append("</table>");

            bool isMarksCorrect = totalMarks == expectedMarks;
            string marksValidationMessage = isMarksCorrect
                ? $"✅ Total marks for Part A is correct ({totalMarks}/{expectedMarks})."
                : $"❌ Incorrect marks for Part A ({totalMarks}/{expectedMarks}).";

            var missingCOs = allCOs.Except(coMarks.Keys).ToList();
            var missingBTs = allBTs.Except(btMarks.Keys).ToList();

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            // Missing COs & BTs
            //if (missingCOs.Count > 0)
            //    htmlTable.Append($"<h3 style='color:red;'>Missing COs: {string.Join(", ", missingCOs)}</h3>");
            //if (missingBTs.Count > 0)
            //    htmlTable.Append($"<h3 style='color:red;'>Missing BTs: {string.Join(", ", missingBTs)}</h3>");

            var partBResults = ValidateUGPartB(doc);
            return ($"{htmlTable.ToString()} + {partBResults.Item1}", partBResults.Item2);
        }
        public static (string, bool) ValidateUGPartB(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part B: Question Validation</h2>");
            //htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            //htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 0, 6, 8, 10, 16 };

            // Sample marks allocation for each CO-BT combination
            Dictionary<(string CO, string BT), int> marksDistribution = new Dictionary<(string, string), int>();
            Dictionary<string, int> coTotals = new Dictionary<string, int>(); // Total per CO
            Dictionary<string, int> btTotals = new Dictionary<string, int>(); // Total per BT
            int grandTotal = 0; // Total of all marks


            int totalPartBMarks = 0;
            int expectedPartBMarks = 160;
            // Store question pair details
            List<(int QNo, List<(string CO, string BT, int Marks)> SubQs)> partBData = new();

            for (int qNo = 11; qNo <= 20; qNo++)
            {
                var subQs = new List<(string CO, string BT, int Marks)>();

                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMARKS");
                if (subQ1CO != "Select" && subQ1BT != "Select")
                    subQs.Add((subQ1CO, subQ1BT, subQ1Marks));
                var key = (subQ1CO, subQ1BT);
                if (marksDistribution.ContainsKey(key))
                {
                    marksDistribution[key] += subQ1Marks;
                }
                else
                {
                    marksDistribution[key] = subQ1Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ1CO))
                    coTotals[subQ1CO] += subQ1Marks;
                else
                    coTotals[subQ1CO] = subQ1Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ1BT))
                    btTotals[subQ1BT] += subQ1Marks;
                else
                    btTotals[subQ1BT] = subQ1Marks;

                // Update grand total
                grandTotal += subQ1Marks;

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMARKS");
                if (subQ2CO != "Select" && subQ2BT != "Select")
                    subQs.Add((subQ2CO, subQ2BT, subQ2Marks));

                var key2 = (subQ2CO, subQ2BT);
                if (marksDistribution.ContainsKey(key2))
                {
                    marksDistribution[key2] += subQ2Marks;
                }
                else
                {
                    marksDistribution[key2] = subQ2Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ2CO))
                    coTotals[subQ2CO] += subQ2Marks;
                else
                    coTotals[subQ2CO] = subQ2Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ2BT))
                    btTotals[subQ2BT] += subQ2Marks;
                else
                    btTotals[subQ2BT] = subQ2Marks;

                // Update grand total
                grandTotal += subQ2Marks;

                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartBMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 0, 6, 8, 10 or 16.");
                if (subQ1Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                }

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 0, 6, 8, 10 or 16.");
                if (subQ2Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                }
                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 16) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 16).");
                partBData.Add((qNo, subQs));
                // Add to table
                //htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }
            // Validation for either-or pair rules
            for (int i = 0; i < partBData.Count; i += 2)
            {
                var q1 = partBData[i];
                var q2 = partBData[i + 1];

                bool sameBTCOinQ1 = q1.SubQs.All(sq => sq.CO == q1.SubQs[0].CO && sq.BT == q1.SubQs[0].BT);
                bool sameBTCOinQ2 = q2.SubQs.All(sq => sq.CO == q2.SubQs[0].CO && sq.BT == q2.SubQs[0].BT);

                int totalQ1 = q1.SubQs.Sum(sq => sq.Marks);
                int totalQ2 = q2.SubQs.Sum(sq => sq.Marks);

                if (sameBTCOinQ1)
                {
                    if (totalQ1 != totalQ2)
                    {
                        errors.Add($"❌ Q{q1.QNo} and Q{q2.QNo} should have same total marks ({totalQ1} ≠ {totalQ2}) since BT & CO are same in Q{q1.QNo}.");
                    }
                }
                else
                {
                    var q1Details = q1.SubQs.Select(sq => (sq.CO, sq.BT, sq.Marks)).OrderBy(x => x.CO + x.BT + x.Marks);
                    var q2Details = q2.SubQs.Select(sq => (sq.CO, sq.BT, sq.Marks)).OrderBy(x => x.CO + x.BT + x.Marks);
                    if (!q1Details.SequenceEqual(q2Details))
                    {
                        errors.Add($"❌ Q{q1.QNo} and Q{q2.QNo} must have same BT-CO-Marks combination as they have mixed BT/CO.");
                    }
                }
            }
            //htmlTable.Append("</table>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<th>CO</th>");
            foreach (var bt in validBTs)
            {
                htmlTable.Append($"<th>{bt}</th>");
            }
            htmlTable.Append("<th>Total</th></tr>"); // Add total column

            // Populate table rows
            foreach (var co in validCOs)
            {
                htmlTable.Append("<tr>");
                htmlTable.Append($"<td><b>{co}</b></td>");
                int coTotal = 0;

                foreach (var bt in validBTs)
                {
                    int marks = marksDistribution.ContainsKey((co, bt)) ? marksDistribution[(co, bt)] : 0;
                    coTotal += marks;
                    htmlTable.Append($"<td>{marks}</td>");
                }

                // Append row total
                htmlTable.Append($"<td><b>{coTotal}</b></td>");
                htmlTable.Append("</tr>");
            }

            // Add totals row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<td>Total</td>");

            foreach (var bt in validBTs)
            {
                int btTotal = btTotals.ContainsKey(bt) ? btTotals[bt] : 0;
                htmlTable.Append($"<td>{btTotal}</td>");
            }

            // Append grand total
            htmlTable.Append($"<td><b>{grandTotal}</b></td>");
            htmlTable.Append("</tr>");

            htmlTable.Append("</table>");

            bool isPartBValid = totalPartBMarks == expectedPartBMarks;
            string marksValidationMessage = isPartBValid
                ? $"✅ Total Part B Marks are correct ({totalPartBMarks}/{expectedPartBMarks})."
                : $"❌ Total Part B Marks are incorrect ({totalPartBMarks}/{expectedPartBMarks}).";

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            return (htmlTable.ToString(), errors.Any());
        }
        private async Task<(string message, bool inValidForSubmission)> PGQPValidationAsync(UserQPTemplate userQPTemplate, Document doc)
        {
            List<string> validationResults = new List<string>();
            Dictionary<string, int> coMarks = new Dictionary<string, int>();
            Dictionary<string, int> btMarks = new Dictionary<string, int>();

            HashSet<string> allCOs = new HashSet<string> { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" }; // Predefined COs
            HashSet<string> allBTs = new HashSet<string> { "U", "AP", "AN" }; // Predefined BTs

            int totalMarks = 0;
            int expectedMarks = 20; // Expected marks for Part A
            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 2 };

            // Sample marks allocation for each CO-BT combination
            Dictionary<(string CO, string BT), int> marksDistribution = new Dictionary<(string, string), int>();
            Dictionary<string, int> coTotals = new Dictionary<string, int>(); // Total per CO
            Dictionary<string, int> btTotals = new Dictionary<string, int>(); // Total per BT
            int grandTotal = 0; // Total of all marks

            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part A: Question Validation</h2>");
            //htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            //htmlTable.Append("<tr><th>Question</th><th>CO</th><th>BT</th><th>Marks</th></tr>");

            for (int qNo = 1; qNo <= 10; qNo++)
            {
                string questionBookmark = $"Q{qNo}";
                string coBookmark = $"Q{qNo}CO";
                string btBookmark = $"Q{qNo}BT";
                string marksBookmark = $"Q{qNo}MARKS";

                string co = ExtractBookmarkText(doc, coBookmark);
                string bt = ExtractBookmarkText(doc, btBookmark);
                int marks = ExtractMarksFromBookmark(doc, marksBookmark);
                totalMarks += marks;

                var key = (co, bt);
                if (marksDistribution.ContainsKey(key))
                {
                    marksDistribution[key] += marks;
                }
                else
                {
                    marksDistribution[key] = marks;
                }
                // Add row to HTML table
                //htmlTable.Append($"<tr><td>Q{qNo}</td><td>{co}</td><td>{bt}</td><td>{marks}</td></tr>");

                if (!string.IsNullOrEmpty(co) && allCOs.Contains(co))
                {
                    if (!coMarks.ContainsKey(co)) coMarks[co] = 0;
                    coMarks[co] += marks;
                }

                if (!string.IsNullOrEmpty(bt) && allBTs.Contains(bt))
                {
                    if (!btMarks.ContainsKey(bt)) btMarks[bt] = 0;
                    btMarks[bt] += marks;
                }
                // Validations
                if (string.IsNullOrEmpty(co) || !validCOs.Contains(co)) errors.Add($"❌ Q{qNo}  CO is invalid.");
                if (string.IsNullOrEmpty(bt) || !validBTs.Contains(bt)) errors.Add($"❌ Q{qNo}  BT is invalid.");
                if (!validMarks.Contains(marks)) errors.Add($"❌ Q{qNo} Marks should be 2.");

                if (marks != 2) errors.Add($"❌ Q{qNo} Total Marks = {marks} (should be 2).");

                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(co))
                    coTotals[co] += marks;
                else
                    coTotals[co] = marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(bt))
                    btTotals[bt] += marks;
                else
                    btTotals[bt] = marks;

                // Update grand total
                grandTotal += marks;
                if (qNo == 10)
                {
                    // Add Total row to HTML table
                    // htmlTable.Append($"<tr><td></td><td></td><td>Total</td><td>{totalMarks}</td></tr>");
                }
            }

            htmlTable.Append("</table>");

            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<th>CO</th>");
            foreach (var bt in allBTs)
            {
                htmlTable.Append($"<th>{bt}</th>");
            }
            htmlTable.Append("<th>Total</th></tr>"); // Add total column

            // Populate table rows
            foreach (var co in allCOs)
            {
                htmlTable.Append("<tr>");
                htmlTable.Append($"<td><b>{co}</b></td>");
                int coTotal = 0;

                foreach (var bt in allBTs)
                {
                    int marks = marksDistribution.ContainsKey((co, bt)) ? marksDistribution[(co, bt)] : 0;
                    coTotal += marks;
                    htmlTable.Append($"<td>{marks}</td>");
                }

                // Append row total
                htmlTable.Append($"<td><b>{coTotal}</b></td>");
                htmlTable.Append("</tr>");
            }

            // Add totals row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<td>Total</td>");

            foreach (var bt in allBTs)
            {
                int btTotal = btTotals.ContainsKey(bt) ? btTotals[bt] : 0;
                htmlTable.Append($"<td>{btTotal}</td>");
            }

            // Append grand total
            htmlTable.Append($"<td><b>{grandTotal}</b></td>");
            htmlTable.Append("</tr>");

            htmlTable.Append("</table>");


            bool isMarksCorrect = totalMarks == expectedMarks;
            string marksValidationMessage = isMarksCorrect
                ? $"✅ Total marks for Part A is correct ({totalMarks}/{expectedMarks})."
                : $"❌ Incorrect marks for Part A ({totalMarks}/{expectedMarks}).";

            var missingCOs = allCOs.Except(coMarks.Keys).ToList();
            var missingBTs = allBTs.Except(btMarks.Keys).ToList();

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            //// Missing COs & BTs
            //if (missingCOs.Count > 0)
            //    htmlTable.Append($"<h3 style='color:red;'>Missing COs: {string.Join(", ", missingCOs)}</h3>");
            //if (missingBTs.Count > 0)
            //    htmlTable.Append($"<h3 style='color:red;'>Missing BTs: {string.Join(", ", missingBTs)}</h3>");

            var partBResults = ValidatePGPartB(doc);
            var partCResults = ValidatePGPartC(doc);
            partBResults.Item2 = (partBResults.Item2 == false) ? partCResults.Item2 : partBResults.Item2;
            return ($"{htmlTable.ToString()}  {partBResults.Item1}  {partCResults.Item1}", partBResults.Item2);
        }
        public static (string, bool) ValidatePGPartB(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part B: Question Validation</h2>");
            //htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            //htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 0, 5, 10, 15 };
            // Sample marks allocation for each CO-BT combination
            Dictionary<(string CO, string BT), int> marksDistribution = new Dictionary<(string, string), int>();
            Dictionary<string, int> coTotals = new Dictionary<string, int>(); // Total per CO
            Dictionary<string, int> btTotals = new Dictionary<string, int>(); // Total per BT
            int grandTotal = 0; // Total of all marks

            int totalPartBMarks = 0;
            int expectedPartBMarks = 120;
            // Store question pair details
            List<(int QNo, List<(string CO, string BT, int Marks)> SubQs)> partBData = new();

            for (int qNo = 11; qNo <= 18; qNo++)
            {
                var subQs = new List<(string CO, string BT, int Marks)>();
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMARKS");
                if (subQ1CO != "Select" && subQ1BT != "Select")
                    subQs.Add((subQ1CO, subQ1BT, subQ1Marks));
                var key = (subQ1CO, subQ1BT);
                if (marksDistribution.ContainsKey(key))
                {
                    marksDistribution[key] += subQ1Marks;
                }
                else
                {
                    marksDistribution[key] = subQ1Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ1CO))
                    coTotals[subQ1CO] += subQ1Marks;
                else
                    coTotals[subQ1CO] = subQ1Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ1BT))
                    btTotals[subQ1BT] += subQ1Marks;
                else
                    btTotals[subQ1BT] = subQ1Marks;

                // Update grand total
                grandTotal += subQ1Marks;

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMARKS");
                if (subQ2CO != "Select" && subQ2BT != "Select")
                    subQs.Add((subQ2CO, subQ2BT, subQ2Marks));
                var key2 = (subQ2CO, subQ2BT);
                if (marksDistribution.ContainsKey(key2))
                {
                    marksDistribution[key2] += subQ2Marks;
                }
                else
                {
                    marksDistribution[key2] = subQ2Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ2CO))
                    coTotals[subQ2CO] += subQ2Marks;
                else
                    coTotals[subQ2CO] = subQ2Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ2BT))
                    btTotals[subQ2BT] += subQ2Marks;
                else
                    btTotals[subQ2BT] = subQ2Marks;

                // Update grand total
                grandTotal += subQ2Marks;
                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartBMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 0, 5, 10, or 15.");

                if (subQ1Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                }

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 0, 5, 10, or 15.");
                if (subQ2Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");

                }
                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 15) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 15).");
                partBData.Add((qNo, subQs));
                // Add to table
                //htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

            // Validation for either-or pair rules
            for (int i = 0; i < partBData.Count; i += 2)
            {
                var q1 = partBData[i];
                var q2 = partBData[i + 1];

                bool sameBTCOinQ1 = q1.SubQs.All(sq => sq.CO == q1.SubQs[0].CO && sq.BT == q1.SubQs[0].BT);
                bool sameBTCOinQ2 = q2.SubQs.All(sq => sq.CO == q2.SubQs[0].CO && sq.BT == q2.SubQs[0].BT);

                int totalQ1 = q1.SubQs.Sum(sq => sq.Marks);
                int totalQ2 = q2.SubQs.Sum(sq => sq.Marks);

                if (sameBTCOinQ1)
                {
                    if (totalQ1 != totalQ2)
                    {
                        errors.Add($"❌ Q{q1.QNo} and Q{q2.QNo} should have same total marks ({totalQ1} ≠ {totalQ2}) since BT & CO are same in Q{q1.QNo}.");
                    }
                }
                else
                {
                    var q1Details = q1.SubQs.Select(sq => (sq.CO, sq.BT, sq.Marks)).OrderBy(x => x.CO + x.BT + x.Marks);
                    var q2Details = q2.SubQs.Select(sq => (sq.CO, sq.BT, sq.Marks)).OrderBy(x => x.CO + x.BT + x.Marks);
                    if (!q1Details.SequenceEqual(q2Details))
                    {
                        errors.Add($"❌ Q{q1.QNo} and Q{q2.QNo} must have same BT-CO-Marks combination as they have mixed BT/CO.");
                    }
                }
            }
            //htmlTable.Append("</table>");

            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<th>CO</th>");
            foreach (var bt in validBTs)
            {
                htmlTable.Append($"<th>{bt}</th>");
            }
            htmlTable.Append("<th>Total</th></tr>"); // Add total column

            // Populate table rows
            foreach (var co in validCOs)
            {
                htmlTable.Append("<tr>");
                htmlTable.Append($"<td><b>{co}</b></td>");
                int coTotal = 0;

                foreach (var bt in validBTs)
                {
                    int marks = marksDistribution.ContainsKey((co, bt)) ? marksDistribution[(co, bt)] : 0;
                    coTotal += marks;
                    htmlTable.Append($"<td>{marks}</td>");
                }

                // Append row total
                htmlTable.Append($"<td><b>{coTotal}</b></td>");
                htmlTable.Append("</tr>");
            }

            // Add totals row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<td>Total</td>");

            foreach (var bt in validBTs)
            {
                int btTotal = btTotals.ContainsKey(bt) ? btTotals[bt] : 0;
                htmlTable.Append($"<td>{btTotal}</td>");
            }

            // Append grand total
            htmlTable.Append($"<td><b>{grandTotal}</b></td>");
            htmlTable.Append("</tr>");

            htmlTable.Append("</table>");
            bool isPartBValid = totalPartBMarks == expectedPartBMarks;
            string marksValidationMessage = isPartBValid
                ? $"✅ Total Part B Marks are correct ({totalPartBMarks}/{expectedPartBMarks})."
                : $"❌ Total Part B Marks are incorrect ({totalPartBMarks}/{expectedPartBMarks}).";

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            return (htmlTable.ToString(), errors.Any());
        }
        public static (string, bool) ValidatePGPartC(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part C: Question Validation</h2>");
            //htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            //htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 0, 10, 20 };
            // Sample marks allocation for each CO-BT combination
            Dictionary<(string CO, string BT), int> marksDistribution = new Dictionary<(string, string), int>();
            Dictionary<string, int> coTotals = new Dictionary<string, int>(); // Total per CO
            Dictionary<string, int> btTotals = new Dictionary<string, int>(); // Total per BT
            int grandTotal = 0; // Total of all marks
            int totalPartCMarks = 0;
            int expectedPartCMarks = 20;
            // Store question pair details
            List<(int QNo, List<(string CO, string BT, int Marks)> SubQs)> partCData = new();

            for (int qNo = 19; qNo <= 19; qNo++)
            {
                var subQs = new List<(string CO, string BT, int Marks)>();
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMARKS");
                subQs.Add((subQ1CO, subQ1BT, subQ1Marks));
                var key = (subQ1CO, subQ1BT);
                if (marksDistribution.ContainsKey(key))
                {
                    marksDistribution[key] += subQ1Marks;
                }
                else
                {
                    marksDistribution[key] = subQ1Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ1CO))
                    coTotals[subQ1CO] += subQ1Marks;
                else
                    coTotals[subQ1CO] = subQ1Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ1BT))
                    btTotals[subQ1BT] += subQ1Marks;
                else
                    btTotals[subQ1BT] = subQ1Marks;

                // Update grand total
                grandTotal += subQ1Marks;
                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMARKS");
                subQs.Add((subQ2CO, subQ2BT, subQ2Marks));
                var key2 = (subQ2CO, subQ2BT);
                if (marksDistribution.ContainsKey(key2))
                {
                    marksDistribution[key2] += subQ2Marks;
                }
                else
                {
                    marksDistribution[key2] = subQ2Marks;
                }
                // Update row-wise (CO) totals
                if (coTotals.ContainsKey(subQ2CO))
                    coTotals[subQ2CO] += subQ2Marks;
                else
                    coTotals[subQ2CO] = subQ2Marks;

                // Update column-wise (BT) totals
                if (btTotals.ContainsKey(subQ2BT))
                    btTotals[subQ2BT] += subQ2Marks;
                else
                    btTotals[subQ2BT] = subQ2Marks;

                // Update grand total
                grandTotal += subQ2Marks;
                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartCMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 0, 10, 20.");

                if (subQ1Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                }

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 0, 10, 20.");

                if (subQ2Marks != 0)
                {
                    if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                    if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                }
                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 20) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 20).");
                partCData.Add((qNo, subQs));
                // Add to table
                //htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

            //htmlTable.Append("</table>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<th>CO</th>");
            foreach (var bt in validBTs)
            {
                htmlTable.Append($"<th>{bt}</th>");
            }
            htmlTable.Append("<th>Total</th></tr>"); // Add total column

            // Populate table rows
            foreach (var co in validCOs)
            {
                htmlTable.Append("<tr>");
                htmlTable.Append($"<td><b>{co}</b></td>");
                int coTotal = 0;

                foreach (var bt in validBTs)
                {
                    int marks = marksDistribution.ContainsKey((co, bt)) ? marksDistribution[(co, bt)] : 0;
                    coTotal += marks;
                    htmlTable.Append($"<td>{marks}</td>");
                }

                // Append row total
                htmlTable.Append($"<td><b>{coTotal}</b></td>");
                htmlTable.Append("</tr>");
            }

            // Add totals row
            htmlTable.Append("<tr style='background-color:#f2f2f2; font-weight:bold;'>");
            htmlTable.Append("<td>Total</td>");

            foreach (var bt in validBTs)
            {
                int btTotal = btTotals.ContainsKey(bt) ? btTotals[bt] : 0;
                htmlTable.Append($"<td>{btTotal}</td>");
            }

            // Append grand total
            htmlTable.Append($"<td><b>{grandTotal}</b></td>");
            htmlTable.Append("</tr>");

            htmlTable.Append("</table>");
            bool isPartCValid = totalPartCMarks == expectedPartCMarks;
            string marksValidationMessage = isPartCValid
                ? $"✅ Total Part C Marks are correct ({totalPartCMarks}/{expectedPartCMarks})."
                : $"❌ Total Part C Marks are incorrect ({totalPartCMarks}/{expectedPartCMarks}).";

            htmlTable.Append($"<h3>{marksValidationMessage}</h3>");

            if (errors.Count > 0)
            {
                htmlTable.Append("<h3 style='color:red;'>Validation Errors:</h3>");
                htmlTable.Append("<ul>");
                foreach (var error in errors)
                {
                    htmlTable.Append($"<li>{error}</li>");
                }
                htmlTable.Append("</ul>");
            }
            else
            {
                htmlTable.Append("<h3 style='color:green;'>✅ No validation errors found.</h3>");
            }

            return (htmlTable.ToString(), errors.Any());
        }
        private static string ExtractBookmarkText(Document doc, string bookmarkName)
        {
            Spire.Doc.Bookmark bookmark = doc.Bookmarks[bookmarkName];
            if (bookmark == null) return "Unknown";
            Spire.Doc.BookmarkStart bookmarkStart = bookmark.BookmarkStart;
            Spire.Doc.BookmarkEnd bookmarkEnd = bookmark.BookmarkEnd;

            if (bookmarkStart == null || bookmarkEnd == null)
                return "Unknown";

            if (bookmark != null)
            {
                StringBuilder contentBuilder = new StringBuilder();
                // Get all paragraphs inside the bookmark
                foreach (Paragraph paragraph in bookmark.BookmarkStart.OwnerParagraph.OwnerTextBody.Paragraphs)
                {
                    contentBuilder.AppendLine(paragraph.Text);
                }

                return contentBuilder.ToString().Trim();
            }
            return "Unknown";
        }
        private static int ExtractMarksFromBookmark(Document doc, string bookmarkName)
        {
            Spire.Doc.Bookmark bookmark = doc.Bookmarks[bookmarkName];
            if (bookmark != null && int.TryParse(ExtractBookmarkText(doc, bookmarkName), out int marks))
            {
                return marks;
            }
            return 0; // Default to 0 if marks not found
        }
        public async Task<bool?> SubmitGeneratedQPAsync(long userQPTemplateId, long documentId, QPSubmissionVM qPSubmissionVM)
        {
            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qpti => qpti.UserQPTemplateId == userQPTemplateId);
            if (userQPTemplate == null) return null;
            if (userQPTemplate.QPTemplateStatusTypeId == 10)
            {
                return await SubmitScrutinizedQPAsync(userQPTemplateId, documentId, qPSubmissionVM);
            }
            else
            {
                var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == userQPTemplate.QPTemplateId);
                if (qpTemplate == null) return null;
                qpTemplate.QPTemplateStatusTypeId = 3; //QP Pending for Scrutiny
                AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
                if (userQPTemplate == null) return null;
                userQPTemplate.QPTemplateStatusTypeId = 9;
                userQPTemplate.IsTablesAllowed = qPSubmissionVM.IsTablesAllowed;
                userQPTemplate.TableName = qPSubmissionVM.TableName;
                userQPTemplate.IsGraphsRequired = qPSubmissionVM.IsGraphsRequired;
                userQPTemplate.GraphName = qPSubmissionVM.GraphName;
                userQPTemplate.SubmittedQPDocumentId = documentId;
                AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
                await _context.SaveChangesAsync();
                var document = _context.Documents.FirstOrDefault(d => d.DocumentId == documentId);
                SaveBookmarksToDatabaseByFilePath(document?.Name, userQPTemplateId, documentId);
                //disable user if all assigned QP's are submitted.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userQPTemplate.UserId);
                if (user != null && !_context.UserQPTemplates.Any(u => u.UserId == userQPTemplate.UserId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 10)))
                    user.IsActive = false;
                await _context.SaveChangesAsync();
                var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qpTemplate.CourseId);
                _emailService.SendEmailAsync(user.Email, "Subject-CONFIDENTIAL-Sri Krishna Institutions",
                                  $"Dear {user.Name}," +
                                  $"\n\nWe are expressing our sincere gratitude to you on receiving your QP for the upcoming End Semester Theory Examinations scheduled in {qpTemplate.ExamMonth}, {qpTemplate.ExamYear} under the autonomous scheme of our institution." +
                                  $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                                  $"\n\nYour expertise and contribution in setting a well-structured QP is highly appreciable." +
                                  $"\n\nThank you once again for your time and commitment to academic Excellence." +
                                  $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
                var adminuser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
                _emailService.SendEmailAsync(adminuser.Email, $"Submitted generated QP by {user.Name} for Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}",
                                 $"Dear {adminuser.Name}," +
                                 $"\n\nPlease find attached submitted QP by {user.Name}." +
                                 $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                                 $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.", userQPTemplate.SubmittedQPDocumentId).Wait();
                return true;
            }

        }
        public async Task<bool?> AssignTemplateForQPScrutinyAsync(long userId, long userQPTemplateId)
        {
            var qpUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.UserQPTemplateId == userQPTemplateId && qp.QPTemplateStatusTypeId == 9);
            if (qpUserTemplate == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpUserTemplate.QPTemplateId);

            if (qpTemplate == null) return null;

            var genereatedDocument = _context.Documents.Where(d => d.DocumentId == qpUserTemplate.SubmittedQPDocumentId).FirstOrDefault();
            MemoryStream memoryStreamScrutiny = await _azureBlobStorageHelper.DownloadWordDocumentFromBlobToOpenXML(genereatedDocument.Name);
            memoryStreamScrutiny.Position = 0;
            long scrutinyVersionDocumentId = _azureBlobStorageHelper.UploadDocxSteamFileToBlob(memoryStreamScrutiny, string.Format("{0}_{1}.docx", "For_Scrutiny",genereatedDocument.Name.Replace(".docx", ""))).Result;

            qpTemplate.QPTemplateStatusTypeId = 4; //QP Scrutiny Allocated
            var userQPTemplate = new UserQPTemplate()
            {
                InstitutionId = qpUserTemplate.InstitutionId,
                UserId = userId,
                QPTemplateStatusTypeId = 10,//Scrutinize QP InProgress
                QPDocumentId = qpUserTemplate.QPDocumentId,
                IsQPOnly = qpUserTemplate.IsQPOnly,
                IsTablesAllowed = qpUserTemplate.IsTablesAllowed,
                TableName = qpUserTemplate.TableName,
                IsGraphsRequired = qpUserTemplate.IsGraphsRequired,
                ParentUserQPTemplateId = qpUserTemplate.UserQPTemplateId,
                GraphName = qpUserTemplate.GraphName,
                QPTemplateId = qpUserTemplate.QPTemplateId,
                SubmittedQPDocumentId = scrutinyVersionDocumentId,
                UserQPDocumentId = scrutinyVersionDocumentId
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
            await _context.SaveChangesAsync();
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qpTemplate.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qpTemplate.DegreeTypeId);
            var emailUser = _context.Users.FirstOrDefault(us => us.UserId == userId);
            //send email
            if (emailUser != null)
            {
                emailUser.IsActive = true;
                _emailService.SendEmailAsync(emailUser.Email, "CONFIDENTIAL – Appointment as Question Paper Scrutinizer – Sri Krishna Institutions",
                               $"Dear {emailUser.Name}," +
                               $"\n\nGreetings from Sri Krishna Institutions!" +
                               $"\n\nWe are pleased to inform you that you have been appointed as a Question Paper Scrutinizer for the upcoming End Semester Theory Examinations scheduled in {qpTemplate.ExamMonth}, {qpTemplate.ExamYear} under the autonomous scheme of our institution." +
                               $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                               $"\n\nDetails regarding the assigned course(s), syllabus and question paper and answer key to be scrutinized are available in the following link. \nPortal Link:  {_configuration["LoginUrl"]} -Kindly login using your email id and OTP received to your mail" +
                               $"\n\nAfter scrutinizing the question paper and answer key kindly re-login using the above link and upload the scrutinized question paper along with answer key within 3 days from the date of receipt of this email:" +
                               $"\n\nNote: Use only the question paper and answer Key downloaded from the portal. Do not modify or delete any unused rows in the format." +
                               $"\n\nPlease treat this communication as confidential. If you have any queries or require assistance, feel free to contact us." +
                               $"\n\nThank you for your valued contribution." +
                               $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
            }
            return true;
        }
        public async Task<bool?> SubmitScrutinizedQPAsync(long userQPTemplateId, long documentId, QPSubmissionVM qPSubmissionVM)
        {
            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qpti => qpti.UserQPTemplateId == userQPTemplateId);
            if (userQPTemplate == null) return null;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == userQPTemplate.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 5; //QP Pending for Selection
            AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
            userQPTemplate.QPTemplateStatusTypeId = 11;//Scrutinized QP Submitted
            userQPTemplate.SubmittedQPDocumentId = (documentId == 0) ? userQPTemplate.SubmittedQPDocumentId : documentId;
            AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
            await _context.SaveChangesAsync();
            //disable user if all assigned QP's are submitted.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userQPTemplate.UserId);
            if (user != null && !_context.UserQPTemplates.Any(u => u.UserId == userQPTemplate.UserId && (u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 10)))
                user.IsActive = false;
            await _context.SaveChangesAsync();
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qpTemplate.CourseId);
            _emailService.SendEmailAsync(user.Email, "Subject-CONFIDENTIAL-Sri Krishna Institutions",
                              $"Dear {user.Name}," +
                              $"\n\nWe are expressing our sincere gratitude to you on receiving your QP for the upcoming End Semester Theory Examinations scheduled in {qpTemplate.ExamMonth}, {qpTemplate.ExamYear} under the autonomous scheme of our institution." +
                              $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                              $"\n\nYour expertise and contribution in setting a well-structured QP is highly appreciable." +
                              $"\n\nThank you once again for your time and commitment to academic Excellence." +
                              $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
            var adminuser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
            _emailService.SendEmailAsync(adminuser.Email, $"Submitted generated QP by {user.Name} for Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}",
                             $"Dear {adminuser.Name}," +
                             $"\n\nPlease find attached submitted QP by {user.Name}." +
                             $"\n\nCourse Details\n\n Course Code:{courseDetails.Code}\n Course Name:{courseDetails.Name}" +
                             $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.", userQPTemplate.SubmittedQPDocumentId).Wait();
            return true;
        }
        private async Task<List<UserQPTemplateVM>> GetUserQPTemplatesAsync(long userId)
        {
            try
            {
                var courses = _context.Courses.ToList();
                var documents = _context.Documents.ToList();
                var qpTemplateStatuss = _context.QPTemplateStatusTypes.ToList();
                var userQPTemplateVMs = new List<UserQPTemplateVM>();
                var users = _context.Users.ToList();
                var userQPTemplates = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId && uqp.IsActive).ToList();
                var qpTemplateIds = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId && uqp.IsActive).Select(qpti => qpti.QPTemplateId).ToList();
                var qpTemplates = await _context.QPTemplates.Where(qp => qpTemplateIds.Contains(qp.QPTemplateId)).ToListAsync();
                userQPTemplates.ForEach(userQPTemplate =>
                {

                    var qpTemplate = qpTemplates.FirstOrDefault(p => p.QPTemplateId == userQPTemplate.QPTemplateId);
                    var qpDocument = _context.QPDocuments.FirstOrDefault(qp => qp.QPDocumentId == userQPTemplate.QPDocumentId);
                    var course = courses.FirstOrDefault(c => c.CourseId == qpTemplate.CourseId);

                    var userQPTemplateVm = new UserQPTemplateVM()
                    {
                        UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                        InstitutionId = userQPTemplate.InstitutionId,
                        UserId = userQPTemplate.UserId,
                        QPTemplateName = qpTemplate.QPTemplateName,
                        CourseCode = course.Code,
                        CourseName = course.Name,
                        UserName = users.FirstOrDefault(u => u.UserId == userQPTemplate.UserId)?.Name ?? string.Empty,
                        QPTemplateStatusTypeId = userQPTemplate.QPTemplateStatusTypeId,
                        QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userQPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                        QPDocumentId = userQPTemplate.UserQPDocumentId.Value,
                        QPDocumentName = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.UserQPDocumentId.Value)?.Name ?? string.Empty,
                        QPDocumentUrl = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.UserQPDocumentId.Value)?.Url ?? string.Empty,
                        CourseSyllabusDocumentId = qpTemplate.CourseSyllabusDocumentId,
                        CourseSyllabusDocumentName = documents.FirstOrDefault(d => d.DocumentId == qpTemplate.CourseSyllabusDocumentId)?.Name ?? string.Empty,
                        CourseSyllabusDocumentUrl = documents.FirstOrDefault(d => d.DocumentId == qpTemplate.CourseSyllabusDocumentId)?.Url ?? string.Empty
                    };
                    if (userQPTemplateVm.QPTemplateStatusTypeId == 10 || userQPTemplateVm.QPTemplateStatusTypeId == 11)
                    {
                        userQPTemplateVm.QPDocumentId = userQPTemplate.SubmittedQPDocumentId;
                        userQPTemplateVm.QPDocumentName = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.SubmittedQPDocumentId)?.Name ?? string.Empty;
                        userQPTemplateVm.QPDocumentUrl = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.SubmittedQPDocumentId)?.Url ?? string.Empty;
                    }
                    userQPTemplateVMs.Add(userQPTemplateVm);
                });
                return userQPTemplateVMs;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<string?> PrintSelectedQPAsync(long userqpTemplateId, string qpCode, bool isForPrint)
        {
            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserQPTemplateId == userqpTemplateId);
            if (userQPTemplate == null) return string.Empty;
            var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == userQPTemplate.QPTemplateId);
            if (qpTemplate == null) return string.Empty;
            if (isForPrint)
            {
                Random random = new Random();
                int randomNumber = random.Next(1000, 10000);
                qpCode = randomNumber.ToString();
                qpTemplate.QPTemplateStatusTypeId = 7; //QP Selected
                qpTemplate.QPCode = qpCode;
                AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
                await _context.SaveChangesAsync();
            }
            if (userQPTemplate != null)
            {
                if (isForPrint)
                {
                    userQPTemplate.QPCode = qpCode;
                    userQPTemplate.IsQPSelected = true;
                    qpTemplate.PrintedDocumentId = userQPTemplate.SubmittedQPDocumentId;
                }
                var qpDocument = _context.QPDocuments.FirstOrDefault(u => u.QPDocumentId == userQPTemplate.QPDocumentId);
                var qpSelectedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == userQPTemplate.SubmittedQPDocumentId);
                var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == qpDocument.DocumentId);
                if (qpSelectedDocument == null || qpToPrintDocument == null) return string.Empty;
                var pdfPath = await _bookmarkProcessor.ProcessBookmarksAndPrint(qpTemplate, userQPTemplate, qpSelectedDocument.Name, qpToPrintDocument.Name, isForPrint, userQPTemplate.SubmittedQPDocumentId);

                await _context.SaveChangesAsync();
                return pdfPath;
            }
            return string.Empty;
        }
        public async Task<IEnumerable<QPAssignmentExpertVM>> GetExpertsForQPAssignmentAsync()
        {
            var users = await _context.Users.Where(u => u.IsEnabled && u.UserId != 1).ToListAsync();
            var userQPTemplates = await _context.UserQPTemplates.Where(uqp => uqp.QPTemplateStatusTypeId == 8 || uqp.QPTemplateStatusTypeId == 10).ToListAsync();
            var qpAssignmentExperts = new List<QPAssignmentExpertVM>();
            foreach (var user in users)
            {
                qpAssignmentExperts.Add(new QPAssignmentExpertVM
                {
                    UserId = user.UserId,
                    UserName = $"{user.Name}-{user.CollegeName}-{user.DepartmentName}",
                    IsAvailableForQPAssignment = (userQPTemplates.Where(uqp => uqp.UserId == user.UserId).ToList().Count < 3)
                });
            }
            return qpAssignmentExperts;
        }
        public void SaveBookmarksToDatabaseByFilePath(string fileName, long userqpTemplateId, long documentId)
        {
            Spire.Doc.Document sourceDoc = _azureBlobStorageHelper.DownloadWordDocumentFromBlob(fileName).Result;
            // Iterate through all bookmarks in the source document

            var qpDocumentBookMarks = new List<UserQPDocumentBookMark>();
            foreach (Spire.Doc.Bookmark bookmark in sourceDoc.Bookmarks)
            {
                string bookmarkHtmlBase64 = string.Empty;
                if (!bookmark.Name.StartsWith("Q") || bookmark.Name.Equals("QPCODE")) continue;
                try
                {
                    bookmarkHtmlBase64 = ExtractBookmarkAsHtmlBase64(sourceDoc, bookmark.Name);
                }
                catch (Exception)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(bookmarkHtmlBase64))
                {
                    var existingQpBookMark = _context.UserQPDocumentBookMarks.FirstOrDefault(qpb => qpb.UserQPTemplateId == userqpTemplateId && qpb.BookMarkName == bookmark.Name && qpb.DocumentId == documentId);
                    if (existingQpBookMark == null)
                    {
                        var qPDocumentBookMark = new UserQPDocumentBookMark
                        {
                            UserQPTemplateId = userqpTemplateId,
                            BookMarkName = bookmark.Name,
                            DocumentId = documentId,
                            BookMarkText = bookmarkHtmlBase64
                        };
                        AuditHelper.SetAuditPropertiesForInsert(qPDocumentBookMark, 1);
                        qpDocumentBookMarks.Add(qPDocumentBookMark);
                    }
                    else
                    {
                        existingQpBookMark.BookMarkText = bookmarkHtmlBase64;
                        AuditHelper.SetAuditPropertiesForUpdate(existingQpBookMark, 1);
                    }
                }
            }
            _context.UserQPDocumentBookMarks.AddRange(qpDocumentBookMarks);
            _context.SaveChanges();
        }
        public async Task<long> GetUpdatedExpertQPDocumentByOpenXML(QPTemplate qPTemplate, UserQPTemplate userQPTemplate, string inputDocPath, string qpType, Dictionary<string, string> bookmarkUpdates)
        {
            try
            {
                // Save the updated document
                var updatedSourcePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.docx", "TempDocument", DateTime.Now.ToString("ddMMyyyyhhmmss")));
                MemoryStream sourceStream = await _azureBlobStorageHelper.DownloadWordDocumentFromBlobToOpenXML(inputDocPath);
                sourceStream.Position = 0;
                var courseSyllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId);
                var courseSyllabusWordDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == courseSyllabusDocument.WordDocumentId);
                // Load the template document where bookmarks need to be replaced
                MemoryStream sourceSyllabusStream = await _azureBlobStorageHelper.DownloadWordDocumentFromBlobToOpenXML(courseSyllabusWordDocument.Name);
                using (WordprocessingDocument SyllabusWordDoc = WordprocessingDocument.Open(sourceSyllabusStream, true))
                {
                    var bookmarks = SyllabusWordDoc.MainDocumentPart.RootElement.Descendants<DocumentFormat.OpenXml.Wordprocessing.BookmarkStart>();

                    foreach (var bookmark in bookmarks)
                    {
                        string name = bookmark.Name;
                        string textValue = bookmark.NextSibling<Run>()?.GetFirstChild<Text>()?.Text;

                        if (!string.IsNullOrEmpty(name) && textValue != null)
                        {
                            bookmarkUpdates[name] = textValue;
                        }
                    }
                }
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(sourceStream, true))
                {
                    var body = wordDoc.MainDocumentPart.Document.Body;

                    foreach (var kvp in bookmarkUpdates)
                    {
                        string bookmarkName = kvp.Key;
                        string newText = kvp.Value;

                        DocumentFormat.OpenXml.Wordprocessing.BookmarkStart bookmarkStart = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.BookmarkStart>()
                                                          .FirstOrDefault(b => b.Name == bookmarkName);

                        if (bookmarkStart != null)
                        {
                            // Remove old content between start and end
                            OpenXmlElement currentElement = bookmarkStart.NextSibling();
                            while (currentElement != null &&
                                   !(currentElement is DocumentFormat.OpenXml.Wordprocessing.BookmarkEnd && ((DocumentFormat.OpenXml.Wordprocessing.BookmarkEnd)currentElement).Id == bookmarkStart.Id))
                            {
                                OpenXmlElement nextElement = currentElement.NextSibling();
                                currentElement.Remove();
                                currentElement = nextElement;
                            }

                            // Insert new text
                            Run run = new Run(new Text(newText));
                            bookmarkStart.Parent.InsertAfter(run, bookmarkStart);
                        }
                    }
                    wordDoc.MainDocumentPart.Document.Save();
                }
                sourceStream.Position = 0; // Reset stream before returning
                return _azureBlobStorageHelper.UploadDocxSteamFileToBlob(sourceStream, string.Format("{0}_{1}_{2}_{3}.docx", inputDocPath.Replace(".docx", ""), bookmarkUpdates["COURSECODE"], bookmarkUpdates["EXAMYEAR"], bookmarkUpdates["EXAMMONTH"].Replace("/", ""), qpType)).Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<bool> ProcessSelectedQPBookMarks(long userQPTemplateId)
        {

            var selectedQPDetail = _context.SelectedQPDetails.FirstOrDefault(qp => qp.UserQPTemplateId == userQPTemplateId && !qp.IsQPOnly.Value);
            if (selectedQPDetail == null) return false;
            SaveSelectedQPBookmarksByFilePath(selectedQPDetail);
            return true;
        }
        public void SaveSelectedQPBookmarksByFilePath(SelectedQPDetail selectedQPDetail)
        {
            try
            {
                var fileName = _context.Documents.FirstOrDefault(d => d.DocumentId == selectedQPDetail.QPPrintedWordDocumentId)?.Name;
                Document sourceDoc = _azureBlobStorageHelper.DownloadWordDocumentFromBlob(fileName).Result;
                // Iterate through all bookmarks in the source document
                var qpDocumentBookMarks = new List<SelectedQPBookMarkDetail>();
                foreach (Spire.Doc.Bookmark bookmark in sourceDoc.Bookmarks)
                {
                    string bookmarkHtmlBase64 = string.Empty;
                    if (!bookmark.Name.StartsWith("Q") || bookmark.Name.Equals("QPCODE")) continue;
                    try
                    {
                        bookmarkHtmlBase64 = ExtractBookmarkAsHtmlBase64(sourceDoc, bookmark.Name);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(bookmarkHtmlBase64))
                    {
                        var existingQpBookMark = _context.SelectedQPBookMarkDetails.FirstOrDefault(qpb => qpb.SelectedQPDetailId == selectedQPDetail.SelectedQPDetailId && qpb.BookMarkName == bookmark.Name);
                        if (existingQpBookMark == null)
                        {
                            var qPDocumentBookMark = new SelectedQPBookMarkDetail
                            {
                                SelectedQPDetailId = selectedQPDetail.SelectedQPDetailId,
                                BookMarkName = bookmark.Name,
                                BookMarkText = bookmarkHtmlBase64
                            };
                            AuditHelper.SetAuditPropertiesForInsert(qPDocumentBookMark, 1);
                            qpDocumentBookMarks.Add(qPDocumentBookMark);
                        }
                        else
                        {
                            existingQpBookMark.BookMarkText = bookmarkHtmlBase64;
                            AuditHelper.SetAuditPropertiesForUpdate(existingQpBookMark, 1);
                        }
                    }
                }
                _context.SelectedQPBookMarkDetails.AddRange(qpDocumentBookMarks);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string ExtractBookmarkAsHtmlBase64(Document doc, string bookmarkName)
        {
            //// Load the Word documentDocument
            //Document doc = new Document();
            //doc.LoadFromFile(filePath);

            // Find the bookmark  
            BookmarksNavigator navigator = new BookmarksNavigator(doc);
            navigator.MoveToBookmark(bookmarkName);

            // Extract the content inside the bookmark as a document fragment  
            Spire.Doc.Documents.TextBodyPart content = navigator.GetBookmarkContent();

            // Create a new temporary document to hold this content  
            Document tempDoc = new Document();
            Section section = tempDoc.AddSection();

            foreach (DocumentObject item in content.BodyItems)
            {
                section.Body.ChildObjects.Add(item.Clone());
            }

            // Force inline styles and embedded images manually via options (not passed to stream save)
            var options = new HtmlExportOptions
            {
                CssStyleSheetType = Spire.Doc.CssStyleSheetType.Inline,
                ImageEmbedded = true
            };
            string htmlContent = string.Empty;
            // Save to file first to apply options (required for options to take effect)
            tempDoc.SaveToFile("temp.html", FileFormat.Html);

            if (bookmarkName.Contains("IMG"))
            {
                htmlContent = LoadHtmlWithEmbeddedResources("temp.html", Path.GetDirectoryName("temp.html"));
            }
            else
            {
                // Read file back to stream or string (simulate SaveToStream with options)
                htmlContent = File.ReadAllText("temp.html");
            }
            htmlContent = htmlContent.Replace("Evaluation Warning: The document was created with Spire.Doc for .NET.", "");
            var base64Html = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent));

            return base64Html;
        }
        public async Task<List<SelectedQPBookMarkDetail>> GetQPAKDetails(SelectedQPDetailVM selectedQPDetailVM, string questionNumber)
        {
            var selectedQPDetail = _context.SelectedQPDetails.Where(qp =>
            qp.InstitutionId == selectedQPDetailVM.InstitutionId &&
            qp.CourseId == selectedQPDetailVM.CourseId &&
            qp.RegulationYear == selectedQPDetailVM.RegulationYear &&
            qp.BatchYear == selectedQPDetailVM.BatchYear &&
            qp.DegreeTypeId == selectedQPDetailVM.DegreeTypeId &&
            qp.ExamType == selectedQPDetailVM.ExamType &&
            qp.Semester == selectedQPDetailVM.Semester &&
            qp.ExamMonth == selectedQPDetailVM.ExamMonth &&
            qp.ExamYear == selectedQPDetailVM.ExamYear && !qp.IsQPOnly.Value).OrderByDescending(d => d.QPPrintedDate).FirstOrDefault();

            if (selectedQPDetail == null) return new List<SelectedQPBookMarkDetail>();
            List<string> questionBookMarks = new List<string>();
            questionBookMarks.Add(questionNumber);
            questionBookMarks.Add($"{questionNumber}IMG");
            questionBookMarks.Add($"{questionNumber}MARKS");
            questionBookMarks.Add($"{questionNumber}AK");
            var selectedQPBookMarks = await _context.SelectedQPBookMarkDetails
                .Where(qpb => qpb.SelectedQPDetailId == selectedQPDetail.SelectedQPDetailId && questionBookMarks.Contains(qpb.BookMarkName))
                .ToListAsync();
            return selectedQPBookMarks;
        }
        public async Task<List<SelectedQPBookMarkDetail>> GetAllQPAKDetails(SelectedQPDetailVM selectedQPDetailVM)
        {
            var selectedQPDetail = _context.SelectedQPDetails.Where(qp =>
            qp.InstitutionId == selectedQPDetailVM.InstitutionId &&
            qp.CourseId == selectedQPDetailVM.CourseId &&
            qp.RegulationYear == selectedQPDetailVM.RegulationYear &&
            qp.BatchYear == selectedQPDetailVM.BatchYear &&
            qp.DegreeTypeId == selectedQPDetailVM.DegreeTypeId &&
            qp.ExamType == selectedQPDetailVM.ExamType &&
            qp.Semester == selectedQPDetailVM.Semester &&
            qp.ExamMonth == selectedQPDetailVM.ExamMonth &&
            qp.ExamYear == selectedQPDetailVM.ExamYear && !qp.IsQPOnly.Value).OrderByDescending(d => d.QPPrintedDate).FirstOrDefault();

            if (selectedQPDetail == null) return new List<SelectedQPBookMarkDetail>();
            var selectedQPBookMarks = await _context.SelectedQPBookMarkDetails
                .Where(qpb => qpb.SelectedQPDetailId == selectedQPDetail.SelectedQPDetailId)
                .ToListAsync();
            return selectedQPBookMarks;
        }
        public static string LoadHtmlWithEmbeddedResources(string htmlFilePath, string basePath)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(htmlFilePath);

            // Embed CSS files
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes)
                {
                    var href = link.GetAttributeValue("href", null);
                    if (href != null)
                    {
                        var cssPath = Path.Combine(basePath, href);
                        if (File.Exists(cssPath))
                        {
                            var cssContent = File.ReadAllText(cssPath);
                            var styleNode = HtmlNode.CreateNode($"<style>{cssContent}</style>");
                            link.ParentNode.ReplaceChild(styleNode, link);
                        }
                    }
                }
            }

            // Embed images as base64
            var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img[@src]");
            if (imgNodes != null)
            {
                foreach (var img in imgNodes)
                {
                    var src = img.GetAttributeValue("src", null);
                    if (src != null)
                    {
                        var imgPath = Path.Combine(basePath, src);
                        if (File.Exists(imgPath))
                        {
                            var imgBytes = File.ReadAllBytes(imgPath);
                            var base64 = Convert.ToBase64String(imgBytes);
                            var ext = Path.GetExtension(imgPath).TrimStart('.');
                            var mime = GetMimeType(ext);

                            img.SetAttributeValue("src", $"data:{mime};base64,{base64}");
                        }
                    }
                }
            }

            using (var sw = new StringWriter())
            {
                htmlDoc.Save(sw);
                return sw.ToString();
            }
        }
        private static string GetMimeType(string ext)
        {
            return ext.ToLower() switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}
