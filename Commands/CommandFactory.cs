using Microsoft.Extensions.DependencyInjection;

namespace Commands
{
    public class CommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICommand CreateCommand(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help"))
                return _serviceProvider.GetRequiredService<HelpCommand>();

            var command = args.FirstOrDefault(arg =>
                arg == "--search" || arg == "--export") ?? "--help";

            return command switch
            {
                "--search" => _serviceProvider.GetRequiredService<SearchCommand>(),

                "--export" => _serviceProvider.GetRequiredService<ExportCommand>(),

                _ => throw new ArgumentException("Invalid command")
            };
        }
    }
}
