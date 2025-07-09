using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using Services.Interfaces;
using Configuration;

namespace Services
{
    public class JsonResumeStorageService : IResumeStorageService, IDisposable
    {
        public readonly string? _storageFile;
        public string StorageFile => _storageFile;
        private readonly ILogger<JsonJobStorageService> _logger;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly ExecutionOptions _executionOptions;

        public JsonResumeStorageService(ILogger<JsonJobStorageService> logger, ExecutionOptions executionOptions)
        {
            _logger = logger;
            _executionOptions = executionOptions;
            if (_storageFile == null)
            {
                _storageFile = "resume.json";
            }
            EnsureStorageDirectoryExists();
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

        public async Task SaveResumeAsync(string resumeFilePath, Resume resume)
        {
            try
            {
                var options = new JsonSerializerOptions() { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(resume, options);
                await File.WriteAllTextAsync(resumeFilePath, json);
                _logger.LogInformation("Resume saved successfully to {FilePath}", resumeFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save resume to file: {FilePath}", resumeFilePath);
            }
        }

        public async Task<string> LoadResumeAsync(string offersFilePath)
        {
            var json = await File.ReadAllTextAsync(offersFilePath);
            return json;
        }
    }
}
