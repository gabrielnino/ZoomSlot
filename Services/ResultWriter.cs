using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    using Microsoft.Extensions.Logging;
    using Services.Interfaces;

    public class ResultWriter : IResultWriter
    {
        private readonly ILogger<ResultWriter> _logger;
        private readonly IFileService _fileService;

        public ResultWriter(ILogger<ResultWriter> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        public async Task WriteResultsAsync(
            Dictionary<string, List<string>> finalGroups,
            string normalizedOutputPath,
            string summaryOutputPath)
        {
            await _fileService.WriteJsonAsync(normalizedOutputPath, finalGroups);

            var summary = new StringBuilder();
            foreach (var (category, items) in finalGroups.OrderBy(g => g.Key))
            {
                summary.AppendLine($"[{category}] - {items.Count} skills");
                foreach (var item in items.OrderBy(x => x))
                    summary.AppendLine($"  - {item}");
                summary.AppendLine();
            }

            await _fileService.WriteTextAsync(summaryOutputPath, summary.ToString());

            _logger.LogInformation("📁 Saved normalized skills to: {OutputPath}", normalizedOutputPath);
            _logger.LogInformation("📄 Saved summary to: {SummaryPath}", summaryOutputPath);
        }
    }

}
