using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Services
{
    public class ChromeDriverFactory : IWebDriverFactory, IDisposable
    {
        private readonly ILogger<ChromeDriverFactory> _logger;
        private ChromeDriverService _driverService;
        private IWebDriver? _sharedDriver;

        public ChromeDriverFactory(ILogger<ChromeDriverFactory> logger)
        {
            _logger = logger;
            _driverService = ChromeDriverService.CreateDefaultService();
            _driverService.HideCommandPromptWindow = true;
        }

        public IWebDriver Create()
        {
            return CreateDriver(GetDefaultOptions());
        }

        public IWebDriver Create(Action<ChromeOptions> configureOptions)
        {
            var options = GetDefaultOptions();
            configureOptions?.Invoke(options);
            return CreateDriver(options);
        }

        public ChromeOptions GetDefaultOptions()
        {
            var options = new ChromeOptions();

            options.AddArguments(
                "--headless",
                "--disable-gpu",
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--window-size=1920,1080",
                "--log-level=3"
            );

            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            return options;
        }

        private IWebDriver CreateDriver(ChromeOptions options)
        {
            try
            {
                if (_sharedDriver == null)
                {
                    _logger.LogInformation("Creating new ChromeDriver instance");
                    _sharedDriver = new ChromeDriver(_driverService, options);
                }
                return _sharedDriver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ChromeDriver");
                throw new WebDriverException("Failed to initialize ChromeDriver", ex);
            }
        }

        public void Dispose()
        {
            _driverService?.Dispose();
        }
    }
}
