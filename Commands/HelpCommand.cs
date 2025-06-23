using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public class 
        HelpCommand : ICommand
    {
        private readonly ILogger<HelpCommand> _logger; // Optional

        public HelpCommand(ILogger<HelpCommand> logger = null) // DI constructor
        {
            _logger = logger;
        }

        public Task ExecuteAsync(Dictionary<string, string>? Arguments)
        {
            _logger?.LogInformation("Displaying help information");
            Console.WriteLine("Available commands:");
            Console.WriteLine("--search\tSearch for jobs");
            Console.WriteLine("--export\tExport results");
            Console.WriteLine("--help\t\tShow this help");
            return Task.CompletedTask;
        }
    }
}
