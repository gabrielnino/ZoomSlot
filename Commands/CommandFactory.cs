using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

namespace Commands
{
    public class CommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _debugMode;

        public CommandFactory(IServiceProvider serviceProvider, CommandArgs commandArgs)
        {
            _serviceProvider = serviceProvider;
            _debugMode = commandArgs.IsDebugMode;
        }

        public ICommand CreateCommand(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help"))
                return _serviceProvider.GetRequiredService<HelpCommand>();

            var command = args.FirstOrDefault(arg =>
                arg == "--search" || arg == "--export" || arg == "--debug") ?? "--help";

            return command switch
            {
                "--search" => _debugMode
                    ? new DebugCommandWrapper(
                        _serviceProvider.GetRequiredService<SearchCommand>(),
                        _serviceProvider.GetRequiredService<ILogger<DebugCommandWrapper>>())
                    : _serviceProvider.GetRequiredService<SearchCommand>(),

                "--export" => _debugMode
                    ? new DebugCommandWrapper(
                        _serviceProvider.GetRequiredService<ExportCommand>(),
                        _serviceProvider.GetRequiredService<ILogger<DebugCommandWrapper>>())
                    : _serviceProvider.GetRequiredService<ExportCommand>(),

                _ => throw new ArgumentException("Invalid command")
            };
        }
    }
}
