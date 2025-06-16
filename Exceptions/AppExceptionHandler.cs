using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Exceptions
{
    public class AppExceptionHandler
    {
        private readonly ILogger<AppExceptionHandler> _logger;

        public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
        {
            _logger = logger;
        }

        public void Handle(Exception ex, string context)
        {
            _logger.LogError(ex, $"Error in {context}");

            // Additional handling logic can be added here
            // e.g., sending notifications, fallback behavior, etc.
        }
    }
}
