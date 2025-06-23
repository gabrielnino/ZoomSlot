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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: "Logs/valkyriehire-.log",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5_000_000,
                retainedFileCountLimit: 3,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();

        try
        {
            Log.Information("ValkyrieHire application starting");

            var host = CreateHostBuilder(args).Build();

            var commandFactory = host.Services.GetRequiredService<CommandFactory>();
            var commands = commandFactory.CreateCommand();

            foreach (var command in commands)
            {
                var jobArgs = host.Services.GetRequiredService<JobCommandArgs>();
                await command.ExecuteAsync(jobArgs.Arguments);
            }

            Log.Information("ValkyrieHire application completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error occurred");
            Console.WriteLine("Use --help for usage information");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
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
                services.AddSingleton(new ExecutionOptions());
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
                services.AddSingleton<IDocumentCoordinator, DocumentCoordinator>();
                services.AddSingleton<IUtil, Util>();
            });
}
