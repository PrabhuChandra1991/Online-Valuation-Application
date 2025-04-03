﻿using DocumentFormat.OpenXml.Packaging;
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
using Syncfusion.DocIO.DLS;
using Spire.Doc.Collections;
using Body = Spire.Doc.Body;
using Syncfusion.DocIO;
using SKCE.Examination.Services.ViewModels.QPSettings;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Math;

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
        public async Task ProcessBookmarksAndPrint(QPTemplate qPTemplate,UserQPTemplate userQPTemplate, string inputDocPath, string documentPathToPrint, bool isForPrint)
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
                    { "PROGRAMME", "" },
                    { "COURSECODE", "" },
                    { "COURSETITLE", "" },
                    { "SEMESTER", "" },
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
                        var departmentVMs = examinations.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId, (cd, d) => new { cd, d })
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
                        bookmarkUpdates[bookmark.Key] = bookmarkHtml;
                    }
                    else if (bookmarkName == "COURSECODE")
                    {
                        bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code ?? string.Empty;
                    }
                    else if (bookmarkName == "COURSETITLE")
                    {
                        bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                    }
                    else if (bookmarkName == "SEMESTER")
                    {
                        bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.Semester.ToString() ?? string.Empty;
                    }
                    else if (bookmarkName == "QPCODE")
                    {
                        bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.QPCode.ToString() ?? string.Empty;
                    }
                    // Replace bookmark content in the template document
                    //ReplaceBookmarkWithHtml(tempDoc, bookmarkName, bookmarkHtml);
                }

                // Loop through each bookmark and update text
                foreach (var bookmark in bookmarkUpdates)
                {
                    Spire.Doc.Bookmark bookMark = sourceDoc.Bookmarks[bookmark.Key];
                    if(bookMark != null)
                    bookMark.BookmarkStart.OwnerParagraph.Text = bookmark.Value;
                }

                sourceDoc.SaveToFile(updatedSourcePath, FileFormat.Docx);

                Document updatedSourcedoc = new Document();
                updatedSourcedoc.LoadFromFile(updatedSourcePath);

                // Iterate through all bookmarks in the source document
                foreach (Spire.Doc.Bookmark bookmark in updatedSourcedoc.Bookmarks)
                {
                    string bookmarkName = bookmark.Name;
                    // Find the same bookmark in the destination document
                    Spire.Doc.Bookmark destinationBookmark = templateDoc.Bookmarks.FindByName(bookmarkName);
                    if (destinationBookmark != null)
                    {
                        // Extract content from the source bookmark (including images)
                        DocumentObjectCollection sourceContent = bookmark.BookmarkStart.OwnerParagraph.ChildObjects;

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
                // Trigger printing
                PrintPdf(outputPdfPath, printerName, numberOfCopies);

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
        private static void PrintPdf(string pdfPath, string printerName, int copies)
        {
            try
            {
                // Load the PDF into Aspose.Words.Document
                Document pdfDoc = new (pdfPath);

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
        private void LoadLicense() {
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
