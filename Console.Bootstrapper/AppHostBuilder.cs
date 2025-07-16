using Commands;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models;
using Serilog;
using Services;
using Services.Interfaces;

namespace ValkyrieHire.Bootstrapper
{
    public static class AppHostBuilder
    {
        public static IHostBuilder Create(string[] args)
        {
            AppConfig appConfig = new();
            ExecutionOptions executionOptions = new(Environment.CurrentDirectory);

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    // Paso 2: Cargar configuración desde appsettings.json
                    hostingContext.Configuration.Bind(appConfig);

                    // Paso 3: Crear ExecutionOptions usando ruta desde configuración
                    executionOptions = new ExecutionOptions(appConfig.Paths.OutPath);
                    Directory.CreateDirectory(executionOptions.ExecutionFolder);

                    // Paso 4: Registrar configuración y opciones en DI
                    services.AddSingleton(appConfig);
                    services.AddSingleton(executionOptions);
                    services.AddSingleton(new JobCommandArgs(args));

                    // Paso 5: Registrar todos los servicios
                    services.AddSingleton<CommandFactory>();
                    services.AddTransient<HelpCommand>();
                    services.AddTransient<SearchCommand>();
                    services.AddTransient<DetailCommand>();
                    services.AddTransient<ApplyCommand>();
                    services.AddTransient<JobsCommand>();
                    services.AddTransient<PromtCommand>();
                    services.AddTransient<SkillCommand>();
                    services.AddTransient<IJobSearchCoordinator, JobSearchCoordinator>();
                    services.AddTransient<IPromptGenerator, PromptGenerator>();
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
                    services.AddSingleton<IJobDocumentCoordinator, JobDocumentCoordinator>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<ICategoryResolver, CategoryResolver>();
                    services.AddSingleton<IResultWriter, ResultWriter>();
                    services.AddSingleton<ISkillNormalizerService, SkillNormalizerService>();
                })
                .UseSerilog((context, services, config) =>
                {
                    // Paso 6: Crear carpeta de logs y configurar Serilog
                    var logPath = Path.Combine(executionOptions.ExecutionFolder, "Logs");
                    Directory.CreateDirectory(logPath);

                    config.MinimumLevel.Debug()
                          .WriteTo.Console()
                          .WriteTo.File(
                              path: Path.Combine(logPath, "valkyriehire-.log"),
                              rollingInterval: RollingInterval.Day,
                              fileSizeLimitBytes: 5_000_000,
                              retainedFileCountLimit: 3,
                              rollOnFileSizeLimit: true
                          );
                });
        }
    }
}
