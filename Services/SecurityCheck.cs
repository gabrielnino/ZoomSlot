using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class SecurityCheck : ISecurityCheck
    {
        private readonly ILogger<DetailProcessing> _logger;
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly List<Models.JobOfferDetail> _offersDetail;
        private readonly ICaptureSnapshot _capture;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "SecurityCheck";
        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;
        public SecurityCheck(IWebDriverFactory driverFactory,
            ILogger<DetailProcessing> logger,
            ICaptureSnapshot capture,
            ExecutionOptions executionOptions,
            IDirectoryCheck directoryCheck)
        {
            _offersDetail = new List<Models.JobOfferDetail>();
            _driver = driverFactory.Create();
            _logger = logger;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _capture = capture;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }

        public bool IsSecurityCheck()
        {
            try
            {
                var title = _driver.Title.Contains("Security Verification");
                if (title)
                {
                    _logger.LogWarning("⚠️ Title Security Verification detected on the page.");
                    return true;
                }
                var captcha = _driver.FindElements(By.Id("captcha-internal")).Any();
                if (captcha)
                {
                    _logger.LogWarning("⚠️ CAPTCHA image detected on the page.");
                    return true;
                }

                var text = _driver.FindElements(By.XPath("//h1[contains(text(), 'Let’s do a quick security check')]")).Any();

                if (text)
                {
                    _logger.LogWarning("⚠️ Text 'Let’s do a quick security check' detected on the page.");
                    return true;
                }

                // Detect common CAPTCHA indicators
                var captchaImages = _driver.FindElements(By.XPath("//img[contains(@src, 'captcha')]")).Any();
                if (captchaImages)
                {
                    _logger.LogWarning("⚠️ CAPTCHA image detected on the page.");
                    return true;
                }

                // Detect common texts indicating human check
                var bodyText = _driver.FindElement(By.TagName("body")).Text;
                var indicators = new[] { "are you a human", "please verify", "unusual activity", "security check", "confirm your identity" };

                if (indicators.Any(indicator => bodyText.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _logger.LogWarning("⚠️ Security check text detected on the page.");
                    return true;
                }

                // Optionally: detect if login form re-appeared
                var loginForm = _driver.FindElements(By.XPath("//input[@name='session_key']"));
                if (loginForm.Any())
                {
                    _logger.LogWarning("⚠️ Unexpected LinkedIn login form detected. Session might have expired.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error while checking for security verification.");
                return false; // Fail-safe: assume no security check if we can't verify
            }
        }

        public async Task TryStartPuzzle()
        {
            try
            {
                _logger.LogDebug($"🔎 ID:{_executionOptions.TimeStamp} Searching for 'Start Puzzle' button...");
                await _capture.CaptureArtifactsAsync(_executionOptions.ExecutionFolder, "Error in Detailed Job Offer");
                var startPuzzleButton = _wait.Until(driver =>
                {
                    var xpathText = "//button[contains(text(), 'Start Puzzle')]";
                    var button = driver.FindElements(By.XPath(xpathText))
                                                   .FirstOrDefault();
                    return (button != null && button.Displayed && button.Enabled) ? button : null;
                });

                await _capture.CaptureArtifactsAsync(FolderPath, "Error in Detailed Job Offer");

                if (startPuzzleButton == null)
                {
                    _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} 'Start Puzzle' button not found on security check page.");
                }

                if (!startPuzzleButton.Displayed || !startPuzzleButton.Enabled)
                {
                    _logger.LogWarning($"⚠️ ID:{_executionOptions.TimeStamp} 'Start Puzzle' button is not interactable.");
                }

                _logger.LogInformation($"🧩 ID:{_executionOptions.TimeStamp} Clicking 'Start Puzzle' button...");
                startPuzzleButton.Click();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ ID:{_executionOptions.TimeStamp} Failed to click 'Start Puzzle' button.");
            }
        }

        public async Task HandleSecurityPage()
        {
            var timestamp = await _capture.CaptureArtifactsAsync(FolderPath, "SecurityPageDetected");
            _logger.LogError($" ID:{_executionOptions.TimeStamp} Unexpected page layout detected.");
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║           SECURITY PAGE DETECTED          ║");
            Console.WriteLine("╠════════════════════════════════════════════╣");
            Console.WriteLine($"║ Current URL: {_driver.Url,-30} ║");
            Console.WriteLine("║                                            ║");
            Console.WriteLine($"║ HTML saved to: {timestamp}.html ║");
            Console.WriteLine($"║ Screenshot saved to: {timestamp}.png ║");
            Console.WriteLine("╚════════════════════════════════════════════╝\n");
        }

        public async Task HandleUnexpectedPage()
        {
            var timestamp = await _capture.CaptureArtifactsAsync(FolderPath, "UnexpectedPageDetected");
            _logger.LogError($" ID:{_executionOptions.TimeStamp} Unexpected page layout detected.");
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
