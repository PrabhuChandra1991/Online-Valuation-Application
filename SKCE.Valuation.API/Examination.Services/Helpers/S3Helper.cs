using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
namespace SKCE.Examination.Services.Helpers
{
    public class S3Helper
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName="";
        private readonly ExaminationDbContext _context;

        public S3Helper(IConfiguration configuration, ExaminationDbContext examinationDbContext)
        {
            var awsAccessKey = configuration["AWS:AccessKey"];
            var awsSecretKey = configuration["AWS:SecretKey"];
            var region = configuration["AWS:Region"];
            _bucketName = configuration["AWS:BucketName"];
            _context = examinationDbContext;
            _s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, RegionEndpoint.GetBySystemName(region));
        }

        /// <summary>
        /// Uploads a file to S3 bucket.
        /// </summary>
        public async Task<int?> UploadFileAsync(string key, Stream fileStream, string contentType)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = key,
                    BucketName = _bucketName,
                    ContentType = contentType
                };

                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                // Save file metadata in the database
                var document = new DocumentDetails
                {
                    DocumentName = key,
                    DocumentUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}"
                };

                _context.DocumentDetails.Add(document);
                await _context.SaveChangesAsync();

                return document.DocumentId; // Return the generated Document ID
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads a file from AWS S3 using Document ID.
        /// </summary>
        public async Task<(Stream?, string?)> DownloadFileAsync(int documentId)
        {
            try
            {
                var document = await _context.DocumentDetails.FindAsync(documentId);
                if (document == null)
                    return (null, null);

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = document.DocumentName
                };

                using var response = await _s3Client.GetObjectAsync(request);
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset stream position

                return (memoryStream, document.DocumentName);
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
                return (null, null);
            }
        }
    }
}
