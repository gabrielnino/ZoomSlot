using Commands;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Services;

public class Program
{
    private static CommandArgs? commandArgs;
    public static async Task Main(string[] args)
    {
        commandArgs = new CommandArgs(args);
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
                // Configuration
                var config = hostingContext.Configuration.Get<AppConfig>();
                services.AddSingleton(config);

                services.AddSingleton(commandArgs);
                // Register commands
                services.AddTransient<HelpCommand>();
                services.AddTransient<SearchCommand>();
                //services.AddTransient<ExportCommand>();

                // Register factory as singleton
                services.AddSingleton<CommandFactory>();

                // Register other services
                services.AddTransient<ILinkedInService, LinkedInService>();
                services.AddTransient <ILoginService, LinkedInLoginService>();
                services.AddTransient<ICaptureService, CaptureService>();
                services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
                services.AddSingleton<CommandFactory>();
            });
}
