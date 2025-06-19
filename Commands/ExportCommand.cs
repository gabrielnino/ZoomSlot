using Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.interfaces;
using Formatting = Newtonsoft.Json.Formatting;

namespace Commands
{
    public class ExportCommand : ICommand
    {
        private readonly IJobStorageService _storageService;
        private readonly ILogger<ExportCommand> _logger;
        private readonly AppConfig _config;

        public ExportCommand(
            IJobStorageService storageService,
            ILogger<ExportCommand> logger,
            AppConfig config)
        {
            _storageService = storageService;
            _logger = logger;
            _config = config;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting job data export...");

                // Get jobs from storage
                var jobs = await _storageService.LoadJobsAsync();

                if (jobs == null || !jobs.Any())
                {
                    _logger.LogWarning("No jobs found to export");
                    Console.WriteLine("No jobs available for export");
                    return;
                }

                // Create export directory if it doesn't exist
                var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "Exports");
                Directory.CreateDirectory(exportDir);

                // Generate filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var exportPath = Path.Combine(exportDir, $"jobs_export_{timestamp}.json");

                // Serialize and save to file
                var json = JsonConvert.SerializeObject(jobs, Formatting.Indented);
                await File.WriteAllTextAsync(exportPath, json);

                _logger.LogInformation("Successfully exported {JobCount} jobs to {ExportPath}",jobs?.Count() ?? 0,   exportPath);
                Console.WriteLine($"Exported {jobs?.Count() ?? 0} jobs to:\n{exportPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export jobs");
                throw;  // Let the global error handler catch this
            }
        }
    }

}
