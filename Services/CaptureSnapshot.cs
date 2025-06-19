using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Services.interfaces;

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

        public async Task<string> CaptureArtifacts(string executionFolder, string stage)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logger.LogWarning($"⚠️ CaptureDebugArtifacts called with {timestamp}");
            if (string.IsNullOrWhiteSpace(stage))
            {
                stage = "UnknownStage";
            }

            var htmlfile = $"{timestamp}.html";
            var htmlPath = Path.Combine(executionFolder, htmlfile);
            var screenshotFile = $"{timestamp}.png";
            var screenshotPath = Path.Combine(executionFolder, screenshotFile);
            await File.WriteAllTextAsync(htmlPath, _driver.PageSource);
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            screenshot.SaveAsFile(screenshotPath);
            _logger.LogDebug($"📸 Debug capture for '{stage}':\nHTML: {htmlfile}\nScreenshot: {screenshotFile}");
            return timestamp;
        }
    }
}
