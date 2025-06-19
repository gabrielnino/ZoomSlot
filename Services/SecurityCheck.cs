using Microsoft.Extensions.Logging;
using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.interfaces;

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
        public bool IsSecurityChek()
        {
            var title = _driver.Title.Contains("Security Verification");
            var captcha = _driver.FindElements(By.Id("captcha-internal")).Any();
            var text = _driver.FindElements(By.XPath("//h1[contains(text(), 'Let’s do a quick security check')]")).Any();
            return title || captcha || text;
        }

        public async Task TryStartPuzzle()
        {
            try
            {
                _logger.LogDebug($"🔎 ID:{_executionOptions.TimeStamp} Searching for 'Start Puzzle' button...");
                await _capture.CaptureArtifacts(_executionOptions.ExecutionFolder, "Error in Detailed Job Offer");
                var startPuzzleButton = _wait.Until(driver =>
                {
                    var xpathText = "//button[contains(text(), 'Start Puzzle')]";
                    var button = driver.FindElements(By.XPath(xpathText))
                                                   .FirstOrDefault();
                    return (button != null && button.Displayed && button.Enabled) ? button : null;
                });

                await _capture.CaptureArtifacts(FolderPath, "Error in Detailed Job Offer");

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
            var timestamp = await _capture.CaptureArtifacts(FolderPath, "SecurityPageDetected");
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
            var timestamp = await _capture.CaptureArtifacts(FolderPath, "UnexpectedPageDetected");
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
