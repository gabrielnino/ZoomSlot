using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Services.Interfaces
{
    public interface IWebDriverFactory
    {
        IWebDriver Create();
        IWebDriver Create(Action<ChromeOptions> configureOptions);
        ChromeOptions GetDefaultOptions();
    }
}
