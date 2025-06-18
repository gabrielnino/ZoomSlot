using Configuration;
using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;

namespace Services
{
    public class LinkedInLoginService : ILoginService
    {
        private readonly AppConfig _config;
        private readonly IWebDriver _driver;
        private readonly ILogger<LinkedInLoginService> _logger;
        private readonly ICaptureService _capture;
        private readonly bool _debugMode;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Login";
        private static string Timestamp => DateTime.Now.ToString("yyyyMMdd_HHmmss");
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        public LinkedInLoginService(
            AppConfig config, 
            IWebDriverFactory driverFactory, 
            ILogger<LinkedInLoginService> logger, 
            ICaptureService capture,
            ExecutionOptions executionOptions)
        {
            _config = config;
            _driver = driverFactory.Create();
            _logger = logger;
            _capture = capture;
            _executionOptions = executionOptions;
        }

        public async Task LoginAsync()
        {
            _logger.LogInformation("🔐 Attempting to login to LinkedIn...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            await Task.Delay(3000);

            if (!IsOnLoginPage())
            {
                if (IsSecurityCheckPresent())
                {
                    var timestamp = await _capture.CaptureArtifacts(FolderPath, "UnexpectedPageDetected");
                    _logger.LogError($"Unexpected page layout detected.");
                    Console.WriteLine("\n╔════════════════════════════════════════════╗");
                    Console.WriteLine("║           SECURITY PAGE DETECTED          ║");
                    Console.WriteLine("╠════════════════════════════════════════════╣");
                    Console.WriteLine($"║ Current URL: {_driver.Url,-30} ║");
                    Console.WriteLine("║                                            ║");
                    Console.WriteLine($"║ HTML saved to: {timestamp}.html ║");
                    Console.WriteLine($"║ Screenshot saved to: {timestamp}.png ║");
                    Console.WriteLine("╚════════════════════════════════════════════╝\n");
                    throw new InvalidOperationException(
                        "LinkedIn requires manual security verification. Please login manually in browser first.");
                }
                await HandleUnexpectedPage();
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
            return usernameElements.Count > 0 && passwordElements.Count > 0 && urlContainsLogin;
        }

        private bool IsSecurityCheckPresent()
        {
            var securityCheckHeader = _driver.FindElements(By.XPath("//h1[contains(text(), 'Let's do a quick security check')]"));
            var startPuzzleButton = _driver.FindElements(By.XPath("//button[contains(text(), 'Start Puzzle')]"));
            return securityCheckHeader.Any() || startPuzzleButton.Any();
        }

        private async Task HandleUnexpectedPage()
        {
            var timestamp = await _capture.CaptureArtifacts(FolderPath, "UnexpectedPageDetected");
            _logger.LogError($"Unexpected page layout detected.");
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║           UNEXPECTED PAGE DETECTED          ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine($"║ Current URL: {_driver.Url,-30} ║");
            Console.WriteLine("║                                            ║");
            Console.WriteLine($"║ HTML saved to: {timestamp}.html ║");
            Console.WriteLine($"║ Screenshot saved to: {timestamp}.png ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");
        }
    }
}
