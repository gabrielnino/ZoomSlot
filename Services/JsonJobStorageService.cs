using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class JsonJobStorageService : IJobStorageService, IDisposable
    {
        private const string StorageFile = "jobs_data.json";
        private readonly ILogger<JsonJobStorageService> _logger;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public JsonJobStorageService(ILogger<JsonJobStorageService> logger)
        {
            _logger = logger;
            EnsureStorageDirectoryExists();
        }

        public async Task SaveJobsAsync(IEnumerable<string> jobs)
        {
            if (jobs == null)
            {
                _logger.LogWarning("Attempted to save null jobs collection");
                throw new ArgumentNullException(nameof(jobs));
            }

            await _fileLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(jobs, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });

                await File.WriteAllTextAsync(StorageFile, json);
                _logger.LogInformation("Successfully saved {JobCount} jobs to storage", jobs.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save jobs to storage");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<IEnumerable<string>> LoadJobsAsync()
        {
            if (!File.Exists(StorageFile))
            {
                _logger.LogInformation("No storage file found - returning empty collection");
                return Enumerable.Empty<string>();
            }

            await _fileLock.WaitAsync();
            try
            {
                var json = await File.ReadAllTextAsync(StorageFile);
                var jobs = JsonConvert.DeserializeObject<List<string>>(json)
                          ?? Enumerable.Empty<string>();

                _logger.LogInformation("Loaded {JobCount} jobs from storage", jobs.Count());
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load jobs from storage");
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
                    _logger.LogInformation("Job storage cleared successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear job storage");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private void EnsureStorageDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(StorageFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure storage directory exists");
                throw;
            }
        }

        public void Dispose()
        {
            _fileLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
