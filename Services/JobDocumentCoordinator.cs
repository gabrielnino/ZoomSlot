using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Configuration;
using Services.Interfaces;

namespace Services
{
    public class JobDocumentCoordinator : IJobDocumentCoordinator
    {
        private readonly IJobStorageService _jobStorageService;
        private readonly IDocumentParse _documentParse;
        private readonly IGenerator _generator;
        private readonly IDocumentPDF _documentPDF;
        private readonly ExecutionOptions _executionOptions;
        private readonly IDirectoryCheck _directoryCheck;
        private readonly ILogger<JobDocumentCoordinator> _logger;
        private readonly AppConfig _appConfig;
        private const string FolderName = "Document";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private string DetailOffersFilePath => Path.Combine(_executionOptions.ExecutionFolder, _appConfig.FilePaths.DetailOutputFilePath);
        public JobDocumentCoordinator(
            IJobStorageService jobStorageService,
            IDocumentParse documentParse,
            IGenerator generator,
            IDocumentPDF documentPDF,
            IDirectoryCheck directoryCheck,
            ExecutionOptions executionOptions,
            ILogger<JobDocumentCoordinator> logger,
            AppConfig appConfig)
        {
            _jobStorageService = jobStorageService;
            _documentParse = documentParse;
            _generator = generator;
            _documentPDF = documentPDF;
            _directoryCheck = directoryCheck;
            _executionOptions = executionOptions;
            _logger = logger;
            _appConfig = appConfig;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _logger.LogInformation("📁 Document directory ensured at: {FolderPath}", FolderPath);
        }

        private async Task SaveToFileAsync(List<JobOfferDetail> list, string path)
        {
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        private async Task SaveToFileJobAsync(List<JobOffer> list, string path)
        {
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<IEnumerable<JobOffer>> GenerateJobsDocumentAsync()
        {
            _logger.LogInformation("🚀 Starting document generation process...");
            string sourceFilePath = Path.Combine(_executionOptions.ExecutionFolder, DetailOffersFilePath);
            string pendingJobsFilePath = Path.Combine(_executionOptions.ExecutionFolder, "pending_parse_jobs.json");
            string processedJobsFilePath = Path.Combine(_executionOptions.ExecutionFolder, _appConfig.Paths.InputFile);
            try
            {
                var pendingJobs = File.Exists(pendingJobsFilePath) ? await _jobStorageService.LoadJobsDetailAsync(pendingJobsFilePath): [];
                var processedJobs = File.Exists(processedJobsFilePath)? await _jobStorageService.LoadJobsAsync(processedJobsFilePath): [];
                var allJobs = File.Exists(sourceFilePath) ? await _jobStorageService.LoadJobsDetailAsync(sourceFilePath): [];
                _logger.LogInformation("📊 {PendingCount} pending job offers loaded", pendingJobs.Count());
                _logger.LogInformation("📊 {ProcessedCount} previously processed job offers loaded", processedJobs.Count());
                var results = processedJobs.Count() > 0 ? [.. processedJobs] : new List<JobOffer>();
                var pendingList = pendingJobs.Count() == 0 ? [.. allJobs] : new List<JobOfferDetail>(pendingJobs);
                var offers = pendingList.ToList();
                foreach (var job in offers)
                {
                    _logger.LogInformation("🔍 Processing job offer | ID: {JobID} | Title: {Title}", job.Link, job.JobOfferTitle);
                    try
                    {
                        var parsedJob = await _documentParse.ParseJobOfferAsync(job.Description);
                        //parsedJob.Description = job.Description;
                        parsedJob.RawJobDescription = job.Description.Split(Environment.NewLine).Distinct().ToList();
                        parsedJob.Url = job.Link;
                        parsedJob.Id = job.ID;
                        results.Add(parsedJob);
                        pendingList.Remove(job);
                        await SaveToFileAsync(pendingList, pendingJobsFilePath);
                        await SaveToFileJobAsync(results, processedJobsFilePath);
                        _logger.LogInformation("✅ Processed | ID: {JobID} | Remaining: {Remaining}", job.Link, pendingList.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing job offer | ID: {JobID}", job.Link);
                        throw;
                    }
                }

                // Cleanup
                if (File.Exists(pendingJobsFilePath)) File.Delete(pendingJobsFilePath);
                _logger.LogInformation("🎉 Successfully generated {Count} job offer documents", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Document generation failed | Message: {Message}", ex.Message);
                throw;
            }
        }
    }
}
