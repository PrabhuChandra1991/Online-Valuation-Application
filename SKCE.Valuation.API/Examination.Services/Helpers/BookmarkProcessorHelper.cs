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

namespace SKCE.Examination.Services.Helpers
{
    public class BookmarkProcessor
    {
        private string outputPdfPath = "";
        private readonly string printerName = "";
        private readonly string asposeLicenseFileName = "";
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
        private readonly ExaminationDbContext _context;
        public BookmarkProcessor(IConfiguration configuration, AzureBlobStorageHelper azureBlobStorageHelper, ExaminationDbContext context)
        {
            _context = context;
            _azureBlobStorageHelper = azureBlobStorageHelper;
            asposeLicenseFileName = configuration["Print:AsposeLicenseFileName"] ?? throw new ArgumentNullException(nameof(configuration), "AsposeLicenseFileName is not configured.");
            printerName = configuration["Print:PrinterName"] ?? throw new ArgumentNullException(nameof(configuration), "PrinterName is not configured.");
        }
        // Processes bookmarks in a source document and prints the modified document
        public async Task ProcessBookmarksAndPrint(QPTemplate qPTemplate, UserQPTemplate userQPTemplate, string inputDocPath, string documentPathToPrint, bool isForPrint)
        {
            try
            {
                // Save the updated document
                var updatedSourcePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.docx", qPTemplate.QPTemplateName, DateTime.Now.ToString("ddMMyyyyhhmmss")));
                //Loading license
                LoadLicense();

                int numberOfCopies = 0; // Number of copies to print
                // Load the source document that contains the bookmarks
                Document sourceDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(inputDocPath);

                // Load the template document where bookmarks need to be replaced
                Document templateDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(documentPathToPrint);

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

                var courseSyllabusDocument = _context.CourseSyllabusDocuments.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId);
                
                var courseSyllabusWordDocument = _context.Documents.FirstOrDefault(d => d.DocumentId == courseSyllabusDocument.WordDocumentId);
                // Load the template document where bookmarks need to be replaced
                Document SyllabusWordDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(courseSyllabusWordDocument.Name);
                
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
                        bookmarkHtml = _context.QPTemplates.FirstOrDefault(c =>
                        c.QPTemplateId == qPTemplate.QPTemplateId)?.QPCode.ToString() ?? string.Empty;
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
                        if (userQPTemplate.IsGraphsRequired.Value)
                        {
                            bookmarkHtml = $"Graph Sheet required - {userQPTemplate.GraphName}";
                        }
                        if (userQPTemplate.IsTablesAllowed.Value)
                        {
                            bookmarkHtml = bookmarkHtml + $"Tables are allowed - {userQPTemplate.TableName}";
                        }
                    }

                    bookmarkUpdates[bookmark.Key] = bookmarkHtml;
                    // Replace bookmark content in the template document
                    //ReplaceBookmarkWithHtml(tempDoc, bookmarkName, bookmarkHtml);
                }

