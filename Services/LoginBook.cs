using System;
using System.IO;
using System.Threading.Tasks;
using Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class LoginBook : ILoginBook
    {
        private readonly AppConfig _config;
        private readonly IWebDriver _driver;
        private readonly ILogger<LoginService> _logger;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Login";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;

        public LoginBook(AppConfig config,
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

        public async Task LoginAsync()
        {
            try
            {
                _logger.LogInformation($"🔐 ID:{_executionOptions.TimeStamp} Starting login process to ICBC Booking system...");

                var url = "https://onlinebusiness.icbc.com/webdeas-ui/login;type=driver";
                _logger.LogInformation("🌐 Navigating to: {Url}", url);
                _driver.Navigate().GoToUrl(url);
                await _capture.CaptureArtifactsAsync(FolderPath, $"01_GoToUrl");

                await Task.Delay(1000);
                _logger.LogInformation("👤 Entering driver's last name...");
                var lastName = _driver.FindElement(By.CssSelector("input[formcontrolname='drvrLastName']"));
                lastName.SendKeys(_config.BookCredentials.DirversLastname);

                await Task.Delay(1000);
                _logger.LogInformation("🪪 Entering driver’s licence number...");
                var licence = _driver.FindElement(By.CssSelector("input[formcontrolname='licenceNumber']"));
                licence.SendKeys(_config.BookCredentials.LicenceNumber);

                await Task.Delay(1000);
                _logger.LogInformation("🔑 Entering password...");
                var password = _driver.FindElement(By.CssSelector("input[formcontrolname='keyword']"));
                password.SendKeys(_config.BookCredentials.Password);

                await Task.Delay(1000);
                _logger.LogInformation("☑️ Accepting terms and conditions checkbox...");
                var agreeLabel = _driver.FindElement(By.CssSelector("label[for='mat-checkbox-1-input']"));
                agreeLabel.Click();

                await Task.Delay(1000);
                _logger.LogInformation("🚪 Clicking 'Sign in'...");
                var signIn = _driver.FindElement(By.XPath("//button[normalize-space()='Sign in']"));
                signIn.Click();

                await Task.Delay(2000);
                await _capture.CaptureArtifactsAsync(FolderPath, "02_AfterLogin");

                _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully logged in to ICBC Booking.");
            }
            catch (Exception ex)
            {
                _driver.Dispose();
                _driver.Quit();
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Login to ICBC Booking failed: {ex.Message}");
            }
        }
    }
}
