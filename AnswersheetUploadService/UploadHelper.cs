using AnswersheetUploadService;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace AnswerSheetUploader
{
    public class UploadHelper
    {
        public bool IsUploaderRunning { get; set; }
        public string FromLocation { get; set; }
        public string ArchieveLocation { get; set; }
        public string IgnoredLocation { get; set; }
        public string APIBaseURL { get; set; }
        private readonly ILogger<Worker> _logger;

        public UploadHelper(string fromLoc, string archieveLoc, string ignoreLoc, string apiBaseUrl, ILogger<Worker> logger)
        {
            this.FromLocation = fromLoc;
            this.ArchieveLocation = archieveLoc;
            this.IgnoredLocation = ignoreLoc;
            this.APIBaseURL = apiBaseUrl;
            this._logger = logger;
        }


        public async Task UploadAllFilesToAzureBlobAsync()
        {
            this.IsUploaderRunning = true;
            await ProcessDirectoryAsync(this.FromLocation);
            this.IsUploaderRunning = false;
        }

        private async Task ProcessDirectoryAsync(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory, "*.pdf");
            foreach (string fileName in fileEntries)
                await ProcessFileAsync(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                await ProcessDirectoryAsync(subdirectory);
        }

        public async Task ProcessFileAsync(string path)
        {
            var names = path.Split("\\");
            var dummyNumber = names[names.Length - 1];
            var courseCode = names[names.Length - 2];
            var response = await UploadAnswersheetAsync(courseCode, dummyNumber, path);

            ProcessFileMove(path, response, courseCode, dummyNumber);

            Console.WriteLine("Processed file '{0}'.", path);
        }

        private void ProcessFileMove(string path, string apiResonse, string courseCode, string dummyNumber)
        {
            switch (apiResonse)
            {
                case "UPLOAD-SUCCESS":
                    MoveToArchieve(path, courseCode, dummyNumber);
                    break;
                case "ALREADY-EXISTS":
                    MoveToIgnore(path, courseCode, dummyNumber);
                    break;
                default:
                    break;
            }
        }

        private void MoveToArchieve(string path, string courseCode, string dummyNumber)
        {
            var archievePath = $"{this.ArchieveLocation}\\{courseCode}";

            if (!Directory.Exists(archievePath))
                Directory.CreateDirectory(archievePath);

            File.Move(path, archievePath + "\\" + dummyNumber);
        }

        private void MoveToIgnore(string path, string courseCode, string dummyNumber)
        {
            var ignorePath = $"{this.IgnoredLocation}\\{courseCode}";

            if (!Directory.Exists(ignorePath))
                Directory.CreateDirectory(ignorePath);

            File.Move(path, ignorePath + "\\" + dummyNumber);
        }


        private async Task<string> UploadAnswersheetAsync(string courseCode, string dummyNumber, string pdfPath)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(600);

            var url = this.APIBaseURL + $"/AnswersheetImport/UploadAnswerSheetData/{courseCode}/{dummyNumber}";

            using var form = new MultipartFormDataContent();

            var pdfContent = new ByteArrayContent(await File.ReadAllBytesAsync(pdfPath));
            pdfContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            form.Add(pdfContent, "File", Path.GetFileName(pdfPath));

            var response = await client.PostAsync(url, form);

            var result = await response.Content.ReadAsStringAsync();

            switch (result)
            {
                case "UPLOAD-SUCCESS":
                    Console.WriteLine($"Successfully uploaded : {dummyNumber}");
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"Uploaded DummyNumber: {dummyNumber} at {DateTimeOffset.Now}");
                    }
                    break;
                case "UPLOAD-FAILED":
                    Console.WriteLine($"Failed to upload : {response.StatusCode}");
                    _logger.LogError($"Failed to upload : {dummyNumber} : {response.StatusCode}");
                    break;
                case "ALREADY-EXISTS":
                    Console.WriteLine($"Failed to upload : {response.StatusCode}");
                    _logger.LogError($"Failed to upload : {dummyNumber} : {response.StatusCode}");
                    break;
                default:
                    break;
            }

            return result;

        }


    }
}
