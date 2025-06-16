using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public class DebugCommandWrapper : ICommand
    {
        private readonly ICommand _innerCommand;
        private readonly ILogger _logger;

        public DebugCommandWrapper(ICommand innerCommand, ILogger logger)
        {
            _innerCommand = innerCommand;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("🚀 Starting command in debug mode");
            try
            {
                await _innerCommand.ExecuteAsync();
                _logger.LogInformation("✅ Command completed successfully in debug mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Command failed in debug mode");
                throw;
            }
        }
    }
}
