using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class SkillCommand : ICommand
    {
        private readonly ILogger<ApplyCommand> _logger;
        private readonly ISkillNormalizerService _skillNormalizerService;
        public SkillCommand(
            ILogger<ApplyCommand> logger,
            ISkillNormalizerService skillNormalizerService)
        {
            _logger = logger;
            _skillNormalizerService = skillNormalizerService;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
           await _skillNormalizerService.RunAsync();

        }
    }
}
