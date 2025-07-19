using Commands;
using Console.Bootstrapper;
using Microsoft.Extensions.DependencyInjection;
using Serilog;


public class Program
{
    public static async Task Main(string[] args)
    {
        while (true)
        {
            try
            {
                Log.Information("🚗 Executing booking at {Time}", DateTimeOffset.Now);
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

                    Log.Information("✅ All commands processed successfully");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "❌ Application terminated unexpectedly");
                    Environment.ExitCode = 1;
                }
                finally
                {
                    await Log.CloseAndFlushAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Error while booking road test");
            }

            Log.Information("⏱ Waiting 15 minutes before the next booking attempt...");

            for (int i = 1; i <= 15; i++)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Log.Information("⏳ Minute {Minute}/15 elapsed", i);
            }
        }
    }
}