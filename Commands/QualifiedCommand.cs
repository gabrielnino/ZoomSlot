using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Commands
{
    public class QualifiedCommand : ICommand
    {
        private readonly ILogger<ApplyCommand> _logger;
        private readonly IQualifiedService _qualifiedService;
        private readonly IJobDocumentCoordinator _jobDocumentCoordinator;

        public QualifiedCommand(
            ILogger<ApplyCommand> logger,
            IJobSearchCoordinator linkedInService,
            IQualifiedService qualifiedService,
            IJobDocumentCoordinator jobDocumentCoordinator)
        {
            _logger = logger;
            _qualifiedService = qualifiedService;
            _jobDocumentCoordinator = jobDocumentCoordinator;
        }

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "Arguments dictionary cannot be null.");
            }  

            if (!arguments.TryGetValue("offersFilePath", out var offersFilePath))
            {
                throw new ArgumentException("Missing required argument: offersFilePath", nameof(arguments));
            }
             
            if (!arguments.TryGetValue("resumeFilePath", out var resumeFilePath))
            {
                throw new ArgumentException("Missing required argument: resumeFilePath", nameof(arguments));
            }

            var offers = await _jobDocumentCoordinator.GenerateJobsDocumentAsync();
            _logger.LogInformation("✅ Qualified resume generated successfully.");
            await _qualifiedService.QualifiedAsync(offersFilePath, resumeFilePath);
            _logger.LogInformation("✅ Qualified resume finished successfully.");
        }

    }
}
