using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class JsonJobStorageService : IJobStorageService, IDisposable
    {
        private readonly string? StorageFile;
        private readonly ILogger<JsonJobStorageService> _logger;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly ExecutionOptions _executionOptions;

        public JsonJobStorageService(ILogger<JsonJobStorageService> logger, ExecutionOptions executionOptions)
        {
            _logger = logger;
            _executionOptions = executionOptions;
            if(StorageFile == null)
            {
                StorageFile = $"jobs_data_{_executionOptions.TimeStamp}.json";
            }
            EnsureStorageDirectoryExists();
        }

        public async Task SaveJobsAsync(IEnumerable<JobOfferDetail> jobs)
        {
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));

            await _fileLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(jobs, Formatting.Indented);
                await File.WriteAllTextAsync(StorageFile, json);
                _logger.LogInformation("✅ Saved {JobCount} job details to storage", jobs.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save job details");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<IEnumerable<JobOfferDetail>> LoadJobsAsync()
        {
            if (!File.Exists(StorageFile))
            {
                _logger.LogInformation("⚠️ Storage file not found");
                return Enumerable.Empty<JobOfferDetail>();
            }
            
            await _fileLock.WaitAsync();
            try
            {
                var directoryPath = Directory.GetCurrentDirectory();
                var directories = Directory.GetDirectories(directoryPath, $"*{ExecutionOptions.FolderName}_*", SearchOption.TopDirectoryOnly);
                var lastFolder = directories.OrderByDescending(d => d).FirstOrDefault();
                var filePath = Directory.GetFiles(lastFolder, $"jobs_data_{_executionOptions.TimeStamp}.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
                var json = await File.ReadAllTextAsync(filePath);
                var jobs = JsonConvert.DeserializeObject<List<JobOfferDetail>>(json) ?? new List<JobOfferDetail>();
                _logger.LogInformation("✅ Loaded {JobCount} job details", jobs.Count);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load job details");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<int> GetJobCountAsync()
        {
            var jobs = await LoadJobsAsync();
            return jobs.Count();
        }

        public async Task ClearStorageAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(StorageFile))
                {
                    File.Delete(StorageFile);
                    _logger.LogInformation("✅ Cleared job storage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to clear storage");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public void Dispose()
        {
            _fileLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private void EnsureStorageDirectoryExists()
        {
            var directory = Path.GetDirectoryName(StorageFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
