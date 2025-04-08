using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using Spire.Doc;
using Document = SKCE.Examination.Models.DbModels.Common.Document;

namespace SKCE.Examination.Services.Helpers
{
    public class AzureBlobStorageHelper
    {
        public readonly BlobServiceClient _blobServiceClient;
        public readonly string _containerName;
        public readonly string _connectionString;
        private readonly ExaminationDbContext _context;
        public AzureBlobStorageHelper(IConfiguration configuration, ExaminationDbContext context)
        {
             _connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];

            _blobServiceClient = new BlobServiceClient(_connectionString);
            _context = context;
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        public async Task<long> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = blobContainerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            fileStream.Position = 0;
            await blobClient.UploadAsync(fileStream, blobHttpHeaders);

            // Save file metadata in the database
            var document = new Document
            {
                Name = blobClient.Name,
                Url = blobClient.Uri.ToString()
            };
            AuditHelper.SetAuditPropertiesForInsert(document,1);
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document.DocumentId; // Return the generated Document ID
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage.
        /// </summary>
        public async Task<(Stream?, string?)> DownloadFileAsync(long documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return (null, null);

            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = blobContainerClient.GetBlobClient(document.Name);

            if (await blobClient.ExistsAsync())
            {
                BlobDownloadInfo download = await blobClient.DownloadAsync();
                return (download.Content, document.Name); ;
            }

            return (null, null);
        }

        // Download Word document from Azure Blob
        public async Task<Spire.Doc.Document> DownloadWordDocumentFromBlob(string blobName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                using MemoryStream stream = new MemoryStream();
                await blobClient.DownloadToAsync(stream);
                return new Spire.Doc.Document(new MemoryStream(stream.ToArray()));
            }
            else
            {
                throw new Exception($"Blob {blobName} not found in container {_containerName}.");
            }
        }

        // Uploads a file to Azure Blob Storage
        public async Task<long> UploadFileToBlob(string filePath, string fileName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            await using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, new BlobHttpHeaders { ContentType = "application/pdf" });

            // Save file metadata in the database
            var document = new Document
            {
                Name = blobClient.Name,
                Url = blobClient.Uri.ToString()
            };
            AuditHelper.SetAuditPropertiesForInsert(document, 1);
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document.DocumentId; // Return the generated Document ID
        }

        // Uploads a file to Azure Blob Storage
        public async Task<long> UploadDocxFileToBlob(string filePath, string fileName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            await using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, new BlobHttpHeaders { ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document" });

            // Save file metadata in the database
            var document = new Document
            {
                Name = blobClient.Name,
                Url = blobClient.Uri.ToString()
            };
            AuditHelper.SetAuditPropertiesForInsert(document, 1);
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document.DocumentId; // Return the generated Document ID
        }

        /// <summary>
        /// Lists all files in the blob container.
        /// </summary>
        public async Task<List<string>> ListFilesAsync()
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobItems = blobContainerClient.GetBlobsAsync();

            var files = new List<string>();
            await foreach (var blobItem in blobItems)
            {
                files.Add(blobItem.Name);
            }

            return files;
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            return await blobClient.DeleteIfExistsAsync();
        }
    }
}
