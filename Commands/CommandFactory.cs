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
                case JobCommandArgs.search:
                    commands.Add(_serviceProvider.GetRequiredService<SearchCommand>());
                    break;
                case JobCommandArgs.detail:
                    commands.Add(_serviceProvider.GetRequiredService<DetailCommand>());
                    break;
                case JobCommandArgs.apply:
                    commands.Add(_serviceProvider.GetRequiredService<ApplyCommand>());
                    break;
                case JobCommandArgs.job:
                    commands.Add(_serviceProvider.GetRequiredService<JobsCommand>());
                    break;
                case JobCommandArgs.resume:
                    commands.Add(_serviceProvider.GetRequiredService<ResumeCommand>());
                    break;
                case JobCommandArgs.prompt:
                    commands.Add(_serviceProvider.GetRequiredService<PromtCommand>());
                    break;
                case JobCommandArgs.qualified:
                    commands.Add(_serviceProvider.GetRequiredService<QualifiedCommand>());
                    break;

                case JobCommandArgs.skill:
                    commands.Add(_serviceProvider.GetRequiredService<SkillCommand>());
                    break;
                default:
                    commands.Add(_serviceProvider.GetRequiredService<HelpCommand>());
                    break;
            }

            return commands;
        }
    }
}

