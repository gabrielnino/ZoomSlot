using Configuration;
using Microsoft.Extensions.Logging;
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

        public LinkedInLoginService(
            AppConfig config, 
            IWebDriverFactory driverFactory, 
            ILogger<LinkedInLoginService> logger, 
            ICaptureService capture)
        {
            _config = config;
            _driver = driverFactory.Create();
            _logger = logger;
            _capture = capture;
        }

        public async Task LoginAsync(string folderName, string timestamp)
        {
            _logger.LogInformation("🔐 Attempting to login to LinkedIn...");
            _driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            await Task.Delay(3000);

            if (!IsOnLoginPage())
            {
                if (IsSecurityCheckPresent())
                {
                    throw new InvalidOperationException(
                        "LinkedIn requires manual security verification. Please login manually in browser first.");
                }

                if (_debugMode)
                {
                    await HandleUnexpectedPage(folderName, timestamp);
                }

                throw new InvalidOperationException(
                    $"Failed to load LinkedIn login page. Current URL: {_driver.Url}");
            }

            var emailInput = _driver.FindElement(By.Id("username"));
            emailInput.SendKeys(_config.LinkedInCredentials.Email);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(folderName, "Entered email");
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(_config.LinkedInCredentials.Password + Keys.Enter);
            await Task.Delay(3000);
            await _capture.CaptureArtifacts(folderName, "Entered password");
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
            return securityCheckHeader.Count > 0 || startPuzzleButton.Count > 0;
        }

        private async Task HandleUnexpectedPage(string folderName, string timestamp)
        {
            await _capture.CaptureArtifacts(folderName, "UnexpectedPageDetected");
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
