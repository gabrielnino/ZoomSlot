using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using Services.Interfaces;

namespace Services
{
    public class JsonJobStorageService : IJobStorageService, IDisposable
    {
        public readonly string? _storageFile;
        public string StorageFile => _storageFile;
        private readonly ILogger<JsonJobStorageService> _logger;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly ExecutionOptions _executionOptions;

        public JsonJobStorageService(ILogger<JsonJobStorageService> logger, ExecutionOptions executionOptions)
        {
            _logger = logger;
            _executionOptions = executionOptions;
            if (_storageFile == null)
            {
                _storageFile = "offers_detail.json";
            }
            EnsureStorageDirectoryExists();
        }

        public async Task SaveJobOfferDetailAsync(string offersDetailFilePath, IEnumerable<JobOfferDetail> offersDetail)
        {
            if (offersDetail == null) throw new ArgumentNullException(nameof(offersDetail));

            await _fileLock.WaitAsync();
            try
            {
                await WriteFile<JobOfferDetail>(offersDetailFilePath, offersDetail);
                _logger.LogInformation("✅ Saved {JobCount} job details to storage", offersDetail.Count());
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

        public async Task SaveJobOfferAsync(string offersDetailFilePath, IEnumerable<JobOffer> offers)
        {
            if (offers == null) throw new ArgumentNullException(nameof(offers));

            await _fileLock.WaitAsync();
            try
            {
                await WriteFile<JobOffer>(offersDetailFilePath, offers);
                _logger.LogInformation("✅ Saved {JobCount} job details to storage", offers.Count());
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

        private static async Task WriteFile<T>(string offersDetailFilePath, IEnumerable<T> offersDetail)
        {
            var json = JsonConvert.SerializeObject(offersDetail, Formatting.Indented);
            await File.WriteAllTextAsync(offersDetailFilePath, json);
        }

        public async Task<IEnumerable<JobOfferDetail>> LoadJobsDetailAsync(string offersFilePath)
        {
            try
            {

                if (offersFilePath == null || !File.Exists(offersFilePath))
                {
                    _logger.LogWarning("⚠️ Job data file not found in '{LastFolder}'", offersFilePath);
                    return Enumerable.Empty<JobOfferDetail>();
                }
                var json = await File.ReadAllTextAsync(offersFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("⚠️ Job data file '{FilePath}' is empty", offersFilePath);
                    return Enumerable.Empty<JobOfferDetail>();
                }
                var jobs = JsonConvert.DeserializeObject<List<JobOfferDetail>>(json) ?? new List<JobOfferDetail>();
                _logger.LogInformation("✅ Loaded {JobCount} job details from '{FilePath}'", jobs.Count, offersFilePath);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load job details");
                throw;
            }
        }

        public async Task<IEnumerable<JobOffer>> LoadJobsAsync(string offersFilePath)
        {
            try
            {

                if (offersFilePath == null || !File.Exists(offersFilePath))
                {
                    _logger.LogWarning("⚠️ Job data file not found in '{LastFolder}'", offersFilePath);
                    return Enumerable.Empty<JobOffer>();
                }
                var json = await File.ReadAllTextAsync(offersFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("⚠️ Job data file '{FilePath}' is empty", offersFilePath);
                    return Enumerable.Empty<JobOffer>();
                }
                var jobs = JsonConvert.DeserializeObject<List<JobOffer>>(json) ?? new List<JobOffer>();
                _logger.LogInformation("✅ Loaded {JobCount} job details from '{FilePath}'", jobs.Count, offersFilePath);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load job details");
                throw;
            }
        }

        public async Task<IEnumerable<string>>? LoadOffersAsync(string offersFilePath)
        {
            var json = await File.ReadAllTextAsync(offersFilePath);
            var offers = JsonConvert.DeserializeObject<List<string>>(json) ?? [];
            return offers;
        }

        public async Task SaveOffersAsync(string offersFilePath, IEnumerable<string> offersPending)
        {
            try
            {
                var options = new JsonSerializerOptions() { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(offersPending, options);
                await File.WriteAllTextAsync(offersFilePath, json);
                _logger.LogInformation("Updated offers.json with {Count} pending URLs", offersPending.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save pending offers to file");
            }
        }

        public void Dispose()
        {
            _fileLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private void EnsureStorageDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_storageFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }


    }
}
