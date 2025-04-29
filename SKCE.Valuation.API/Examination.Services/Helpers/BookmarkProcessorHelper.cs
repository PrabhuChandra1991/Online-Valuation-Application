using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SKCE.Examination.Models.DbModels.QPSettings;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using System.Diagnostics;
using System.Text;
using Syncfusion.Licensing;
using Spire.Doc;
using Document = Spire.Doc.Document;
using Paragraph = Spire.Doc.Documents.Paragraph;
using Spire.Doc.Documents;
using Syncfusion.DocIO.DLS;
using Spire.Doc.Collections;
using Body = Spire.Doc.Body;
using Syncfusion.DocIO;
using SKCE.Examination.Services.ViewModels.QPSettings;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Math;
using Spire.Doc.Fields;
using Amazon.Runtime.Internal.Transform;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.IO;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml;

namespace SKCE.Examination.Services.Helpers
{
    public class BookmarkProcessor
    {
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
        private readonly ExaminationDbContext _context;
        public BookmarkProcessor(IConfiguration configuration, AzureBlobStorageHelper azureBlobStorageHelper, ExaminationDbContext context)
        {
            _context = context;
            _azureBlobStorageHelper = azureBlobStorageHelper;
        }
        // Processes bookmarks in a source document and prints the modified document
        public async Task<string> ProcessBookmarksAndPrint(QPTemplate qPTemplate, UserQPTemplate userQPTemplate, string inputDocPath, string documentPathToPrint, bool isForPrint, long printedWordDocumentId)
        {
            try
            {
                //Loading license
                LoadLicense();

                // Dictionary of bookmarks and their new values
                Dictionary<string, string> bookmarkUpdates = new Dictionary<string, string>
                {
                    { "QPCODE", "" },
                    { "EXAMMONTH", "" },
                    { "EXAMYEAR", "" },
                    { "EXAMTYPE", "" },
                    { "REGULATIONYEAR", "" },
                    { "PROGRAMME","" },
                    { "SEMESTER", "" },
                    { "COURSECODE", "" },
                    { "COURSETITLE", "" },
                    { "SUPPORTCATALOGS", "" }
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
                        .Where(cd => cd.cd.InstitutionId == userQPTemplate.InstitutionId)
                        .Select(cd => new
                        {
                            DepartmentId = cd.d.DepartmentId,
                            DepartmentName = cd.d.Name,
                            DepartmentShortName = cd.d.ShortName,
                        }).ToList();

                        var departmentIds = departmentVMs.Select(q => q.DepartmentId).ToList();

                        if (departmentIds.Any() && departmentIds.Count > 2)
                        {
                            bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.ShortName).ToList());
                        }
                        else
                            bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.Name).ToList());
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
                    else if (bookmarkName == "QPCODE")
                    {
                        bookmarkHtml = string.Empty;
                        if (isForPrint)
                        {
                            bookmarkHtml = GetQPCode(qPTemplate);
                            qPTemplate.QPCode = bookmarkHtml;
                            userQPTemplate.QPCode = bookmarkHtml;
                        }
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
                    else if (bookmarkName == "SUPPORTCATALOGS")
                    {
                        bookmarkHtml = string.Empty;
                        var GraphName = string.Empty;
                        var TableName = string.Empty;
                        if (userQPTemplate.IsGraphsRequired != null && userQPTemplate.IsGraphsRequired.Value)
                        {
                            bookmarkHtml = $"Graph Sheet required - {userQPTemplate.GraphName}\n\r";
                        }
                        if (userQPTemplate.IsTablesAllowed != null && userQPTemplate.IsTablesAllowed.Value)
                        {
                            bookmarkHtml = bookmarkHtml + $"\nTables are allowed - {userQPTemplate.TableName}";
                        }
                    }

                    bookmarkUpdates[bookmark.Key] = bookmarkHtml;
                }
                // Load the template document where bookmarks need to be replaced
                Document templateDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(documentPathToPrint);
                var courseSyllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId);
                var courseSyllabusWordDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == courseSyllabusDocument.WordDocumentId);

                // Save the updated document
                var updatedSourcePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_updatedSourceForPreview.docx", bookmarkUpdates["COURSECODE"], DateTime.Now.ToString("ddMMyyyyhhmmss")));
                
                MemoryStream sourceStream = await _azureBlobStorageHelper.DownloadWordDocumentFromBlobToOpenXML(inputDocPath);
                sourceStream.Position = 0;
                // Load the template document where bookmarks need to be replaced
                MemoryStream sourceSyllabusStream = await _azureBlobStorageHelper.DownloadWordDocumentFromBlobToOpenXML(courseSyllabusWordDocument.Name);
                using (WordprocessingDocument SyllabusWordDoc = WordprocessingDocument.Open(sourceSyllabusStream, true))
                {
                    var bookmarks = SyllabusWordDoc.MainDocumentPart.RootElement.Descendants<DocumentFormat.OpenXml.Wordprocessing.BookmarkStart>();

                    foreach (var bookmark in bookmarks)
                    {
                        string name = bookmark.Name;
                        string textValue = bookmark.NextSibling<DocumentFormat.OpenXml.Wordprocessing.Run>()?.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Text>()?.Text;

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
                            DocumentFormat.OpenXml.Wordprocessing.Run run = new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(newText));
                            bookmarkStart.Parent.InsertAfter(run, bookmarkStart);
                        }
                    }
                    wordDoc.MainDocumentPart.Document.Save();
                }
                sourceStream.Position = 0; // Reset stream before returning

                using (FileStream fileStream = new FileStream(updatedSourcePath, FileMode.Create, FileAccess.Write))
                {
                    sourceStream.CopyTo(fileStream);
                }
                Document updatedSourcedoc = new Document();
                updatedSourcedoc.LoadFromFile(updatedSourcePath);

                // Iterate through all bookmarks in the updated source document and replace in actual QP template
                foreach (Spire.Doc.Bookmark bookmark1 in updatedSourcedoc.Bookmarks)
                {
                    string bookmarkName = bookmark1.Name;
                    // Find the same bookmark in the destination document
                    Spire.Doc.Bookmark destinationBookmark = templateDoc.Bookmarks.FindByName(bookmarkName);
                    if (destinationBookmark != null)
                    {
                        // Extract content from the source bookmark (including images)
                        DocumentObjectCollection sourceContent = bookmark1.BookmarkStart.OwnerParagraph.ChildObjects;

                        // Clear existing content in destination bookmark
                        Paragraph destParagraph = destinationBookmark.BookmarkStart.OwnerParagraph;
                        destParagraph.ChildObjects.Clear();

                        // Copy content to the destination bookmark
                        foreach (DocumentObject obj in sourceContent)
                        {
                            destParagraph.ChildObjects.Add(obj.Clone());
                        }
                    }
                }
                var previewdocPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}.docx", bookmarkUpdates["COURSECODE"], qPTemplate.QPCode, DateTime.Now.ToString("ddMMyyyyhhmmss")));
                templateDoc.Watermark = null;
                templateDoc.SaveToFile(previewdocPath, FileFormat.Docx);
                
                // Remove evaluation watermark from the output document By OpenXML
                RemoveTextFromDocx(previewdocPath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");

                // Save the modified document as PDF
                var previewPdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}.pdf", bookmarkUpdates["COURSECODE"], qPTemplate.QPCode, DateTime.Now.ToString("ddMMyyyyhhmmss")));
                //ConvertToPdfBySyncfusion(previewdocPath, previewPdfPath);

                if (!isForPrint) return previewdocPath;

                //var pdfDocumentId =  await _azureBlobStorageHelper.UploadFileToBlob(previewPdfPath, string.Format("{0}_{1}_{2}_{3}_{4}.pdf", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, bookmarkUpdates["COURSECODE"], DateTime.UtcNow.ToShortDateString()));
                var wordDocumentId = await _azureBlobStorageHelper.UploadDocxFileToBlob(previewdocPath, string.Format("{0}_{1}_{2}_{3}_{4}.docx", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, bookmarkUpdates["COURSECODE"], DateTime.UtcNow.ToShortDateString()));

               await SaveSelectedQPDetail(qPTemplate, userQPTemplate, printedWordDocumentId, wordDocumentId);
               return await PrintQP(updatedSourcedoc, bookmarkUpdates,qPTemplate,userQPTemplate);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return string.Empty;
        }

        private async Task<string> PrintQP(Document updatedSourcedoc, Dictionary<string, string> bookmarkUpdates, QPTemplate qPTemplate, UserQPTemplate userQPTemplate) 
        {
            var degreeTypeName = _context.DegreeTypes.FirstOrDefault(dt => dt.DegreeTypeId == qPTemplate.DegreeTypeId)?.Name ?? string.Empty;
            var qpDocument = _context.QPDocuments.FirstOrDefault(d => d.InstitutionId == userQPTemplate.InstitutionId && d.RegulationYear == qPTemplate.RegulationYear && d.DegreeTypeName == degreeTypeName && d.DocumentTypeId == 2 && d.ExamType.ToLower().Contains(qPTemplate.ExamType.ToLower()));
            string documentPathToPrint = _context.Documents.FirstOrDefault(d => d.DocumentId == qpDocument.DocumentId)?.Name;
            // Load the template document where bookmarks need to be replaced
            Document templateDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(documentPathToPrint);

            // Iterate through all bookmarks in the source document
            foreach (Spire.Doc.Bookmark bookmark1 in updatedSourcedoc.Bookmarks)
            {
                string bookmarkName = bookmark1.Name;
                // Find the same bookmark in the destination document
                Spire.Doc.Bookmark destinationBookmark = templateDoc.Bookmarks.FindByName(bookmarkName);
                if (destinationBookmark != null)
                {
                    // Extract content from the source bookmark (including images)
                    DocumentObjectCollection sourceContent = bookmark1.BookmarkStart.OwnerParagraph.ChildObjects;

                    // Clear existing content in destination bookmark
                    Paragraph destParagraph = destinationBookmark.BookmarkStart.OwnerParagraph;
                    destParagraph.ChildObjects.Clear();

                    // Copy content to the destination bookmark
                    foreach (DocumentObject obj in sourceContent)
                    {
                        destParagraph.ChildObjects.Add(obj.Clone());
                    }
                }
            }

            var previewdocPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}_QP_FinalPrint.docx", bookmarkUpdates["COURSECODE"], qPTemplate.QPCode, DateTime.Now.ToString("ddMMyyyyhhmmss")));
            templateDoc.Watermark = null;
            templateDoc.SaveToFile(previewdocPath, FileFormat.Docx);

            ////// Remove evaluation watermark from the output document By OpenXML
            RemoveTextFromDocx(previewdocPath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");
            //Console.WriteLine("Bookmarks replaced successfully!");

            // Save the modified document as PDF
            var previewPdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}_QP.pdf", bookmarkUpdates["COURSECODE"], qPTemplate.QPCode, DateTime.Now.ToString("ddMMyyyyhhmmss")));
            //ConvertToPdfBySyncfusion(previewdocPath, previewPdfPath);
            var wordDocumentId = await _azureBlobStorageHelper.UploadDocxFileToBlob(previewdocPath, string.Format("{0}_{1}_{2}_{3}_FinalPrinted.docx", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, DateTime.UtcNow.ToShortDateString()));
            var selectedQPDetail = _context.SelectedQPDetails.FirstOrDefault(sqp => sqp.UserQPTemplateId == userQPTemplate.UserQPTemplateId);
            if(selectedQPDetail != null)
            {
                selectedQPDetail.FinalQPPrintedWordDocumentId = wordDocumentId;
                _context.SaveChanges();
            }
            return previewdocPath;
        }
        private string GetQPCode(QPTemplate qPTemplate)
        {
            var qpCode = string.Empty;
            string[] months = qPTemplate.ExamMonth.Split('/');
            var runningNumber = (_context.SelectedQPDetails.Count() + 1).ToString();
            Random random = new Random();
            int randomNumber = random.Next(1000, 10000);
            qpCode = $"{string.Join("", months.Select(m => m.Trim()[0]))}{qPTemplate.ExamYear}{runningNumber}{randomNumber.ToString()}";
            return qpCode;
        }
        private async Task SaveSelectedQPDetail(QPTemplate qPTemplate, UserQPTemplate userQPTemplate, long wordDocumentId,long documentId)
        {
            var existingQPTemplate = _context.QPTemplates.FirstOrDefault(q => q.QPTemplateId == qPTemplate.QPTemplateId);
            if(existingQPTemplate != null)
            {
                existingQPTemplate.QPCode = qPTemplate.QPCode;
                AuditHelper.SetAuditPropertiesForUpdate(existingQPTemplate, 1);
            }
            var existingUserQPTemplate = _context.UserQPTemplates.FirstOrDefault(q => q.QPTemplateId == userQPTemplate.UserQPTemplateId);
            if (existingUserQPTemplate != null)
            {
                existingUserQPTemplate.QPCode = qPTemplate.QPCode;
                AuditHelper.SetAuditPropertiesForUpdate(existingUserQPTemplate, 1);
            }
           var selectedExaminations= _context.Examinations.Where(qp =>
            qp.InstitutionId == userQPTemplate.InstitutionId &&
            qp.CourseId == qPTemplate.CourseId &&
            qp.RegulationYear == qPTemplate.RegulationYear &&
            qp.BatchYear == qPTemplate.BatchYear &&
            qp.DegreeTypeId == qPTemplate.DegreeTypeId &&
            qp.ExamType == qPTemplate.ExamType &&
            qp.Semester == qPTemplate.Semester &&
            qp.ExamMonth == qPTemplate.ExamMonth &&
            qp.ExamYear == qPTemplate.ExamYear).ToList();
            foreach (var selectedExamination in selectedExaminations)
            {
                selectedExamination.IsQPPrinted = true;
                selectedExamination.QPPrintedById = 1;
                selectedExamination.QPPrintedDate = DateTime.Now;
            }

            var selectedQPDetail = new SelectedQPDetail
            {
                BatchYear = qPTemplate.BatchYear,
                CourseId = qPTemplate.CourseId,
                DegreeTypeId = qPTemplate.DegreeTypeId,
                ExamMonth = qPTemplate.ExamMonth,
                ExamType = qPTemplate.ExamType,
                ExamYear = qPTemplate.ExamYear,
                InstitutionId = userQPTemplate.InstitutionId,
                QPPrintedById = 1,
                QPPrintedDate = DateTime.Now,
                QPPrintedDocumentId = documentId,
                QPPrintedWordDocumentId = wordDocumentId,
                RegulationYear = qPTemplate.RegulationYear,
                Semester = qPTemplate.Semester,
                UserQPTemplateId = userQPTemplate.UserQPTemplateId,
                IsQPOnly = userQPTemplate.IsQPOnly,
                QPCode = qPTemplate.QPCode,
            };
            AuditHelper.SetAuditPropertiesForInsert(selectedQPDetail,1);
            _context.SelectedQPDetails.Add(selectedQPDetail);
            _context.SaveChanges();

            SaveSelectedQPBookmarksByFilePath(selectedQPDetail);
        }
        static void ConvertToPdfBySyncfusion(string docxPath, string pdfPath)
        {
            // Load the Word document
            Syncfusion.DocIO.DLS.WordDocument document = new Syncfusion.DocIO.DLS.WordDocument(docxPath, FormatType.Docx);
            //// Convert Word to PDF
            Syncfusion.Pdf.PdfDocument pdfDocument = new Syncfusion.Pdf.PdfDocument();
            Syncfusion.DocIORenderer.DocIORenderer renderer = new Syncfusion.DocIORenderer.DocIORenderer();
            pdfDocument = renderer.ConvertToPDF(document);
            //ApplyPdfSecurity(pdfDocument);
            // Save the PDF file
            pdfDocument.Save(pdfPath);
            document.Close();

            System.Console.WriteLine("DOCX to PDF conversion By Syncfusion completed.");
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
        private void LoadLicense()
        {
            // This line attempts to set a license from several locations relative to the executable and Aspose.Words.dll.
            // You can also use the additional overload to load a license from a stream, this is useful,
            // for instance, when the license is stored as an embedded resource.
            try
            {
                // Apply Syncfusion License
                SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXtfcXRQQ2lZWEJwW0VWYUA=");
                Console.WriteLine("License set successfully.");
            }
            catch (Exception e)
            {
                // We do not ship any license with this example,
                // visit the Syn fusion site to obtain either a temporary or permanent license. 
                Console.WriteLine("\nThere was an error setting the license: " + e.Message);
            }
        }
        public string ExtractBookmarkAsHtmlBase64(Document doc, string bookmarkName)
        {
            //// Load the Word documentDocument
            //Document doc = new Document();
            //doc.LoadFromFile(filePath);

            // Find the bookmark  
            Spire.Doc.Documents.BookmarksNavigator navigator = new Spire.Doc.Documents.BookmarksNavigator(doc);
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
