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
        public static IHostBuilder Create(string[] args, ExecutionOptions executionOptions)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, config) =>
                {
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
                })
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
                    services.AddTransient<ApplyCommand>();
                    services.AddTransient<JobsCommand>();
                    services.AddTransient<PromtCommand>();
                    services.AddTransient<QualifiedCommand>();
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
                    services.AddSingleton<IDetailProcessing, DetailProcessing>();
                    services.AddSingleton<IJobDocumentCoordinator, JobDocumentCoordinator>();
                    services.AddSingleton<IQualifiedService, QualifiedService>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<ISkillExtractor, SkillExtractor>();
                    services.AddSingleton<ISkillGrouper, SkillGrouper>();
                    services.AddSingleton<ICategoryResolver, CategoryResolver>();
                    services.AddSingleton<IResultWriter, ResultWriter>();
                    services.AddSingleton<ISkillNormalizerService, SkillNormalizerService>();
                });
        }
    }
}
