using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;

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
        public LoginService(
            AppConfig config, 
            IWebDriverFactory driverFactory, 
            ILogger<LoginService> logger, 
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            ISecurityCheck securityCheck)
        {
            _config = config;
            _driver = driverFactory.Create();
            _logger = logger;
            _capture = capture;
            _executionOptions = executionOptions;
            _securityCheck = securityCheck;
        }

        public async Task LoginAsync()
        {
            _logger.LogInformation("🔐 Attempting to login to LinkedIn...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            await Task.Delay(3000);

            if (!IsOnLoginPage())
            {
                if (_securityCheck.IsSecurityChek())
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
            await _capture.CaptureArtifacts(FolderPath, "Entered email");
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(FolderPath, "Entered password");
            _logger.LogInformation("✅ Successfully authenticated with LinkedIn");
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
