using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Services
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
