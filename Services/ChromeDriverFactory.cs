using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Services.Interfaces;

namespace Services
{
    public class ChromeDriverFactory : IWebDriverFactory, IDisposable
    {
        private readonly ILogger<ChromeDriverFactory> _logger;
        private ChromeDriverService _driverService;
        private IWebDriver? _driver;

        public ChromeDriverFactory(ILogger<ChromeDriverFactory> logger)
        {
            _logger = logger;
            _driverService = ChromeDriverService.CreateDefaultService();
            _driverService.HideCommandPromptWindow = true;
        }

        public IWebDriver Create()
        {
            if (_driver == null)
            {
                var options = GetDefaultOptions();
                _driver = new ChromeDriver(_driverService, options);
                _logger.LogInformation("Creating new ChromeDriver instance");
            }
            return _driver;
        }

        public IWebDriver Create(Action<ChromeOptions> configureOptions)
        {
            DisposeDriverIfExists();
            var options = GetDefaultOptions();
            configureOptions?.Invoke(options);
            return CreateDriver(options);
        }

        private IWebDriver CreateDriver(ChromeOptions options)
        {
            try
            {
                _logger.LogInformation("Creating new ChromeDriver instance");
                _driver = new ChromeDriver(_driverService, options);
                return _driver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ChromeDriver");
                throw new WebDriverException("Failed to initialize ChromeDriver", ex);
            }
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

        private void DisposeDriverIfExists()
        {
            if (_driver != null)
            {
                _logger.LogInformation("Disposing existing ChromeDriver instance");
                try { _driver.Quit(); } catch { /* ignore */ }
                _driver.Dispose();
                _driver = null;
            }
        }

        public void Dispose()
        {
            if (_driver != null)
            {
                _logger.LogInformation("Disposing ChromeDriver instance");
                _driver.Quit();
                _driver.Dispose();
                _driver = null;
            }
            _driverService?.Dispose();
        }
    }
}
