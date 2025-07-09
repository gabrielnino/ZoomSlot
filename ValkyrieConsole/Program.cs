using System;
using Commands;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models;
using Serilog;
using Services;
using Services.Interfaces;
using ValkyrieHire.Bootstrapper;


public class Program
{
    public static async Task Main(string[] args)
    {
        var executionOptions = new ExecutionOptions();
        Directory.CreateDirectory(executionOptions.ExecutionFolder);

        try
        {
            using var host = AppHostBuilder.Create(args, executionOptions).Build();
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