                // Loop through each bookmark and update text
                foreach (var bookmark in bookmarkUpdates)
                {
                    Spire.Doc.Bookmark sbookMark = sourceDoc.Bookmarks[bookmark.Key];
                    if (sbookMark != null)
                    {
                        if (sbookMark.BookmarkStart.OwnerParagraph.Items.Count > 0)
                        {
                            foreach (var item in sbookMark.BookmarkStart.OwnerParagraph.Items)
                            {
                                if (item is TextRange textRange)
                                {
                                    if (textRange.Text == sbookMark.Name)
                                    {
                                        // Replace the text in the bookmark with the new value
                                        textRange.Text = bookmark.Value;
                                    }
                                }
                                if (item is Spire.Doc.BookmarkEnd bookmarkEnd)
                                {
                                    if (bookmarkEnd.OwnerParagraph.Text == sbookMark.Name)
                                    {
                                        foreach (var bookowner in sbookMark.BookmarkStart.OwnerParagraph.Items)
                                        {
                                            if (item is TextRange textRange1)
                                            {
                                                if (textRange1.Text == sbookMark.Name)
                                                {
                                                    // Replace the text in the bookmark with the new value
                                                    textRange1.Text = bookmark.Value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                sourceDoc.SaveToFile(updatedSourcePath, FileFormat.Docx);

                Document updatedSourcedoc = new Document();
                updatedSourcedoc.LoadFromFile(updatedSourcePath);

                // Iterate through all bookmarks in the source document
                foreach (Spire.Doc.Bookmark bookmark2 in SyllabusWordDoc.Bookmarks)
                {
                    string bookmarkName = bookmark2.Name;
                    // Find the same bookmark in the destination document
                    Spire.Doc.Bookmark destinationBookmark = templateDoc.Bookmarks.FindByName(bookmarkName);
                    if (destinationBookmark != null)
                    {
                        // Extract content from the source bookmark (including images)
                        DocumentObjectCollection sourceContent = bookmark2.BookmarkStart.OwnerParagraph.ChildObjects;

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

                var previewdocPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.docx", qPTemplate.QPTemplateName, DateTime.Now.ToString("ddMMyyyyhhmmss")));
                templateDoc.SaveToFile(previewdocPath, FileFormat.Docx);

                //// Remove evaluation watermark from the output document By OpenXML
                RemoveTextFromDocx(previewdocPath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");
                Console.WriteLine("Bookmarks replaced successfully!");

                // Save the modified document as PDF
                var previewPdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.pdf", qPTemplate.QPTemplateName, DateTime.Now.ToString("ddMMyyyyhhmmss")));
                ConvertToPdfBySyncfusion(previewdocPath, previewPdfPath);

                OpenPdfInBrowser(previewPdfPath);

                if (!isForPrint) return;
                var documentId = _azureBlobStorageHelper.UploadFileToBlob(outputPdfPath, string.Format("{0}_{1}_{2}_{3}.pdf", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, DateTime.UtcNow.ToShortDateString()));
                
                SaveBookmarksToDatabaseByFilePath(documentPathToPrint, qPTemplate.QPTemplateId,userQPTemplate.SubmittedQPDocumentId);

                var selectedQPTemplate = _context.QPTemplates.Where(qp => qp.QPTemplateId == qPTemplate.QPTemplateId);
                if (selectedQPTemplate != null)
                {
                    //selectedQPTemplate.Selected
                }
                // Trigger printing
                //PrintPdf(outputPdfPath, printerName, numberOfCopies);

                Console.WriteLine("Processing and printing completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void ConvertToPdfBySyncfusion(string docxPath, string pdfPath)
        {
            // Load the Word document
            Syncfusion.DocIO.DLS.WordDocument document = new Syncfusion.DocIO.DLS.WordDocument(docxPath, FormatType.Docx);
            //// Convert Word to PDF
            Syncfusion.Pdf.PdfDocument pdfDocument = new Syncfusion.Pdf.PdfDocument();
            Syncfusion.DocIORenderer.DocIORenderer renderer = new Syncfusion.DocIORenderer.DocIORenderer();
            pdfDocument = renderer.ConvertToPDF(document);
            ApplyPdfSecurity(pdfDocument);
            // Save the PDF file
            pdfDocument.Save(pdfPath);
            document.Close();
            //OpenPdfInBrowser(pdfPath);
            System.Console.WriteLine("DOCX to PDF conversion By Syncfusion completed.");
        }
        static void ApplyPdfSecurity(Syncfusion.Pdf.PdfDocument pdfDocument)
        {
            // Create security settings
            Syncfusion.Pdf.Security.PdfSecurity security = pdfDocument.Security;

            //// Set an owner password (optional)
            //security.OwnerPassword = "Owner@123";

            //// Set a user password (optional, required to open the PDF)
            //security.UserPassword = "User@123";

            // Disable printing
            security.Permissions = Syncfusion.Pdf.Security.PdfPermissionsFlags.Print;

            // Disable copy, edit, and extract
            security.Permissions &= ~(Syncfusion.Pdf.Security.PdfPermissionsFlags.CopyContent | Syncfusion.Pdf.Security.PdfPermissionsFlags.EditContent | Syncfusion.Pdf.Security.PdfPermissionsFlags.AccessibilityCopyContent);

            Console.WriteLine("PDF security applied: No Print, Copy, Edit.");
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
        /// <summary>
        /// Extracts text, images, and tables inside a bookmark and converts it to an HTML string.
        /// </summary>
        public static void OpenPdfInBrowser(string pdfPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true // Opens in default PDF viewer/browser
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening PDF: " + ex.Message);
            }
        }
        public void SaveBookmarksToDatabaseByFilePath(string fileName, long qPtemplateId,long documentId)
        {
            Document sourceDoc = _azureBlobStorageHelper.DownloadWordDocumentFromBlob(fileName).Result;
            // Iterate through all bookmarks in the source document

            var qpDocumentBookMarks = new List<QPDocumentBookMark>();
            foreach (Spire.Doc.Bookmark bookmark in sourceDoc.Bookmarks)
            {
                string bookmarkHtmlBase64 = ConvertBookmarkToHtmlBase64(sourceDoc, bookmark);

                if (!string.IsNullOrEmpty(bookmarkHtmlBase64))
                {
                    var qPDocumentBookMark = new QPDocumentBookMark
                    {
                        QPTemplateId = qPtemplateId,
                        BookMarkName = bookmark.Name,
                        DocumentId = documentId,
                        BookMarkText = bookmarkHtmlBase64
                    };
                    AuditHelper.SetAuditPropertiesForInsert(qPDocumentBookMark, 1);
                    qpDocumentBookMarks.Add(qPDocumentBookMark);
                }
            }
            _context.QPDocumentBookMarks.AddRange(qpDocumentBookMarks);
            _context.SaveChanges();
        }
        private static string ConvertBookmarkToHtmlBase64(Document doc, Spire.Doc.Bookmark bookmark)
        {
            Spire.Doc.BookmarkStart bookmarkStart = bookmark.BookmarkStart;
            Spire.Doc.BookmarkEnd bookmarkEnd = bookmark.BookmarkEnd;

            if (bookmarkStart == null || bookmarkEnd == null) return null;

            Document extractedDoc = new Document();
            Section section = extractedDoc.AddSection();

            bool isInsideBookmark = false;
            foreach (DocumentObject obj in doc.Sections[0].Body.ChildObjects)
            {
                if (obj == bookmarkStart) isInsideBookmark = true;

                if (isInsideBookmark)
                {
                    section.Body.ChildObjects.Add(obj.Clone());
                }

                if (obj == bookmarkEnd) break;
            }

            // Set export options with image embedding
            HtmlExportOptions options = new HtmlExportOptions
            {
                CssStyleSheetType = Spire.Doc.CssStyleSheetType.Internal, // Or Embedded
                ImageEmbedded = true                            // 🔥 Embed images as base64
            };

            // Export to HTML file (optional: use temp file if needed)
            string tempHtmlPath = Path.Combine(Path.GetTempPath(), "output.html");
            extractedDoc.SaveToFile(tempHtmlPath, FileFormat.Html);

            // Read HTML content
            string htmlContent = File.ReadAllText(tempHtmlPath);

            // Optional: Delete temp file
              File.Delete(tempHtmlPath);

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(htmlContent));

        }
        private static void PrintPdf(string pdfPath, string printerName, int copies)
        {
            try
            {
                // Load the PDF into Aspose.Words.Document
                Document pdfDoc = new(pdfPath);

                //PrinterSettings printerSettings = new PrinterSettings
                //{
                //    PrinterName = printerName,
                //    Copies = (short)copies,
                //    Collate = true
                //};

                // Print the document
                //pdfDoc.Print(printerSettings);

                //// Use ProcessStartInfo to print the PDF using the default PDF viewer
                //ProcessStartInfo printProcessInfo = new ProcessStartInfo()
                //{
                //    Verb = "print",
                //    FileName = pdfPath,
                //    CreateNoWindow = true,
                //    WindowStyle = ProcessWindowStyle.Hidden
                //};

                //Process printProcess = new Process();
                //printProcess.StartInfo = printProcessInfo;
                //printProcess.Start();

                //printProcess.WaitForInputIdle();
                //System.Threading.Thread.Sleep(3000); // Wait for the print job to start

                //if (!printProcess.CloseMainWindow())
                //{
                //    printProcess.Kill();
                //}
                Console.WriteLine("Printing started...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Printing failed: " + ex.Message);
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
                SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY_HERE");
                Console.WriteLine("License set successfully.");
            }
            catch (Exception e)
            {
                // We do not ship any license with this example,
                // visit the Syn fusion site to obtain either a temporary or permanent license. 
                Console.WriteLine("\nThere was an error setting the license: " + e.Message);
            }
        }
    }
}
