using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
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
        private readonly ILogger<DocumentCoordinator> _logger;
        private const string FolderName = "Document";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);

        public JobDocumentCoordinator(
            IJobStorageService jobStorageService,
            IDocumentParse documentParse,
            IGenerator generator,
            IDocumentPDF documentPDF,
            IDirectoryCheck directoryCheck,
            ExecutionOptions executionOptions,
            ILogger<DocumentCoordinator> logger)
        {
            _jobStorageService = jobStorageService;
            _documentParse = documentParse;
            _generator = generator;
            _documentPDF = documentPDF;
            _directoryCheck = directoryCheck;
            _executionOptions = executionOptions;
            _logger = logger;

            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _logger.LogInformation("📁 Document directory ensured at: {FolderPath}", FolderPath);
        }

        private async Task SavePendingListAsync(List<JobOffer> pendingList, string path)
        {
            var json = JsonSerializer.Serialize(pendingList, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<IEnumerable<JobOffer>> GenerateJobsDocumentAsync()
        {
            _logger.LogInformation("🚀 Starting document generation process...");

            string filePath = Path.Combine(_executionOptions.ExecutionFolder, _jobStorageService.StorageFile);
            string pendingJobsFilePath = Path.Combine(_executionOptions.ExecutionFolder, "pending_parse_jobs.json");
            string processedJobsFilePath = Path.Combine(_executionOptions.ExecutionFolder, "processed_parse_jobs.json");
            try
            {
                // Intenta cargar los pendientes si existe el archivo temporal
                IEnumerable<JobOffer> listPendingJobOfferDetail, listProcessedJobOfferDetail, process;
                if (File.Exists(pendingJobsFilePath))
                {
                    _logger.LogInformation("♻️ Resuming from pending file: {PendingFile}", pendingJobsFilePath);
                    listPendingJobOfferDetail = await _jobStorageService.LoadJobsAsync(pendingJobsFilePath);
                }
                else
                {
                    listPendingJobOfferDetail = new List<JobOffer>();
                }

                if (File.Exists(processedJobsFilePath))
                {
                    _logger.LogInformation("♻️ Resuming from process file: {PendingFile}", processedJobsFilePath);
                    listProcessedJobOfferDetail = await _jobStorageService.LoadJobsAsync(processedJobsFilePath);
                }
                else
                {
                    listProcessedJobOfferDetail = new List<JobOffer>();
                }

                if (File.Exists(filePath))
                {
                    _logger.LogInformation("♻️ Resuming from process file: {PendingFile}", filePath);
                    process = await _jobStorageService.LoadJobsAsync(filePath);
                }
                else
                {
                    process = new List<JobOffer>();
                }


                int initialCount = listPendingJobOfferDetail.Count() ;
                _logger.LogInformation($"📊 Starting with pendient {initialCount} job offers to process", initialCount);
                var jobOffers = new List<JobOffer>();


                int initialProcessCount = listProcessedJobOfferDetail.Count();
                _logger.LogInformation($"📊 Starting with process {initialProcessCount} job offers to process", initialProcessCount);


                // Se hace copia local para poder eliminar elementos a medida que se procesan
                var pendingList = new List<JobOffer>(listPendingJobOfferDetail);

                foreach (var jobOfferDetail in listPendingJobOfferDetail.ToList()) // Copia para evitar modificar mientras se itera
                {
                    string id = jobOfferDetail.Url;
                    string title = jobOfferDetail.JobOfferTitle;

                    _logger.LogInformation("🔍 Processing job offer | ID: {JobID} | Title: {JobTitle}", id, title);

                    try
                    {
                        var jobOffer = await _documentParse.ParseJobOfferAsync(jobOfferDetail.Description);
                        jobOffer.Description = jobOfferDetail.Description;
                        jobOffer.RawJobDescription = jobOfferDetail.Description.Split(Environment.NewLine).Distinct().ToList();
                        jobOffers.Add(jobOffer);

                        // Eliminar de pendientes y guardar archivo temporal actualizado
                        pendingList.Remove(jobOfferDetail);
                        await SavePendingListAsync(pendingList, pendingJobsFilePath);
                        await SavePendingListAsync(jobOffers, processedJobsFilePath);
                        _logger.LogInformation("✅ Completed | ID: {JobID} | Remaining: {Remaining}", id, pendingList.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing job offer | ID: {JobID} | Message: {ErrorMessage}", id, ex.Message);
                        throw; // Mantiene el comportamiento original
                    }
                }

                // Limpia el archivo temporal si todo se procesó correctamente
                if (File.Exists(pendingJobsFilePath))
                {
                    File.Delete(pendingJobsFilePath);
                    _logger.LogInformation("🧹 Cleared pending file after successful processing.");
                }

                if (File.Exists(processedJobsFilePath))
                {
                    File.Delete(processedJobsFilePath);
                    _logger.LogInformation("🧹 Cleared process file after successful processing.");
                }

                _logger.LogInformation("🎉 Successfully generated {GeneratedCount} job offer documents", jobOffers.Count);
                return jobOffers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Document generation process failed | Message: {ErrorMessage}", ex.Message);
                throw;
            }
        }

    }
}