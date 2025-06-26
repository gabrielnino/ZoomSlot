using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using Services.Interfaces;

namespace Services
{
    public class LoginService : ILoginService
    {
        private readonly AppConfig _config;
        private readonly IWebDriver _driver;
        private readonly ILogger<LoginService> _logger;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Login";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly ISecurityCheck _securityCheck;
        private readonly IDirectoryCheck _directoryCheck;
        public LoginService(
            AppConfig config, 
            IWebDriverFactory driverFactory, 
            ILogger<LoginService> logger, 
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ISecurityCheck securityCheck,
            IDirectoryCheck directoryCheck)
        {
            _config = config;
            _driver = driverFactory.Create();
            _logger = logger;
            _capture = capture;
            _executionOptions = executionOptions;
            _securityCheck = securityCheck;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }

        public async Task LoginAsync()
        {
            _logger.LogInformation($"🔐 ID:{_executionOptions.TimeStamp} Attempting to login to LinkedIn...");
            var url = "https://www.linkedin.com/login";
            _driver.Navigate().GoToUrl(url);
            await _capture.CaptureArtifactsAsync(FolderPath, $"Go to url{url}");
            await Task.Delay(3000);

            if (!IsOnLoginPage())
            {
                if (_securityCheck.IsSecurityCheck())
                {
                    await _securityCheck.HandleSecurityPage();
                    throw new InvalidOperationException(
                        "LinkedIn requires manual security verification. Please login manually in browser first.");
                }
                await _securityCheck.HandleUnexpectedPage();
                throw new InvalidOperationException(
                    $"Failed to load LinkedIn login page. Current URL: {_driver.Url}");
            }

            var emailInput = _driver.FindElement(By.Id("username"));
            emailInput.SendKeys(_config.LinkedInCredentials.Email);
            await Task.Delay(3000);
            await _capture.CaptureArtifactsAsync(FolderPath, "Entered email");
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            //await Task.Delay(3000);
            await _capture.CaptureArtifactsAsync(FolderPath, "Entered password");
            _logger.LogInformation($"✅ ID:{_executionOptions.TimeStamp} Successfully authenticated with LinkedIn");
        }

        private bool IsOnLoginPage()
        {
            var usernameElements = _driver.FindElements(By.Id("username"));
            var passwordElements = _driver.FindElements(By.Id("password"));
            var urlContainsLogin = _driver.Url.Contains("/login");
            return usernameElements.Any() && passwordElements.Any() && urlContainsLogin;
        }
    }
}
