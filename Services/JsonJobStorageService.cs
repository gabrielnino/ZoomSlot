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

        public async Task SaveJobOfferDetailAsync(IEnumerable<JobOfferDetail> jobs)
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

        public async Task SaveJobsAsync(IEnumerable<string> jobs)
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


        public async Task<IEnumerable<string>> LoadJobsAsync()
        {
            try
            {
                var lastFolder = GetLatestJobFolder();
                if (lastFolder == null)
                {
                    _logger.LogWarning("⚠️ No matching job folders found");
                    return [];
                }
                var filePath = GetJobDataFilePath(lastFolder);
                if (filePath == null || !File.Exists(filePath))
                {
                    _logger.LogWarning("⚠️ Job data file not found in '{LastFolder}'", lastFolder);
                    return [];
                }
                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("⚠️ Job data file '{FilePath}' is empty", filePath);
                    return [];
                }
                var jobs = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                _logger.LogInformation("✅ Loaded {JobCount} job details from '{FilePath}'", jobs.Count, filePath);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load job details");
                throw;
            }
        }

        public async Task<IEnumerable<JobOfferDetail>> LoadJobsDetailAsync()
        {
            try
            {
                var lastFolder = GetLatestJobFolder();
                if (lastFolder == null)
                {
                    _logger.LogWarning("⚠️ No matching job folders found");
                    return Enumerable.Empty<JobOfferDetail>();
                }
                var filePath = GetJobDataFilePath(lastFolder);
                if (filePath == null || !File.Exists(filePath))
                {
                    _logger.LogWarning("⚠️ Job data file not found in '{LastFolder}'", lastFolder);
                    return Enumerable.Empty<JobOfferDetail>();
                }
                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("⚠️ Job data file '{FilePath}' is empty", filePath);
                    return Enumerable.Empty<JobOfferDetail>();
                }
                var jobs = JsonConvert.DeserializeObject<List<JobOfferDetail>>(json) ?? new List<JobOfferDetail>();
                _logger.LogInformation("✅ Loaded {JobCount} job details from '{FilePath}'", jobs.Count, filePath);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load job details");
                throw;
            }
        }

        private string? GetLatestJobFolder()
        {
            var directoryPath = Directory.GetCurrentDirectory();
            return directoryPath;
        }

        private string? GetJobDataFilePath(string folderPath)
        {
            var filesPath = Directory.GetFiles(folderPath, "jobs_data_*.json", SearchOption.TopDirectoryOnly);
            return filesPath.OrderByDescending(f => f).FirstOrDefault();
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
