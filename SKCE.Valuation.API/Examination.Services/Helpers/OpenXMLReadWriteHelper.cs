using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Helpers
{
  public  class OpenXMLReadWriteHelper
    {
        public static List<string> ExtractTags(Stream fileStream)
        {
            var tags = new List<string>();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(fileStream, false))
            {
                var text = wordDoc.MainDocumentPart.Document.Body.InnerText;
                var matches = Regex.Matches(text, @"<<([^<>]+)>>");
                foreach (Match match in matches)
                {
                    tags.Add(match.Groups[1].Value);
                }
            }

            return tags.Distinct().ToList();
        }
    }
}
