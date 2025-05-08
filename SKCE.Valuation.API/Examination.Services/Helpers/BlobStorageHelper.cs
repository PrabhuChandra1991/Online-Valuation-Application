using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace SKCE.Examination.Services.Helpers
{
    public class BlobStorageHelper
    {
        public readonly BlobServiceClient _blobServiceClient;
        public readonly string _containerName;
        public readonly string _connectionString;

        public BlobStorageHelper(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = blobContainerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            fileStream.Position = 0;
            await blobClient.UploadAsync(fileStream, blobHttpHeaders);

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        public async Task<bool> ExistsAsync(string fileLocation)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);             
            var blobClient = blobContainerClient.GetBlobClient(fileLocation);
            return await blobClient.ExistsAsync(); 
        }


    }
}
