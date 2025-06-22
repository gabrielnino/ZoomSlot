using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Commands
{
    public class CommandFactory(IServiceProvider serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public IEnumerable<ICommand> CreateCommand(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help"))
                return [_serviceProvider.GetRequiredService<HelpCommand>()];

            var commands = args
                .Where(arg => arg == "--search" || arg == "--export" || arg == "--apply")
                .Distinct()
                .ToList();

            if (commands.Contains("--search") && commands[0] != "--search")
            {
                commands.Remove("--search");
                commands.Insert(0, "--search");
            }

            return [.. commands.Select<string, ICommand>(arg => arg switch
            {
                "--search" => _serviceProvider.GetRequiredService<SearchCommand>(),
                "--export" => _serviceProvider.GetRequiredService<ExportCommand>(),
                _ => throw new ArgumentException($"Invalid command argument: {arg}")
            })];
        }

    }
}
