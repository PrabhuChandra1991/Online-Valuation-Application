using Aspose.Words;
using System.Drawing.Printing;
using SKCE.Examination.Models.DbModels.QPSettings;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using Document = Aspose.Words.Document;
using System.Diagnostics;
using System.Text;
using MigraDoc.Rendering;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using Aspose.Pdf.Facades;

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
        public async Task ProcessBookmarksAndPrint(QPTemplate qPTemplate, QPTemplateInstitution qPTemplateInstitution, string inputDocPath, string documentPathToPrint, bool isForPrint)
        {
            try
            {
                //Loading license
                LoadLicense();

                int numberOfCopies = 0; // Number of copies to print
                // Load the source document that contains the bookmarks
                Document sourceDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(inputDocPath);

                // Load the template document where bookmarks need to be replaced
                Document templateDoc = await _azureBlobStorageHelper.DownloadWordDocumentFromBlob(documentPathToPrint);

                // Iterate through all bookmarks in the source document
                foreach (Aspose.Words.Bookmark bookmark in sourceDoc.Range.Bookmarks)
                {
                    string bookmarkName = bookmark.Name;
                    string bookmarkHtml = string.Empty;

                    if (bookmarkName == "PROGRAMME")
                    {
                        var departmentIds = _context.QPTemplateInstitutionDepartments.Where(q=> q.QPTemplateInstitutionId == qPTemplateInstitution.QPTemplateInstitutionId).Select(q=>q.DepartmentId).ToList();
                        if (departmentIds.Any() && departmentIds.Count > 2) { 
                            bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.ShortName).ToList());
                        }else
                            bookmarkHtml = String.Join(", ", _context.Departments.Where(d => departmentIds.Contains(d.DepartmentId)).Select(d => d.Name).ToList());
                    } 
                    else if (bookmarkName == "CourseCode")
                    {
                        bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Code?? string.Empty;
                    }
                    else if( bookmarkName == "CourseTitle")
                    {
                        bookmarkHtml = _context.Courses.FirstOrDefault(c => c.CourseId == qPTemplate.CourseId)?.Name ?? string.Empty;
                    }
                    else if (bookmarkName == "Semester")
                    {
                        bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.Semester.ToString() ?? string.Empty;
                    }
                    else if (bookmarkName == "QPCODE")
                    {
                        bookmarkHtml = _context.QPTemplates.FirstOrDefault(c => c.QPTemplateId == qPTemplate.QPTemplateId)?.QPCode.ToString() ?? string.Empty;
                    }
                    else
                    {
                        Node bookmarkContent = bookmark.BookmarkStart.ParentNode;
                        if (bookmarkContent != null)
                        {
                            //// Extract content inside the bookmark
                            //Node[] extractedNodes = ExtractContent(bookmark.BookmarkStart, bookmark.BookmarkEnd);

                            //if (extractedNodes != null && extractedNodes.Length > 0)
                            //{
                            //    // Convert extracted nodes to HTML string
                            //    bookmarkHtml = ConvertNodesToHtml(extractedNodes);
                            //}
                            bookmarkHtml = ConvertBookmarkRangeToHtml(bookmark.BookmarkStart, bookmark.BookmarkEnd);
                        }
                    }
                    // Replace bookmark content in the template document
                    ReplaceBookmarkWithHtml(templateDoc, bookmarkName, bookmarkHtml);
                }

                // Save the modified document as PDF
                var previewPdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}_{3}_{4}.pdf", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, DateTime.UtcNow.ToLongDateString(), DateTime.UtcNow.ToShortTimeString()));
                templateDoc.Save(previewPdfPath, SaveFormat.Pdf);

                var securePdfPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), string.Format("{0}_{1}_{2}_{3}_{4}_Secure.pdf", qPTemplate.QPTemplateName, qPTemplate.QPCode, qPTemplate.ExamYear, DateTime.UtcNow.ToLongDateString(), DateTime.UtcNow.ToShortTimeString()));
                SecurePdf(previewPdfPath, securePdfPath);
                OpenPdfInBrowser(securePdfPath);

                if(!isForPrint) return;
                var documentId = _azureBlobStorageHelper.UploadFileToBlob(outputPdfPath,string.Format("{0}_{1}_{2}_{3}.pdf",qPTemplate.QPTemplateName,qPTemplate.QPCode,qPTemplate.ExamYear,DateTime.UtcNow.ToShortDateString()));
                // Trigger printing
                PrintPdf(outputPdfPath, printerName, numberOfCopies);

                Console.WriteLine("Processing and printing completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
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

        /// <summary>
        /// Opens the generated PDF in the default web browser.
        /// </summary>
        /// <param name="pdfPath">Path to the generated PDF.</param>
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


        /// <summary>
        /// Applies security settings to disable printing and saving.
        /// </summary>
        public static void SecurePdf(string inputPdfPath, string outputPdfPath)
        {
            // Load the PDF document
            Aspose.Pdf.Document pdfDocument = new (inputPdfPath);

            // Set security privileges
            DocumentPrivilege privileges = DocumentPrivilege.ForbidAll; // Deny all permissions
            privileges.AllowScreenReaders = true; // Allow screen readers if needed

            // Apply security settings
            PdfFileSecurity fileSecurity = new PdfFileSecurity(pdfDocument);
            fileSecurity.SetPrivilege(privileges);

            // Save the secured PDF
            pdfDocument.Save(outputPdfPath);

            Console.WriteLine("Secured PDF created successfully!");
        }


        // Extracts content between bookmark start and end
        private static Node[] ExtractContent(Node startNode, Node endNode)
        {
            var extractedNodes = new System.Collections.Generic.List<Node>();
            Node currentNode = startNode;

            while (currentNode != null && currentNode != endNode)
            {
                extractedNodes.Add(currentNode.Clone(true)); // Clone to preserve formatting
                currentNode = currentNode.NextSibling;
            }

            return extractedNodes.ToArray();
        }

        // Converts extracted nodes to an HTML string
        private static string ConvertNodesToHtml(Node[] nodes)
        {
            if (nodes == null || nodes.Length == 0) return string.Empty;

            // Create a temporary document
            Document tempDoc = new Document();
            DocumentBuilder builder = new DocumentBuilder(tempDoc);

            foreach (Node node in nodes)
            {
                builder.InsertNode(node.Clone(true));
            }

            // Convert to HTML
            using (MemoryStream htmlStream = new MemoryStream())
            {
                tempDoc.Save(htmlStream, SaveFormat.Html);
                return System.Text.Encoding.UTF8.GetString(htmlStream.ToArray());
            }
        }

        // Replaces the bookmark content with an HTML string
        private static void ReplaceBookmarkWithHtml(Document doc, string bookmarkName, string htmlContent)
        {
            if (doc.Range.Bookmarks[bookmarkName] != null)
            {
                DocumentBuilder builder = new DocumentBuilder(doc);
                builder.MoveToBookmark(bookmarkName, true, true);
                builder.InsertHtml(htmlContent);
            }
        }

        // Prints the PDF with a specified number of copies
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
            // For complete examples and data files, please go to https://github.com/aspose-words/Aspose.Words-for-.NET.git.
            License license = new License();

            // This line attempts to set a license from several locations relative to the executable and Aspose.Words.dll.
            // You can also use the additional overload to load a license from a stream, this is useful,
            // for instance, when the license is stored as an embedded resource.
            try
            {

                license.SetLicense(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, asposeLicenseFileName));
                Console.WriteLine("License set successfully.");
            }
            catch (Exception e)
            {
                // We do not ship any license with this example,
                // visit the Aspose site to obtain either a temporary or permanent license. 
                Console.WriteLine("\nThere was an error setting the license: " + e.Message);
            }
        }
    }
}
