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


public class Program
{
    public static async Task Main(string[] args)
    {
        const int MAX_RETRIES = 3;
        const int BASE_DELAY_MS = 2000;
        var executionOptions = new ExecutionOptions();
        // Initialize directories
        Directory.CreateDirectory(executionOptions.ExecutionFolder);
        var logPath = Path.Combine(executionOptions.ExecutionFolder, "Logs");
        Directory.CreateDirectory(logPath);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logPath, "valkyriehire-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5_000_000,
                retainedFileCountLimit: 3,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();
        int intented = Random.Shared.Next(1, 90);
        int series = 0;
        do
        {

            try
            {
                using var host = CreateHostBuilder(args, executionOptions).Build();
                var commandFactory = host.Services.GetRequiredService<CommandFactory>();
                var commands = commandFactory.CreateCommand().ToList();
                var jobArgs = host.Services.GetRequiredService<JobCommandArgs>();
                Log.Information($"Starting processing {commands.Count} commands");
                foreach (var command in commands)
                {
                    int attempt = 0;
                    bool succeeded = false;
                    while (attempt <= MAX_RETRIES && !succeeded)
                    {
                        attempt++;
                        try
                        {
                            Log.Information($"Executing {command.GetType().Name} (Attempt {attempt}/{MAX_RETRIES})");
                            await command.ExecuteAsync(jobArgs.Arguments);
                            succeeded = true;
                            Log.Information($"{command.GetType().Name} completed successfully");
                        }
                        catch (Exception ex) when (attempt < MAX_RETRIES)
                        {
                            double begining = Math.Pow(2, attempt - 1);
                            int end = Random.Shared.Next(6_000, 900_000);
                            var delay = (int)(BASE_DELAY_MS * begining + end);
                            Log.Warning(ex, $"Attempt {attempt} failed. Retrying in {delay}ms");
                            await Task.Delay(delay);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Final attempt {attempt} failed for {command.GetType().Name}");
                            throw new AggregateException($"Failed to execute {command.GetType().Name} after {MAX_RETRIES} attempts", ex);
                        }
                    }
                }
                Log.Information("All commands processed successfully"); ;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                Environment.ExitCode = 1; // Signal error to calling process
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
            series++;
        } while (series < intented);
    }

    private static IHostBuilder CreateHostBuilder(string[] args, ExecutionOptions executionOptions) =>
       Host.CreateDefaultBuilder(args)
           .UseSerilog()
           .ConfigureAppConfiguration((hostingContext, config) =>
           {
               config.SetBasePath(Directory.GetCurrentDirectory());
               config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
               config.AddEnvironmentVariables();
           })
           .ConfigureServices((hostingContext, services) =>
           {
               var config = hostingContext.Configuration.Get<AppConfig>();
               services.AddSingleton(config);
               services.AddSingleton(executionOptions);
               services.AddSingleton(new JobCommandArgs(args));
               services.AddSingleton<CommandFactory>();
               services.AddTransient<HelpCommand>();
               services.AddTransient<SearchCommand>();
               services.AddTransient<ExportCommand>();
               services.AddTransient<ApplyCommand>();
               services.AddTransient<IJobSearchCoordinator, JobSearchCoordinator>();
               services.AddTransient<IDetailProcessing, DetailProcessing>();
               services.AddTransient<ILoginService, LoginService>();
               services.AddTransient<ISecurityCheck, SecurityCheck>();
               services.AddTransient<ICaptureSnapshot, CaptureSnapshot>();
               services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
               services.AddTransient<IJobSearch, JobSearch>();
               services.AddTransient<IPageProcessor, PageProcessor>();
               services.AddSingleton<IDirectoryCheck, DirectoryCheck>();
               services.AddSingleton<IJobStorageService, JsonJobStorageService>();
               services.AddSingleton<IDocumentParse, DocumentParse>();
               services.AddSingleton<IGenerator, Generator>();
               services.AddSingleton<IDocumentCoordinator, DocumentCoordinator>();
               services.AddSingleton<IOpenAIClient, OpenAIClient>();
               services.AddSingleton<IDocumentPDF, DocumentPDF>();
               services.AddSingleton<IUtil, Util>();
               services.AddSingleton<IPageTrackingService, PageTrackingService>();
           });
}

