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
using Syncfusion.DocIO.DLS;
using Spire.Doc.Collections;
using Body = Spire.Doc.Body;
using Syncfusion.DocIO;

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
        //public async Task ProcessBookmarksAndPrint(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, string inputDocPath, string documentPathToPrint, bool isForPrint)
        //{
        //    try
        //    {
        //        //Loading license
        //        LoadLicense();

        //        int numberOfCopies = 0; // Number of copies to print
        //        // Load the source document that contains the bookmarks
        //        Document sourceDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(inputDocPath);

        //        // Load the template document where bookmarks need to be replaced
        //        Document templateDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(documentPathToPrint);

        //        // Iterate through all bookmarks in the source document
        //        foreach (Spire.Doc.Bookmark bookmark in sourceDoc.Bookmarks)
        //        {
        //            string bookmarkName = bookmark.Name;
        //            string bookmarkHtml = string.Empty;

        //            if (bookmarkName == "PROGRAMME")
        //            {
        //                var departmentIds = _context.QPTemplateInstitutionDepartments.Where(q=> q.QPTemplateInstitutionId == qPTemplateInstitution.QPTemplateInstitutionId).Select(q=>q.DepartmentId).ToList();
        //                if (departmentIds.Any() && departmentIds.Count > 2) { 
        //                    bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.ShortName).ToList());
        //                }else
        //                    bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.Name).ToList());
        //            } 
        //            else if (bookmarkName == "CourseCode")
        //            {
        //                bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code?? string.Empty;
        //            }
        //            else if( bookmarkName == "CourseTitle")
        //            {
        //                bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
        //            }
        //            else if (bookmarkName == "Semester")
        //            {
        //                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.Semester.ToString() ?? string.Empty;
        //            }
        //            else if (bookmarkName == "QPCODE")
        //            {
        //                bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.QPCode.ToString() ?? string.Empty;
        //            }
        //            else
        //            {
        //                // Create a temporary document
        //                Document tempDoc = new Document();

        //                // Find the same bookmark in the destination document
        //                Spire.Doc.Bookmark destinationBookmark = templateDoc.Bookmarks.FindByName(bookmarkName);
        //                if (destinationBookmark != null)
        //                {
        //                    Section section = tempDoc.AddSection();
        //                    Body body = section.Body;

        //                    // Copy content from the original document's bookmark
        //                    foreach (DocumentObject obj in bookmark.BookmarkStart.OwnerParagraph.OwnerTextBody.ChildObjects)
        //                    {
        //                        body.ChildObjects.Add(obj.Clone());
        //                    }

        //                    // Save the extracted content as an HTML string
        //                    using (MemoryStream ms = new MemoryStream())
        //                    {
        //                        tempDoc.SaveToStream(ms, FileFormat.Html);
        //                        System.Text.Encoding.UTF8.GetString(ms.ToArray());
        //                    }
        //                }
        //            }
        //            // Replace bookmark content in the template document
        //            //ReplaceBookmarkWithHtml(tempDoc, bookmarkName, bookmarkHtml);
        //        }

        //        var previewdocPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.docx", qPTemplate.QPTemplateName, DateTime.Now.ToString("ddMMyyyyhhmmss")));
        //        templateDoc.SaveToFile(previewdocPath, FileFormat.Docx);

        //        //// Remove evaluation watermark from the output document By OpenXML
        //        RemoveTextFromDocx(previewdocPath, "Evaluation Warning: The document was created with Spire.Doc for .NET.");
        //        Console.WriteLine("Bookmarks replaced successfully!");

        //        // Save the modified document as PDF
        //        var previewPdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}.pdf", qPTemplate.QPTemplateName, DateTime.Now.ToString("ddMMyyyyhhmmss")));
        //        ConvertToPdfBySyncfusion(previewdocPath, previewPdfPath);

        //        OpenPdfInBrowser(previewPdfPath);

        //        if(!isForPrint) return;
        //        var documentId = _azureBlobStorageHelper.UploadFileToBlob(outputPdfPath,string.Format("{0}_{1}_{2}_{3}.pdf",qPTemplate.QPTemplateName,qPTemplate.QPCode,qPTemplate.ExamYear,DateTime.UtcNow.ToShortDateString()));
        //        // Trigger printing
        //        PrintPdf(outputPdfPath, printerName, numberOfCopies);

        //        Console.WriteLine("Processing and printing completed successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: " + ex.Message);
        //    }
        //}

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
            OpenPdfInBrowser(pdfPath);
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
