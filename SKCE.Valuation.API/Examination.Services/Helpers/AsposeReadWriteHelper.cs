using Aspose.Words;
using Aspose.Words.Drawing;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Aspose.Words.Replacing;
using Aspose.Words.Saving;
using System.Linq;
using System.Text;
public static class AsposeReadWriteHelper
    {
        /// <summary>
        /// Reads content from a bookmark, including formatted text, images, and tables,
        /// converts it to an HTML string, and returns it as a Base64-encoded string.
        /// </summary>
        /// <param name="inputFilePath">Path to the input Word document</param>
        /// <param name="bookmarkName">Name of the bookmark to extract content from</param>
        /// <returns>Base64-encoded HTML string</returns>
        public static string GetBookmarkContentAsBase64Html(string inputFilePath, string bookmarkName)
        {
            // Load the Word document
            Document doc = new Document(inputFilePath);

            // Check if the bookmark exists
            //if (!doc.Range.Bookmarks.Contains(bookmarkName))
            //{
            //    throw new Exception($"Bookmark '{bookmarkName}' not found in the document.");
            //}
            // Search for the bookmark by name
            Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];

            if (bookmark == null)
            {
                throw new ArgumentException("Bookmark not found in the document");
            }


            // Extract the HTML content from the bookmark range (including formatting)
            string htmlContent = ConvertBookmarkRangeToHtml(bookmark.BookmarkStart, bookmark.BookmarkEnd);

            // Extract images in the bookmark range and convert them to Base64
            List<string> base64Images = ExtractImagesAsBase64(doc, bookmark.BookmarkStart, bookmark.BookmarkEnd);

            // Add image references to the HTML content (inserting Base64 images)
            htmlContent = EmbedImagesInHtml(htmlContent, base64Images);

            //// Extract the text inside the bookmark
            //string bookmarkText = bookmark.Text;
            //// Convert the bookmark text to HTML
            //string htmlContent = ConvertTextToHtml(bookmarkText);


            //// Extract the content from the bookmark
            //Document bookmarkDoc = ExtractBookmarkContent(doc, bookmark);

            //// Save the extracted content as an HTML string with Base64-encoded images
            //string htmlContent = ConvertDocumentToHtml(bookmarkDoc);

            // Encode the HTML content as Base64
            return ConvertToBase64(htmlContent);
        }

        private static string ConvertBookmarkRangeToHtml(Node startNode, Node endNode)
        {
            // Create a temporary document with just the bookmark range
            Document tempDoc = new Document();
            DocumentBuilder builder = new DocumentBuilder(tempDoc);

            Node currentNode = startNode;
            while (currentNode != null && currentNode != endNode)
            {
                Node importedNode = tempDoc.ImportNode(currentNode, true);
                builder.InsertNode(importedNode);
                currentNode = currentNode.NextSibling;
            }

            // Convert the document range to HTML
            using (MemoryStream ms = new MemoryStream())
            {
                tempDoc.Save(ms, SaveFormat.Html);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static List<string> ExtractImagesAsBase64(Document doc, Node startNode, Node endNode)
        {
            List<string> base64Images = new List<string>();

            // Extract the image nodes within the range between startNode and endNode
            foreach (Shape shape in doc.GetChildNodes(NodeType.Shape, true))
            {
                // Ensure that the shape lies within the bookmark range
                if (shape.HasImage && IsNodeInRange(shape, startNode, endNode))
                {
                    byte[] imageBytes;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        shape.ImageData.Save(ms);
                        imageBytes = ms.ToArray();
                    }
                    string base64Image = Convert.ToBase64String(imageBytes);

                    // Optionally add "data:image" to identify the image type
                    string imageType = shape.ImageData.ImageType.ToString().ToLower();
                    base64Images.Add($"data:image/{imageType};base64,{base64Image}");
                }
            }

            return base64Images;
        }

        private static bool IsNodeInRange(Node node, Node startNode, Node endNode)
        {
            // Check if the node is in the bookmark range by comparing its positions
            return node.Document.GetChildNodes(NodeType.Paragraph, true).Contains(startNode) &&
                   node.Document.GetChildNodes(NodeType.Paragraph, true).Contains(endNode);
        }

        private static string EmbedImagesInHtml(string htmlContent, List<string> base64Images)
        {
            int imageIndex = 0;

            // Embed each image into the HTML content
            foreach (var image in base64Images)
            {
                // Replace image placeholders (for example: <img src="image_placeholder_{index}"/>
                string placeholder = $"image_placeholder_{imageIndex++}";
                htmlContent = htmlContent.Replace(placeholder, image);
            }

            return htmlContent;
        }
        private static string ConvertTextToHtml(string text)
        {
            // Create simple HTML content wrapping the text
            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<body>");
            sb.Append("<p>").Append(text).Append("</p>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }
        /// <summary>
        /// Extracts the content within a bookmark and returns it as a separate document.
        /// </summary>
        private static Document ExtractBookmarkContent(Document doc, Bookmark bookmark)
        {
            // Create an empty document
            Document extractedDoc = new Document();
            DocumentBuilder builder = new DocumentBuilder(extractedDoc);

            // Clone nodes from the bookmark start to end
            Node currentNode = bookmark.BookmarkStart;
            while (currentNode != null && currentNode != bookmark.BookmarkEnd)
            {
                Node importedNode = extractedDoc.ImportNode(currentNode, true, ImportFormatMode.KeepSourceFormatting);
                if(importedNode.NodeType != NodeType.Run)
                extractedDoc.FirstSection.Body.AppendChild(importedNode);
                
                currentNode = currentNode.NextSibling;
            }

            return extractedDoc;
        }

        /// <summary>
        /// Converts a document to an HTML string with Base64-encoded images.
        /// </summary>
        private static string ConvertDocumentToHtml(Document doc)
        {
            HtmlSaveOptions saveOptions = new HtmlSaveOptions(SaveFormat.Html)
            {
                ExportImagesAsBase64 = true, // Convert images to Base64
                PrettyFormat = true // Format the HTML output
            };

            using (MemoryStream htmlStream = new MemoryStream())
            {
                doc.Save(htmlStream, saveOptions);
                return Encoding.UTF8.GetString(htmlStream.ToArray());
            }
        }

        /// <summary>
        /// Encodes a string to Base64.
        /// </summary>
        private static string ConvertToBase64(string text)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }

        /// <summary>
        /// Replaces placeholder tags in a Word document with actual Base64 images and converts it to PDF.
        /// </summary>
        /// <param name="inputFilePath">Path to the input Word document</param>
        /// <param name="outputPdfPath">Path to save the converted PDF</param>
        /// <param name="placeholderTag">The placeholder tag to be replaced (default: "[Image Placeholder]")</param>
        /// <param name="base64Image">The Base64 image string</param>
        public static void ReplacePlaceholderWithBase64ImageAndConvertToPdf(
            string inputFilePath,
            string outputPdfPath,
            string placeholderTag,
            string base64Image)
        {
            // Load the Word document
            Document doc = new Document(inputFilePath);

            // Decode the Base64 string to an image
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            // Replace all placeholders with the actual image
            foreach (Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
            {
                foreach (Run run in para.Runs)
                {
                    if (run.Text.Contains(placeholderTag))
                    {
                        InsertImageAtRun(doc, run, imageBytes);
                        run.Text = string.Empty; // Remove placeholder text
                    }
                }
            }

            // Save the updated document as PDF
            doc.Save(outputPdfPath, SaveFormat.Pdf);

            Console.WriteLine($"PDF saved at: {outputPdfPath}");
        }

        /// <summary>
        /// Inserts an image at the position of a specified Run node.
        /// </summary>
        private static void InsertImageAtRun(Document doc, Run run, byte[] imageBytes)
        {
            DocumentBuilder builder = new DocumentBuilder(doc);
            builder.MoveTo(run); // Move to the run position
            Shape imageShape = builder.InsertImage(imageBytes); // Insert the image
            imageShape.WrapType = WrapType.Inline; // Keep image inline
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
        /// Replaces a placeholder tag with an HTML Base64 string in a Word document and converts it to a PDF.
        /// </summary>
        /// <param name="inputFilePath">Path to the input Word document</param>
        /// <param name="outputPdfPath">Path to save the converted PDF</param>
        /// <param name="placeholderTag">The placeholder tag to be replaced (e.g., "[HtmlContent]")</param>
        /// <param name="base64Html">The Base64-encoded HTML string</param>
        public static void ReplacePlaceholderWithHtmlAndConvertToPdf(
            string inputFilePath,
            string outputPdfPath,
            string placeholderTag,
            string base64Html)
        {
            // Load the Word document
            Document doc = new Document(inputFilePath);

            // Decode the Base64 HTML string
            string htmlContent = DecodeBase64(base64Html);

            // Replace placeholder with HTML content
            FindReplaceOptions options = new FindReplaceOptions
            {
                ReplacingCallback = new ReplaceWithHtmlCallback(htmlContent),
                Direction = FindReplaceDirection.Forward
            };

            doc.Range.Replace(placeholderTag, "", options);

            // Save the updated document as PDF
            doc.Save(outputPdfPath, SaveFormat.Pdf);

            Console.WriteLine($"PDF saved at: {outputPdfPath}");
        }

        /// <summary>
        /// Decodes a Base64 string to a normal string.
        /// </summary>
        private static string DecodeBase64(string base64Text)
        {
            byte[] textBytes = Convert.FromBase64String(base64Text);
            return Encoding.UTF8.GetString(textBytes);
        }

        /// <summary>
        /// Callback class to insert HTML content when replacing text.
        /// </summary>
        private class ReplaceWithHtmlCallback : IReplacingCallback
        {
            private readonly string _htmlContent;

            public ReplaceWithHtmlCallback(string htmlContent)
            {
                _htmlContent = htmlContent;
            }

            public ReplaceAction Replacing(ReplacingArgs e)
            {
                DocumentBuilder builder = new DocumentBuilder((Document)e.MatchNode.Document);
                builder.MoveTo(e.MatchNode);
                builder.InsertHtml(_htmlContent);
                e.MatchNode.Remove(); // Remove the placeholder
                return ReplaceAction.Skip;
            }
        }
    }
