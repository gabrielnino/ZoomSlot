using Microsoft.Extensions.DependencyInjection;
using Services;

namespace Commands
{
    public class CommandFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly JobCommandArgs _jobCommandArgs;

        public CommandFactory(IServiceProvider serviceProvider, JobCommandArgs jobCommandArgs)
        {
            _serviceProvider = serviceProvider;
            _jobCommandArgs = jobCommandArgs;
        }

        public IEnumerable<ICommand> CreateCommand()
        {
            var commands = new List<ICommand>();

            switch (_jobCommandArgs.MainCommand.ToLowerInvariant())
            {
                case "--search":
                    commands.Add(_serviceProvider.GetRequiredService<SearchCommand>());
                    break;
                case "--apply":
                    commands.Add(_serviceProvider.GetRequiredService<ApplyCommand>());
                    break;
                case "--job":
                    commands.Add(_serviceProvider.GetRequiredService<JobsCommand>());
                    break;
                default:
                    commands.Add(_serviceProvider.GetRequiredService<HelpCommand>());
                    break;
            }

            return commands;
        }
    }
}

