using Commands;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models;
using Serilog;
using Services;
using Services.interfaces;

public class Program
{
    private static JobCommandArgs? commandArgs;
    public static async Task Main(string[] args)
    {
        commandArgs = new JobCommandArgs(args);
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
            var command = commandFactory.CreateCommand(args);

            await command.ExecuteAsync();

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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(static (hostingContext, services) =>
            {
                // Configuration
                var config = hostingContext.Configuration.Get<AppConfig>();
                services.AddSingleton(config);
               
                var execution = new ExecutionOptions();
                services.AddSingleton<ExecutionOptions>(execution);
                services.AddSingleton(commandArgs);
                services.AddTransient<HelpCommand>();
                services.AddTransient<SearchCommand>();
                services.AddSingleton<CommandFactory>();
                services.AddTransient<IJobSearchCoordinator, JobSearchCoordinator>();
                services.AddTransient<IDetailProcessing, Services.DetailProcessing>();
                services.AddTransient<ILoginService, LoginService>();
                services.AddTransient<ISecurityCheck, SecurityCheck>();
                services.AddTransient<ICaptureSnapshot, CaptureSnapshot>();
                services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
                services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
                services.AddSingleton<IJobSearch, JobSearch>();
                services.AddSingleton<IPageProcessor, PageProcessor>();
                services.AddSingleton<IDirectoryCheck, DirectoryCheck>();
                services.AddSingleton<CommandFactory>();
            });
    }
}
