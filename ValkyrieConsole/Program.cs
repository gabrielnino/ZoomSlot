using Commands;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("ValkyrieHire application starting");

            var commandFactory = host.Services.GetRequiredService<CommandFactory>();
            var command = commandFactory.CreateCommand(args);

            await command.ExecuteAsync();

            logger.LogInformation("ValkyrieHire application completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine("Use --help for usage information");
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config.AddEnvironmentVariables();
        })
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.ClearProviders();
            logging.AddConsole();

            var config = hostingContext.Configuration.Get<AppConfig>();

            // Add file logging with proper configuration
            logging.AddFile(config.Logging.LogFilePath, fileLoggerOpts =>
            {
                fileLoggerOpts.MinLevel = GetLogLevel(config.Logging.FileLogLevel);
                fileLoggerOpts.FileSizeLimitBytes = 5_000_000; // 5MB
                fileLoggerOpts.RetainedFileCountLimit = 3;
                fileLoggerOpts.EnsureDirectoryExists = true;
            });
        })
        .ConfigureServices((hostingContext, services) =>
        {
            // Configuration
            var config = hostingContext.Configuration.Get<AppConfig>();
            services.AddSingleton(config);

            // Register your services here
            // services.AddTransient<IMyService, MyService>();
        });


    private static LogLevel GetLogLevel(string level) => level.ToUpper() switch
    {
        "TRACE" => LogLevel.Trace,
        "DEBUG" => LogLevel.Debug,
        "INFORMATION" => LogLevel.Information,
        "WARNING" => LogLevel.Warning,
        "ERROR" => LogLevel.Error,
        "CRITICAL" => LogLevel.Critical,
        _ => LogLevel.Information
    };
}