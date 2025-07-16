using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Services;
using Services.Interfaces;

namespace Commands
{
    public class BookCommand(
        IBookCoordinator bookCoordinator,
        ILogger<BookCommand> logger) : ICommand
    {
        private readonly IBookCoordinator _bookCoordinator = bookCoordinator;
        private readonly ILogger<BookCommand> _logger = logger;

        public async Task ExecuteAsync(Dictionary<string, string>? arguments = null)
        {
            _logger.LogInformation("Starting book a road test...");
            await _bookCoordinator.BookAsyn();
        }

    }

}
