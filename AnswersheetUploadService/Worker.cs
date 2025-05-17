using AnswerSheetUploader;

namespace AnswersheetUploadService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var fromLocation = _configuration.GetSection("AppLocations")["FromLocation"] ?? string.Empty;
            var archieveLocation = _configuration.GetSection("AppLocations")["ArchieveLocation"] ?? string.Empty;
            var ignoreLocation = _configuration.GetSection("AppLocations")["IgnoreLocation"] ?? string.Empty;
            var apiBaseURL = _configuration.GetSection("AppLocations")["APIBaseURL"] ?? string.Empty;

            var uploadHelper = new UploadHelper(fromLocation, archieveLocation, ignoreLocation, apiBaseURL, _logger);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!uploadHelper.IsUploaderRunning)
                    await uploadHelper.UploadAllFilesToAzureBlobAsync();

                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}
