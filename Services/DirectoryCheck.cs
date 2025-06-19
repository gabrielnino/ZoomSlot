using Microsoft.Extensions.Logging;
using Models;

namespace Services
{
    public class DirectoryCheck : IDirectoryCheck
    {
        private readonly ILogger<JobSearch> _logger;
        private readonly ExecutionOptions _executionOptions;
        public DirectoryCheck(ILogger<JobSearch> logger, ExecutionOptions executionOptions)
        {
            _logger = logger;
            _executionOptions = executionOptions;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation($"📁 Created execution folder at: {_executionOptions.ExecutionFolder}");
            }
        }
    }
}
