using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;
using AutoMapper;
using SKCE.Examination.Services.Helpers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Spire.Doc;
using Document = Spire.Doc.Document;
using Spire.Doc.Documents;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Syncfusion.Pdf;
using System.Buffers;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIO;
using DocumentFormat.OpenXml.Bibliography;
namespace SKCE.Examination.Services.QPSettings
{
    public class QpTemplateService 
    {
        private readonly ExaminationDbContext _context;
        private readonly IMapper _mapper;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly BookmarkProcessor _bookmarkProcessor;
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
        private readonly BlobServiceClient _blobServiceClient;
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
            _blobServiceClient = new BlobServiceClient(connectionString);
            _bookmarkProcessor = bookmarkProcessor;
            _context = context;
            _mapper = mapper;
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
                    var qpDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear== qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId ==2 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    var qpAkDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == institution.InstitutionId && d.RegulationYear == qPTemplate.RegulationYear && d.DegreeTypeName == qPTemplate.DegreeTypeName && d.DocumentTypeId == 3 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
                    if(qpAkDocument != null)
                    {
                        var qpDocumentForGeneration = new QPDocumentVM
                        {
                            QPDocumentId = qpAkDocument.QPDocumentId,
                            InstitutionId = qpAkDocument.InstitutionId,
                            QPDocumentName = qpAkDocument.QPDocumentName,
                            QPOnlyDocumentId = qpDocument?.QPDocumentId??0,
                        };
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { InstitutionId= institutionId,QPTemplateId =0, UserQPTemplateId =0,IsQPOnly=false,StatusTypeId=8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                        qpDocumentForGeneration.QPAssignedUsers.Add(new QPDocumentUserVM { InstitutionId = institutionId, QPTemplateId = 0, UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName="", UserId = 0, UserName = string.Empty });
                        qPTemplate.QPDocuments.Add(qpDocumentForGeneration);
                    }
                }
            }

