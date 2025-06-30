using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class PromtCommand : ICommand
    {

        private readonly ILogger<PromtCommand> _logger;
        private readonly IJobStorageService _storageService;
        private readonly IPromptGenerator _promptGenerator;

        public PromtCommand(
            ILogger<PromtCommand> logger,
            IPromptGenerator promptGenerator)
        {
            _logger = logger;
            _promptGenerator = promptGenerator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting job application process...");
            await _promptGenerator.ExecuteChain();
        }

    }
}
