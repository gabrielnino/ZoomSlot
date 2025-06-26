using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Services.Interfaces;

namespace Services
{
    public class CaptureSnapshot : ICaptureSnapshot
    {
        private readonly IWebDriver _driver;
        private readonly ILogger<CaptureSnapshot> _logger;

        public CaptureSnapshot(IWebDriverFactory driverFactory, ILogger<CaptureSnapshot> logger)
        {
            _driver = driverFactory.Create();
            _logger = logger;
        }

        public async Task<string> CaptureArtifactsAsync(string executionFolder, string stage)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            if (string.IsNullOrWhiteSpace(stage))
                stage = "UnknownStage";

            _logger.LogInformation("Capturing artifacts for stage: {Stage} at {Timestamp}", stage, timestamp);
            var htmlPath = Path.Combine(executionFolder, $"{timestamp}.html");
            var screenshotPath = Path.Combine(executionFolder, $"{timestamp}.png");
            Directory.CreateDirectory(executionFolder);
            await File.WriteAllTextAsync(htmlPath, _driver.PageSource);
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            screenshot.SaveAsFile(screenshotPath);
            _logger.LogInformation("Artifacts captured: {HtmlPath}, {ScreenshotPath}", htmlPath, screenshotPath);
            return timestamp;
        }
    }
}