            return qPTemplate;
        }

        public async Task<QPTemplateVM> ProcessExcelAndGeneratePdfAsync(QPTemplateVM qPTemplate)
        {
            var pdfFileName = qPTemplate.CourseCode + ".pdf";

            var documentId = _context.courseSyllabusMasters.FirstOrDefault()?.DocumentId;
            var syllabusDocumentMaster = _context.Documents.FirstOrDefault(d => d.DocumentId == documentId);

            // 🔹 Step 1: Download Excel from Azure Blob Storage
            byte[] excelData = await DownloadFileFromBlobAsync(syllabusDocumentMaster.Name);

            // 🔹 Step 2: Read Specific Row Data from Excel
            var rowData = ReadSpecificRowFromExcel(excelData, qPTemplate.CourseCode);

            if (rowData.Count == 0)
            {
                throw new Exception($"No row found with value '{qPTemplate.CourseCode}' in the second column.");
            }

            // 3. Replace bookmarks in Word document
            string updatedPdfPath = ReplaceBookmarks(rowData);

            // 🔹 Step 5: Upload PDF to Azure Blob Storage
            long syllabusDocumentId =  await UploadFileToBlobAsync(pdfFileName, updatedPdfPath);

            var courseSyllabusDocuments = _context.CourseSyllabusDocuments.ToList();
            if (!courseSyllabusDocuments.Any(cs => cs.CourseId == qPTemplate.CourseId))
            {
                var courseSyllabusDocument = new CourseSyllabusDocument
                {
                    CourseId = qPTemplate.CourseId,
                    DocumentId = syllabusDocumentId,
                };
                AuditHelper.SetAuditPropertiesForInsert(courseSyllabusDocument, 1);
               await _context.CourseSyllabusDocuments.AddAsync(courseSyllabusDocument);
            }
            else
            {
                var existingCourseSyllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(cs => cs.CourseId == qPTemplate.CourseId);
                if (existingCourseSyllabusDocument != null)
                {
                    existingCourseSyllabusDocument.DocumentId = syllabusDocumentId;
                    AuditHelper.SetAuditPropertiesForUpdate(existingCourseSyllabusDocument, 1);
                }
            }
            await _context.SaveChangesAsync();
            var documents = _context.Documents.Where(d => d.DocumentId == syllabusDocumentId);
            qPTemplate.CourseSyllabusDocumentId = syllabusDocumentId;
            qPTemplate.CourseSyllabusDocumentName = documents.FirstOrDefault(di => di.DocumentId == syllabusDocumentId)?.Name ?? string.Empty;
            qPTemplate.CourseSyllabusDocumentUrl = documents.FirstOrDefault(di => di.DocumentId == syllabusDocumentId)?.Url ?? string.Empty;
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

        private async Task<long> UploadFileToBlobAsync(string blobName,string filePath)
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
                                columnName = columnName.Trim().Replace(" ","");
                                rowData[columnName] = columnValue;
                            }
                        }
                        break; // Stop searching after finding the first match
                    }
                }
            }

            return rowData;
        }
        private string ReplaceBookmarks(Dictionary<string, string> rowData)
        {
           Spire.Doc.Document doc = new Spire.Doc.Document();
            var wordTemplatePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "SyllabusDocumentTemplate\\Syllabus_sample template.doc");
            doc.LoadFromFile(wordTemplatePath);
           

            foreach (Spire.Doc.Bookmark bookmark in doc.Bookmarks)
            {
                Spire.Doc.Documents.BookmarksNavigator navigator = new Spire.Doc.Documents.BookmarksNavigator(doc);
                navigator.MoveToBookmark(bookmark.Name);
                switch (bookmark.Name)
                {
                    case "CourseObjectives1":
                    case "CourseObjectives2":
                    case "CourseObjectives3":
                    case "CourseObjectives4":
                    case "CourseObjectives5":
                    case "CourseObjectives6":
                    case "CourseOutcomesHead1":
                    case "CourseOutcomesHead2":
                    case "CourseOutcomesHead3":
                    case "CourseOutcomesHead4":
                    case "CourseOutcomesHead5":
                    case "CourseOutcomesHead6":
                    case "CourseOutcomes1":
                    case "CourseOutcomes2":
                    case "CourseOutcomes3":
                    case "CourseOutcomes4":
                    case "CourseOutcomes5":
                    case "CourseOutcomes6":
                    case "RBT1":
                    case "RBT2":
                    case "RBT3":
                    case "RBT4":
                    case "RBT5":
                    case "RBT6":
                        navigator.ReplaceBookmarkContent(rowData[bookmark.Name.Remove(bookmark.Name.Length - 1, 1)].Split("[")[Convert.ToInt32(bookmark.Name.Substring(bookmark.Name.Length - 1))].Replace("]",""), true);
                        break;
                    default:
                        navigator.ReplaceBookmarkContent(rowData[bookmark.Name], true);
                        break;
                }
                
            }

            string updatedFilePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "UpdatedSyllabusDocument.docx");
            doc.SaveToFile(updatedFilePath, FileFormat.Docx);
            //// Remove evaluation watermark from the output document By OpenXML
            RemoveTextFromDocx(updatedFilePath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");

            // Convert DOCX to PDF for preview By Syncfusion
            string outputPdfPathBySyncfusion = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "UpdatedSyllabusDocument.pdf");
            ConvertToPdfBySyncfusion(updatedFilePath, outputPdfPathBySyncfusion);

            return outputPdfPathBySyncfusion;
        }
        private void RemoveTextFromDocx(string filePath, string textToRemove)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                foreach (var textElement in body.Descendants<Text>().ToList())
                {
                    if (textElement.Text.Contains(textToRemove))
                    {
                        textElement.Text = textElement.Text.Replace(textToRemove, ""); // Remove text
                    }
                }

                doc.MainDocumentPart.Document.Save(); // Save changes
            }
        }
        static void ConvertToPdfBySyncfusion(string docxPath, string pdfPath)
        {
            // Load the Word document
            WordDocument document = new WordDocument(docxPath, FormatType.Docx);
            //// Convert Word to PDF
            Syncfusion.Pdf.PdfDocument pdfDocument = new Syncfusion.Pdf.PdfDocument();
            Syncfusion.DocIORenderer.DocIORenderer renderer = new Syncfusion.DocIORenderer.DocIORenderer();
            pdfDocument = renderer.ConvertToPDF(document);
            //ApplyPdfSecurity(pdfDocument);
            // Save the PDF file
            pdfDocument.Save(pdfPath);
            document.Close();
            //OpenPdfInBrowser(pdfPath);
            //System.Console.WriteLine("DOCX to PDF conversion By Syncfusion completed.");
        }
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
                StudentCount = qPTemplateVM.StudentCount
            };
            var courseDetails = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplateVM.CourseId);
            var degreeType = _context.DegreeTypes.FirstOrDefault(c => c.DegreeTypeId == qPTemplateVM.DegreeTypeId);
            AuditHelper.SetAuditPropertiesForInsert(qPTemplate, 1);
            _context.QPTemplates.Add(qPTemplate);
            await _context.SaveChangesAsync();
            qPTemplateVM.QPDocuments.ForEach(i =>
            {
                    i.QPAssignedUsers.ForEach(u =>
                    {
                        if (u.UserId > 0)
                        {
                            AssignTemplateForQPGenerationAsync(u, i, qPTemplate);
                            var emailUser = _context.Users.FirstOrDefault(us => us.UserId == u.UserId);
                            //send email
                            if (emailUser != null)
                            {
                                _emailService.SendEmailAsync(emailUser.Email, "Question Paper Assignment Notification – Sri Krishna Institutions, Coimbatore",
                                  $"Dear {emailUser.Name}," +
                                  $"\n\nYou have been assigned to generate a Question Paper for the following course:" +
                                  $"\n\n Course:{courseDetails.Code} - {courseDetails.Name} \n Degree Type: {degreeType.Name}" +
                                  $"\n\nPlease review the assigned question paper and submit it before the due date." +
                                  $"\n To View Assignment please click here {_configuration["LoginUrl"]}" +
                                  $"\n\nContact Details:\nName:\nContact Number:\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution.\n\nWarm regards,\nSri Krishna College of Engineering and Technology").Wait();
                            }
                        }
                    });
            });
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
                        var newAssignedUser = new UserQPTemplate
                        {
                            IsQPOnly = assignedUser.IsQPOnly,
                            InstitutionId = assignedUser.InstitutionId,
                            QPTemplateId = assignedUser.QPTemplateId,
                            UserId = assignedUser.UserId,
                            QPTemplateStatusTypeId = 8,
                            QPDocumentId = assignedUser.IsQPOnly ? qpDocument.QPOnlyDocumentId : qpDocument.QPDocumentId
                        };
                        AuditHelper.SetAuditPropertiesForInsert(newAssignedUser, 1);
                        _context.UserQPTemplates.Add(newAssignedUser);
                        _context.SaveChanges();
                        var emailUser = _context.Users.FirstOrDefault(us => us.UserId == assignedUser.UserId);
                        //send email
                        if (emailUser != null)
                        {
                            _emailService.SendEmailAsync(emailUser.Email, "Question Paper Assignment Notification – Sri Krishna Institutions, Coimbatore",
                              $"Dear {emailUser.Name}," +
                              $"\n\nYou have been assigned to generate a Question Paper for the following course:" +
                              $"\n\n Course:{courseDetails.Code} - {courseDetails.Name} \n Degree Type: {degreeType.Name} \n\n" +
                              $"\n\nPlease review the assigned question paper and submit it before the due date." +
                              $"\n To View Assignment please click here {_configuration["LoginUrl"]}" +
                              $"\n\nContact Details:\nName:\nContact Number:\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution.\n\nWarm regards,\nSri Krishna College of Engineering and Technology").Wait();
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
                        QPTemplateCourseCode = qPTemplate.CourseCode,
                        QPTemplateCourseName = qPTemplate.CourseName,
                        QPTemplateExamMonth = qPTemplate.ExamMonth,
                        QPTemplateExamYear = qPTemplate.ExamYear,
                        QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                        QPTemplateName = qPTemplate.QPTemplateName,
                        QPDocumentId = u.QPDocumentId,
                        IsQPOnly = u.IsQPOnly,
                    }).ToList();
                foreach (var userTemplate in userQPGenerateTemplates)
                {
                    userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                    userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
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
                            InstitutionId = userTemplate.InstitutionId
                        });
                }

                foreach (var qPDocument in qPTemplate.QPDocuments)
                {
                    if (qPDocument.QPAssignedUsers.Count < 2)
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
                     var userQPGenerateTemplates = _context.UserQPTemplates.Where(u => u.QPTemplateId == qPTemplate.QPTemplateId &&(u.QPTemplateStatusTypeId == 8 || u.QPTemplateStatusTypeId == 9) && u.IsActive)
                    .Select(u => new UserQPTemplateVM
                    {
                        UserQPTemplateId = u.UserQPTemplateId,
                                UserId = u.UserId,
                                InstitutionId = u.InstitutionId,
                                QPTemplateCourseCode = qPTemplate.CourseCode,
                                QPTemplateCourseName = qPTemplate.CourseName,
                                QPTemplateExamMonth = qPTemplate.ExamMonth,
                                QPTemplateExamYear = qPTemplate.ExamYear,
                                QPTemplateStatusTypeId = u.QPTemplateStatusTypeId,
                                QPTemplateName = qPTemplate.QPTemplateName,
                                QPDocumentId = u.QPDocumentId,
                                IsQPOnly = u.IsQPOnly,
                            }).ToList();
                    var syllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(d => d.CourseId == qPTemplate.CourseId);
                    if (syllabusDocument != null)
                    {
                        qPTemplate.CourseSyllabusDocumentId = syllabusDocument.DocumentId;
                        qPTemplate.CourseSyllabusDocumentName = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.DocumentId)?.Name ?? string.Empty;
                        qPTemplate.CourseSyllabusDocumentUrl = documents.FirstOrDefault(di => di.DocumentId == syllabusDocument.DocumentId)?.Url ?? string.Empty;
                    }
                    else
                    {
                        await ProcessExcelAndGeneratePdfAsync(qPTemplate);
                    }
                    foreach (var userTemplate in userQPGenerateTemplates)
                    {
                        userTemplate.UserName = users.FirstOrDefault(us => us.UserId == userTemplate.UserId)?.Name ?? string.Empty;
                        userTemplate.QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty;
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
                                InstitutionId = userTemplate.InstitutionId
                            });
                    }
                    foreach (var qPDocument  in qPTemplate.QPDocuments)
                    {
                        if(qPDocument.QPAssignedUsers.Count <2)
                            qPDocument.QPAssignedUsers.Add(new QPDocumentUserVM { UserQPTemplateId = 0, IsQPOnly = false, StatusTypeId = 8, StatusTypeName = "", UserId = 0, UserName = string.Empty });
                        foreach (var user in qPDocument.QPAssignedUsers)
                        {
                            if(user.UserId == qPDocument.QPAssignedUsers[0].UserId)
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
        public bool? AssignTemplateForQPGenerationAsync(QPDocumentUserVM qPDocumentUserVM,QPDocumentVM qPDocument,QPTemplate qPTemplate)
        {
            var qpTemplate =  _context.QPTemplates.FirstOrDefault(qp => qp.QPTemplateId == qPTemplate.QPTemplateId);
            if (qpTemplate == null) return null;
            qpTemplate.QPTemplateStatusTypeId = 2; //QP Generation Allocated

            var userQPTemplate = new UserQPTemplate()
            {
                InstitutionId = qPDocument.InstitutionId,
                QPTemplateId= qpTemplate.QPTemplateId,
                UserId = qPDocumentUserVM.UserId,
                QPTemplateStatusTypeId = 8,//Generation QP InProgress
                QPDocumentId = qPDocumentUserVM.IsQPOnly? qPDocument.QPOnlyDocumentId: qPDocument.QPDocumentId,
                IsQPOnly = qPDocumentUserVM.IsQPOnly
            };
            AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
            _context.UserQPTemplates.Add(userQPTemplate);
             _context.SaveChanges();
            return true;
        }
        public async Task<(string message, bool inValidForSubmission)> ValidateGeneratedQPAndPreview(long userId, long institutionId, Document doc) {
            var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.InstitutionId == institutionId);
            if (userQPTemplate == null)
            {
                return ("User QP assignment is missing.", true);
            }
            var qpDocument = await _context.QPDocuments.FirstOrDefaultAsync(qpd => qpd.QPDocumentId == userQPTemplate.QPDocumentId);
            if (qpDocument == null)
            {
                return ("User QP documemt is missing.",true);
            }
            if (qpDocument.DegreeTypeName == "UG")
            {
                var ugValidations = await UGQPValidationAsync(userQPTemplate, doc);
                  return (ugValidations.Item1, ugValidations.Item2) ;
            }
            else if (qpDocument.DegreeTypeName == "PG")
            {
                var pgValidations = await PGQPValidationAsync(userQPTemplate, doc);
                return (pgValidations.Item1, pgValidations.Item2);
            }
            return (string.Empty,true);
        }
        public async Task<bool> PreviewGeneratedQP(long userId, long InstitutionId, long generatedDocumentId)
        {
            //var qpTemplate = _context.QPTemplates.FirstOrDefault(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
            //if (qpTemplate == null) return false;
            //var institution = _context.Institutions.FirstOrDefault(i => i.InstitutionId == qpTemplateInstitution.InstitutionId);
            //var documentToPrint = await _context.QPTemplateInstitutionDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 2 && qptd.QPTemplateInstitutionId == QPTemplateInstitutionId);
            //var qpSelectedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == generatedDocumentId);
            //var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentToPrint.DocumentId);
            //if (qpSelectedDocument == null || qpToPrintDocument == null) return false;
            //await _bookmarkProcessor.ProcessBookmarksAndPrint(qpTemplate, qpTemplateInstitution, qpSelectedDocument.Name, qpToPrintDocument.Name, false);
            return true;
        }
        private async Task<(string,bool)> UGQPValidationAsync(UserQPTemplate userQPTemplate, Document doc)
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

            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part A: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>CO</th><th>BT</th><th>Marks</th></tr>");

            for (int qNo = 1; qNo <= 10; qNo++)
            {
                string questionBookmark = $"Q{qNo}";
                string coBookmark = $"Q{qNo}CO";
                string btBookmark = $"Q{qNo}BT";
                string marksBookmark = $"Q{qNo}Marks";

                string co = ExtractBookmarkText(doc, coBookmark);
                string bt = ExtractBookmarkText(doc, btBookmark);
                int marks = ExtractMarksFromBookmark(doc, marksBookmark);

                totalMarks += marks;

                // Add row to HTML table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{co}</td><td>{bt}</td><td>{marks}</td></tr>");

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

                if (totalMarks != 2) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 2).");

                if (qNo == 10)
                {
                    // Add Total row to HTML table
                    htmlTable.Append($"<tr><td></td><td></td><td>Total</td><td>{totalMarks}</td></tr>");
                }
            }

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
            // CO Marks Distribution
            htmlTable.Append("<h2>CO Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>CO</th><th>Marks</th></tr>");
            foreach (var kvp in coMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // BT Marks Distribution
            htmlTable.Append("<h2>BT Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>BT</th><th>Marks</th></tr>");
            foreach (var kvp in btMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // Missing COs & BTs
            if (missingCOs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing COs: {string.Join(", ", missingCOs)}</h3>");
            if (missingBTs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing BTs: {string.Join(", ", missingBTs)}</h3>");

            var partBResults = ValidateUGPartB(doc);
            return ($"{htmlTable.ToString()} +\n\n {partBResults.Item1}", partBResults.Item2);
        }
        public static (string,bool) ValidateUGPartB(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part B: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 6, 8, 10 };

            int totalPartBMarks = 0;
            int expectedPartBMarks = 160;

            for (int qNo = 11; qNo <= 20; qNo++)
            {
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMarks");

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMarks");

                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartBMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 6, 8, or 10.");

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 6, 8, or 10.");

                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 16) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 16).");

                // Add to table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

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

            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part A: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>CO</th><th>BT</th><th>Marks</th></tr>");

            for (int qNo = 1; qNo <= 10; qNo++)
            {
                string questionBookmark = $"Q{qNo}";
                string coBookmark = $"Q{qNo}CO";
                string btBookmark = $"Q{qNo}BT";
                string marksBookmark = $"Q{qNo}Marks";

                string co = ExtractBookmarkText(doc, coBookmark);
                string bt = ExtractBookmarkText(doc, btBookmark);
                int marks = ExtractMarksFromBookmark(doc, marksBookmark);

                totalMarks += marks;

                // Add row to HTML table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{co}</td><td>{bt}</td><td>{marks}</td></tr>");

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

                if (totalMarks != 2) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 2).");

                if (qNo == 10)
                {
                    // Add Total row to HTML table
                    htmlTable.Append($"<tr><td></td><td></td><td>Total</td><td>{totalMarks}</td></tr>");
                }
            }

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
            // CO Marks Distribution
            htmlTable.Append("<h2>CO Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>CO</th><th>Marks</th></tr>");
            foreach (var kvp in coMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // BT Marks Distribution
            htmlTable.Append("<h2>BT Marks Distribution</h2><table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'><tr><th>BT</th><th>Marks</th></tr>");
            foreach (var kvp in btMarks)
            {
                htmlTable.Append($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
            }
            htmlTable.Append("</table>");

            // Missing COs & BTs
            if (missingCOs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing COs: {string.Join(", ", missingCOs)}</h3>");
            if (missingBTs.Count > 0)
                htmlTable.Append($"<h3 style='color:red;'>Missing BTs: {string.Join(", ", missingBTs)}</h3>");

            var partBResults = ValidatePGPartB(doc);
            var partCResults = ValidatePGPartC(doc);
            partBResults.Item2 = (partBResults.Item2 == false)?partCResults.Item2:partBResults.Item2;
            return ($"{htmlTable.ToString()} +\n\n {partBResults.Item1} +\n\n {partCResults.Item1}", partBResults.Item2);
        }
        public static (string, bool) ValidatePGPartB(Document doc)
        {
            StringBuilder htmlTable = new StringBuilder();
            htmlTable.Append("<h2>Part B: Question Validation</h2>");
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 6, 8, 10 };

            int totalPartBMarks = 0;
            int expectedPartBMarks = 120;

            for (int qNo = 11; qNo <= 18; qNo++)
            {
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMarks");

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMarks");

                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartBMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 6, 8, or 10.");

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 6, 8, or 10.");

                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 16) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 16).");

                // Add to table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

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
            htmlTable.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            htmlTable.Append("<tr><th>Question</th><th>SubQ1 Text</th><th>SubQ1 CO</th><th>SubQ1 BT</th><th>SubQ1 Marks</th><th>SubQ2 Text</th><th>SubQ2 CO</th><th>SubQ2 BT</th><th>SubQ2 Marks</th><th>Total Marks</th></tr>");

            List<string> errors = new List<string>();
            HashSet<string> validBTs = new() { "U", "AP", "AN" };
            HashSet<string> validCOs = new() { "CO1", "CO2", "CO3", "CO4", "CO5", "CO6" };
            HashSet<int> validMarks = new() { 20 };

            int totalPartCMarks = 0;
            int expectedPartCMarks = 20;

            for (int qNo = 19; qNo <= 19; qNo++)
            {
                string subQ1Text = ExtractBookmarkText(doc, $"Q{qNo}I");
                string subQ1CO = ExtractBookmarkText(doc, $"Q{qNo}ICO");
                string subQ1BT = ExtractBookmarkText(doc, $"Q{qNo}IBT");
                int subQ1Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IMarks");

                string subQ2Text = ExtractBookmarkText(doc, $"Q{qNo}II");
                string subQ2CO = ExtractBookmarkText(doc, $"Q{qNo}IICO");
                string subQ2BT = ExtractBookmarkText(doc, $"Q{qNo}IIBT");
                int subQ2Marks = ExtractMarksFromBookmark(doc, $"Q{qNo}IIMarks");

                string qpakAssigned = ExtractBookmarkText(doc, $"Q{qNo}QPAK");
                string subQ2AnswerKey = ExtractBookmarkText(doc, $"Q{qNo}SubQ2AnswerKey");

                int totalMarks = subQ1Marks + subQ2Marks;
                totalPartCMarks += totalMarks;

                // Validations
                if (string.IsNullOrEmpty(subQ1Text)) errors.Add($"❌ Q{qNo} SubQ1 text is empty.");
                if (string.IsNullOrEmpty(subQ1CO) || !validCOs.Contains(subQ1CO)) errors.Add($"❌ Q{qNo} SubQ1 CO is invalid.");
                if (string.IsNullOrEmpty(subQ1BT) || !validBTs.Contains(subQ1BT)) errors.Add($"❌ Q{qNo} SubQ1 BT is invalid.");
                if (!validMarks.Contains(subQ1Marks)) errors.Add($"❌ Q{qNo} SubQ1 Marks should be 6, 8, or 10.");

                if (string.IsNullOrEmpty(subQ2Text)) errors.Add($"❌ Q{qNo} SubQ2 text is empty.");
                if (string.IsNullOrEmpty(subQ2CO) || !validCOs.Contains(subQ2CO)) errors.Add($"❌ Q{qNo} SubQ2 CO is invalid.");
                if (string.IsNullOrEmpty(subQ2BT) || !validBTs.Contains(subQ2BT)) errors.Add($"❌ Q{qNo} SubQ2 BT is invalid.");
                if (!validMarks.Contains(subQ2Marks)) errors.Add($"❌ Q{qNo} SubQ2 Marks should be 6, 8, or 10.");

                if (!string.IsNullOrEmpty(qpakAssigned) && string.IsNullOrEmpty(subQ2AnswerKey)) errors.Add($"❌ Q{qNo} Answer key is missing for SubQ2 (QPAK assigned).");

                if (totalMarks != 20) errors.Add($"❌ Q{qNo} Total Marks = {totalMarks} (should be 20).");

                // Add to table
                htmlTable.Append($"<tr><td>Q{qNo}</td><td>{subQ1Text}</td><td>{subQ1CO}</td><td>{subQ1BT}</td><td>{subQ1Marks}</td><td>{subQ2Text}</td><td>{subQ2CO}</td><td>{subQ2BT}</td><td>{subQ2Marks}</td><td>{totalMarks}</td></tr>");
            }

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
            if(bookmark == null) return "Unknown";
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
        public async Task<bool?> SubmitGeneratedQPAsync(long userId, long InstitutionId, long documentId)
        {
            //var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
            //if (qpTemplateInstitution == null) return null;
            //var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
            
            //if (qpTemplate == null) return null;
            //qpTemplate.QPTemplateStatusTypeId = 3; //QP Pending for Scrutiny
            //AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

            //var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == qpTemplateInstitution.QPTemplateInstitutionId);
            
            //if (userQPTemplate == null) return null;
            //userQPTemplate.QPTemplateStatusTypeId = 9;

            //var generatedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 7 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            //if (generatedQpDocument != null)
            //{
            //    generatedQpDocument.DocumentId = documentId;
            //    AuditHelper.SetAuditPropertiesForUpdate(generatedQpDocument, 1);
            //}

            //AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
            //await _context.SaveChangesAsync();
            return true;
        }
        //public async Task<bool?> AssignTemplateForQPScrutinyAsync(long userId, long QPTemplateInstitutionId)
        //{
        //    var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
        //    if (qpTemplateInstitution == null) return null;
        //    var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);
        //    if (qpTemplate == null) return null;
        //    qpTemplate.QPTemplateStatusTypeId = 4; //QP Scrutiny Allocated

        //    var qpUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateInstitutionId == QPTemplateInstitutionId && qp.QPTemplateStatusTypeId==9);
            
        //    if (qpUserTemplate == null) return null;

        //    var userQPTemplate = new UserQPTemplate()
        //    {
        //        QPTemplateInstitutionId = qpUserTemplate.QPTemplateInstitutionId,
        //        UserId = userId,
        //        QPTemplateStatusTypeId = 10,//Scrutinize QP InProgress
        //        QPDocumentId = qpUserTemplate.QPDocumentId
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(userQPTemplate, 1);
        //    _context.UserQPTemplates.Add(userQPTemplate);
        //    await _context.SaveChangesAsync();

        //    var qpSyllabusDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 1 && qptd.UserQPTemplateId == qpUserTemplate.UserQPTemplateId);
        //    var userQPSyllabusDocument = new UserQPTemplateDocument()
        //    {
        //        QPDocumentTypeId = 1,
        //        UserQPTemplateId = userQPTemplate.UserQPTemplateId,
        //        DocumentId = qpSyllabusDocument?.DocumentId ?? 0
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(userQPSyllabusDocument, 1);
        //    _context.UserQPTemplateDocuments.Add(userQPSyllabusDocument);

        //    var qpGenerationDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 7 && qptd.UserQPTemplateId == qpUserTemplate.UserQPTemplateId);
        //    var userQPDocument = new UserQPTemplateDocument()
        //    {
        //        QPDocumentTypeId = 6,
        //        UserQPTemplateId = userQPTemplate.UserQPTemplateId,
        //        DocumentId = qpGenerationDocument?.DocumentId ?? 0
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
        //    _context.UserQPTemplateDocuments.Add(userQPDocument);

        //    var scrutinizedQPDocument = new UserQPTemplateDocument()
        //    {
        //        QPDocumentTypeId = 7,
        //        UserQPTemplateId = userQPTemplate.UserQPTemplateId,
        //        DocumentId = 0
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(scrutinizedQPDocument, 1);
        //    _context.UserQPTemplateDocuments.Add(scrutinizedQPDocument);

        //    await _context.SaveChangesAsync();
        //    return true;
        ////}
        //public async Task<bool?> SubmitScrutinizedQPAsync(long userId, long QPTemplateInstitutionId, long documentId)
        //{
        //    var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
        //    if (qpTemplateInstitution == null) return null;
        //    var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

        //    if (qpTemplate == null) return null;
        //    qpTemplate.QPTemplateStatusTypeId = 5; //QP Pending for Selection
        //    AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

        //    var qpScrutinizedUserTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == QPTemplateInstitutionId && uqp.QPTemplateStatusTypeId == 10);

        //    if (qpScrutinizedUserTemplate == null) return null;
        //    qpScrutinizedUserTemplate.QPTemplateStatusTypeId = 11;//Scrutinized QP Submitted
        //    AuditHelper.SetAuditPropertiesForUpdate(qpScrutinizedUserTemplate, 1);
           

        //    var userQPTemplateForSelection = new UserQPTemplate()
        //    {
        //        QPTemplateInstitutionId = qpScrutinizedUserTemplate.QPTemplateInstitutionId,
        //        UserId = 1,
        //        QPTemplateStatusTypeId = 12,//Selection QP InProgress
        //        QPDocumentId = qpScrutinizedUserTemplate.QPDocumentId
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(userQPTemplateForSelection, 1);
        //    _context.UserQPTemplates.Add(userQPTemplateForSelection);
        //    await _context.SaveChangesAsync();

        //    var scrutinizedQPDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 9 && qptd.UserQPTemplateId == qpScrutinizedUserTemplate.UserQPTemplateId);
        //    if(scrutinizedQPDocument != null)
        //    {
        //        scrutinizedQPDocument.DocumentId = documentId;
        //        AuditHelper.SetAuditPropertiesForUpdate(scrutinizedQPDocument, 1);
        //    }
        //    await _context.SaveChangesAsync();

        //    var userQPDocument = new UserQPTemplateDocument()
        //    {
        //        QPDocumentTypeId = 8,//For QP Selection
        //        UserQPTemplateId = userQPTemplateForSelection.UserQPTemplateId,
        //        DocumentId = documentId
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(userQPDocument, 1);
        //    _context.UserQPTemplateDocuments.Add(userQPDocument);

        //    var selectedQPDocument = new UserQPTemplateDocument()
        //    {
        //        QPDocumentTypeId = 9,
        //        UserQPTemplateId = userQPTemplateForSelection.UserQPTemplateId,
        //        DocumentId = documentId
        //    };
        //    AuditHelper.SetAuditPropertiesForInsert(selectedQPDocument, 1);
        //    _context.UserQPTemplateDocuments.Add(selectedQPDocument);

        //    await _context.SaveChangesAsync();
        //    return true;
        //}
        //public async Task<bool?> SubmitSelectedQPAsync(long userId, long QPTemplateInstitutionId, long documentId)
        //{
        //    var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == QPTemplateInstitutionId);
        //    if (qpTemplateInstitution == null) return null;
        //    var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

        //    if (qpTemplate == null) return null;
        //    qpTemplate.QPTemplateStatusTypeId = 6; //QP Selected
        //    AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);

        //    var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.UserId == userId && uqp.QPTemplateInstitutionId == QPTemplateInstitutionId && uqp.QPTemplateStatusTypeId ==12);

        //    if (userQPTemplate == null) return null;
        //    userQPTemplate.QPTemplateStatusTypeId = 13;//Selected QP Submitted
        //    AuditHelper.SetAuditPropertiesForUpdate(userQPTemplate, 1);
        //    await _context.SaveChangesAsync();


        //    var selectedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 11 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
        //    if(selectedQpDocument != null)
        //    {
        //        selectedQpDocument.DocumentId = documentId;
        //        AuditHelper.SetAuditPropertiesForUpdate(selectedQpDocument, 1);
        //    }
        //    await _context.SaveChangesAsync();
        //    return true;
        //}
        private async Task<List<UserQPTemplateVM>> GetUserQPTemplatesAsync(long userId)
        {
            try
            {
            var courses = _context.Courses.ToList();
            var documents = _context.Documents.ToList();
            var qpTemplateStatuss =_context.QPTemplateStatusTypes.ToList();
            var userQPTemplateVMs = new List<UserQPTemplateVM>();
            var users = _context.Users.ToList();
            var userQPTemplates = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId && uqp.IsActive).ToList();
            var qpTemplateIds = _context.UserQPTemplates.Where(uqp => uqp.UserId == userId && uqp.IsActive).Select(qpti => qpti.QPTemplateId).ToList();
            var qpTemplates = await _context.QPTemplates.Where(qp => qpTemplateIds.Contains(qp.QPTemplateId)).ToListAsync();
            userQPTemplates.ForEach(userQPTemplate =>
            {
                var qpTemplate = qpTemplates.FirstOrDefault(p => p.QPTemplateId == userQPTemplate.QPTemplateId);
                userQPTemplateVMs.Add(new UserQPTemplateVM() {
                    UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                    InstitutionId = userQPTemplate.InstitutionId,
                    UserId = userQPTemplate.UserId,
                    QPTemplateName= qpTemplate.QPTemplateName,
                    UserName = users.FirstOrDefault(u => u.UserId == userQPTemplate.UserId)?.Name ?? string.Empty,
                    QPTemplateStatusTypeId = userQPTemplate.QPTemplateStatusTypeId, 
                    QPTemplateStatusTypeName = qpTemplateStatuss.FirstOrDefault(qps => qps.QPTemplateStatusTypeId == userQPTemplate.QPTemplateStatusTypeId)?.Name ?? string.Empty,
                    QPDocumentId = userQPTemplate.QPDocumentId,
                    QPDocumentName = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.QPDocumentId)?.Name ?? string.Empty,
                    QPDocumentUrl = documents.FirstOrDefault(d => d.DocumentId == userQPTemplate.QPDocumentId)?.Url ?? string.Empty,
                    CourseSyllabusDocumentId = qpTemplate.CourseSyllabusDocumentId,
                    CourseSyllabusDocumentName = documents.FirstOrDefault(d => d.DocumentId == qpTemplate.CourseSyllabusDocumentId)?.Name ?? string.Empty,
                    CourseSyllabusDocumentUrl = documents.FirstOrDefault(d => d.DocumentId == qpTemplate.CourseSyllabusDocumentId)?.Url ?? string.Empty
                });
            });
                return userQPTemplateVMs;
            }
            catch (Exception ex)
            {
                throw ;
            }
        }
        //public async Task<bool?> PrintSelectedQPAsync(long qpTemplateInstitutionId, string qpCode, bool isForPrint)
        //{
        //    var qpTemplateInstitution = await _context.QPTemplateInstitutions.FirstOrDefaultAsync(qpti => qpti.QPTemplateInstitutionId == qpTemplateInstitutionId);
        //    if (qpTemplateInstitution == null) return null;
        //    var qpTemplate = await _context.QPTemplates.FirstOrDefaultAsync(qp => qp.QPTemplateId == qpTemplateInstitution.QPTemplateId);

        //    if (qpTemplate == null) return null;
        //    qpTemplate.QPTemplateStatusTypeId = 7; //QP Selected
        //    qpTemplate.QPCode = qpCode;
        //    AuditHelper.SetAuditPropertiesForUpdate(qpTemplate, 1);
        //    await _context.SaveChangesAsync();

        //    var userQPTemplate = await _context.UserQPTemplates.FirstOrDefaultAsync(uqp => uqp.QPTemplateInstitutionId == qpTemplateInstitutionId && uqp.QPTemplateStatusTypeId == 12);
        //    if (userQPTemplate != null)
        //    {
        //        var selectedQpDocument = await _context.UserQPTemplateDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 9 && qptd.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
        //        var institutaions = await _context.QPTemplateInstitutions.Where(qpti => qpti.QPTemplateId == qpTemplate.QPTemplateId).ToListAsync();
        //        foreach (var institution in institutaions)
        //        {
        //            var qpDocumentToPrint = await _context.QPTemplateInstitutionDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 2 && qptd.QPTemplateInstitutionId == institution.QPTemplateInstitutionId);
        //            if(selectedQpDocument != null && qpDocumentToPrint != null)
        //            await PrintQPDocumentByInstitutionAsync(qpTemplate, institution, selectedQpDocument, qpDocumentToPrint, isForPrint);

        //            var qpDocumentWithAnswerToPrint = await _context.QPTemplateInstitutionDocuments.FirstOrDefaultAsync(qptd => qptd.QPDocumentTypeId == 3 && qptd.QPTemplateInstitutionId == institution.QPTemplateInstitutionId);
        //            if (selectedQpDocument != null && qpDocumentWithAnswerToPrint != null)
        //                await PrintQPAnswerDocumentByInstitutionAsync(qpTemplate, institution, selectedQpDocument, qpDocumentWithAnswerToPrint);
        //        }
        //    }
        //    return true;
        //}
        ////private async Task<bool> PrintQPDocumentByInstitutionAsync(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, UserQPTemplateDocument selectedQPDocument, QPTemplateInstitutionDocument documentToPrint, bool isForPrint)
        //{
        //    var qpSelectedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == selectedQPDocument.DocumentId);
        //    var qpToPrintDocument = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentToPrint.DocumentId);
        //    if (qpSelectedDocument == null || qpToPrintDocument == null) return false;
        //    await _bookmarkProcessor.ProcessBookmarksAndPrint(qPTemplate, qPTemplateInstitution, qpSelectedDocument.Name, qpToPrintDocument.Name,isForPrint);
        //    return true;
        //}
        //private async Task<bool> PrintQPAnswerDocumentByInstitutionAsync(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, UserQPTemplateDocument selectedQPDocument, QPTemplateInstitutionDocument documentToPrint)
        //{
        //    return true;
        //}
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
                    UserName =$"{user.Name}-{user.CollegeName}-{user.DepartmentName}",
                    IsAvailableForQPAssignment = (userQPTemplates.Where(uqp => uqp.UserId == user.UserId).ToList().Count < 3)
                });
            }
            return qpAssignmentExperts;
        }
    }
}
