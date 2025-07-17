using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Services.Interfaces;

namespace Services
{
    public class Booking : IBooking
    {
        private readonly AppConfig _config;
        private readonly IWebDriver _driver;
        private readonly ILogger<LoginService> _logger;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Login";

        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;

        public Booking(AppConfig config,
                       IWebDriverFactory driverFactory,
                       ILogger<LoginService> logger,
                       ICaptureSnapshot capture,
                       ExecutionOptions executionOptions,
                       IDirectoryCheck directoryCheck)
        {
            _config = config;
            _driver = driverFactory.Create();
            _logger = logger;
            _capture = capture;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }

        public async Task Search()
        {
            _logger.LogInformation($"🔍 ID:{_executionOptions.TimeStamp} Starting search for booking appointments...");
            await Task.Delay(3000);
            await CloseSurvey();
            await FindAppointments();
        }

        public async Task FindAppointments()
        {
            var search = "//input[@placeholder='Start typing...']";

            var imputSearch = _driver.FindElement(By.XPath(search));
            imputSearch.SendKeys("Vancouver, BC" + Environment.NewLine);
            imputSearch.Click();


        
        }

        public async Task CloseSurvey()
        {
            try
            {
                _logger.LogInformation("🔎 Checking for survey popup...");
                await Task.Delay(TimeSpan.FromSeconds(10));
                var dialog = _driver.FindElement(By.XPath("//div[@role='dialog' and contains(., 'Help us improve our appointment booking services')]"));
                if (dialog != null)
                {
                    _logger.LogInformation("📋 Survey popup found. Attempting to dismiss...");
                    var noThanksBtn = dialog.FindElement(By.XPath(".//button[normalize-space(text())='No thanks']"));
                    noThanksBtn.Click();
                    _logger.LogInformation("✅ Survey popup dismissed successfully.");
                }
            }
            catch (NoSuchElementException)
            {
                _logger.LogInformation("ℹ️ No survey popup displayed. Continuing normally.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Unexpected error while checking for survey popup.");
            }
        }
    }
}
