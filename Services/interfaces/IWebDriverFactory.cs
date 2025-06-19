using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Services.interfaces
{
    public interface IWebDriverFactory
    {
        /// <summary>
        /// Creates a new WebDriver instance with default configuration
        /// </summary>
        IWebDriver Create();

        /// <summary>
        /// Creates a WebDriver with custom options
        /// </summary>
        IWebDriver Create(Action<ChromeOptions> configureOptions);

        /// <summary>
        /// Gets the default Chrome options used by the factory
        /// </summary>
        ChromeOptions GetDefaultOptions();
    }
}
