using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using System.IO;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Helpers
{
    public class AzureBlobStorageHelper
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ExaminationDbContext _context;
        public AzureBlobStorageHelper(IConfiguration configuration, ExaminationDbContext context)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];

            _blobServiceClient = new BlobServiceClient(connectionString);
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
            _context.DocumentDetails.Add(document);
            await _context.SaveChangesAsync();

            return document.DocumentId; // Return the generated Document ID
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage.
        /// </summary>
        public async Task<(Stream?, string?)> DownloadFileAsync(long documentId)
        {
            var document = await _context.DocumentDetails.FindAsync(documentId);
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
