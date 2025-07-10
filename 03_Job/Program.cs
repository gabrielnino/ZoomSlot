using Commands;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ValkyrieHire.Bootstrapper;


public class Program
{
    public static async Task Main(string[] args)
    {


        try
        {
            using var host = AppHostBuilder.Create(args).Build();
            var commandFactory = host.Services.GetRequiredService<CommandFactory>();
            var commands = commandFactory.CreateCommand().ToList();
            var jobArgs = host.Services.GetRequiredService<JobCommandArgs>();

            Log.Information($"Starting processing {commands.Count} commands");
            foreach (var command in commands)
            {
                try
                {
                    Log.Information("Executing command...");
                    await command.ExecuteAsync(jobArgs.Arguments);
                    Log.Information($"{command.GetType().Name} completed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Execution failed for {command.GetType().Name}");
                    throw new AggregateException($"Command {command.GetType().Name} failed", ex);
                }
            }

            Log.Information("All commands processed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Environment.ExitCode = 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
