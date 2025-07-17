using Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Services.Interfaces;

namespace Services
{
    public class Booking : IBooking
    {
        private readonly IWebDriver _driver;
        private readonly ILogger<LoginService> _logger;
        private readonly ExecutionOptions _executionOptions;
        private const string FolderName = "Login";

        private string FolderPath => Path.Combine(_executionOptions.ExecutionFolder, FolderName);
        private readonly IDirectoryCheck _directoryCheck;

        public Booking(AppConfig config,
                       IWebDriverFactory driverFactory,
                       ILogger<LoginService> logger,
                       ExecutionOptions executionOptions,
                       IDirectoryCheck directoryCheck)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
        }

        public async Task Search()
        {
            _logger.LogInformation($"🔍 ID:{_executionOptions.TimeStamp} Starting search for booking appointments...");
            await Task.Delay(3000);
            await CloseSurvey();
            await FindAppointments();
            await SelectAppointmentTime("Monday, November 24th, 2025", "8:35 AM");
        }
        public async Task FindAppointments()
        {
            _logger.LogInformation("🧭 Navigating to location input and selecting from dropdown...");
            try
            {
                var searchBoxXpath = "//input[@placeholder='Start typing...']";
                var inputSearch = _driver.FindElement(By.XPath(searchBoxXpath));
                inputSearch.Clear();
                inputSearch.SendKeys("vancou");
                await Task.Delay(1000);
                inputSearch.SendKeys("v");
                await Task.Delay(500);
                var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                var dropdownOptionXpath = "//mat-option//span[contains(text(), 'Vancouver, BC')]";
                var dropdownItem = wait.Until(drv => drv.FindElement(By.XPath(dropdownOptionXpath)));
                dropdownItem.Click(); // Garantiza que el valor sea realmente seleccionado
                _logger.LogInformation("✅ Location selected from dropdown: Vancouver, BC");
                await Task.Delay(500); // Esperar a que se active el botón de búsqueda
                var searchButtonXpath = "//button[.//span[contains(text(),'Search')]]";
                var searchButton = _driver.FindElement(By.XPath(searchButtonXpath));
                searchButton.Click();
                await Task.Delay(1000);
                var officeContainerXpath = "//div[contains(@class,'first-office-container') and .//div[contains(@class,'department-title') and contains(., 'Vancouver claim centre')]]";
                var officeContainer = wait.Until(driver => driver.FindElement(By.XPath(officeContainerXpath)));
                _logger.LogInformation("🏢 Found office container for Vancouver claim centre (Kingsway)");
                officeContainer.Click();
                await Task.Delay(1000);
                _logger.LogInformation("🔍 Search button clicked successfully.");
            }
            catch (NoSuchElementException ex)
            {
                _logger.LogError(ex, "❌ Could not find one of the required elements (input, dropdown, or button).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error during FindAppointments.");
            }
        }


        public async Task SelectAppointmentTime(string desiredDate, string desiredTime)
        {
            _logger.LogInformation("⏳ Waiting for appointment time slots to appear...");

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            try
            {
                // Espera a que aparezca alguna fecha disponible
                var dateTitleXpath = $"//div[contains(@class, 'date-title') and contains(., '{desiredDate}')]";
                var dateElement = wait.Until(driver => driver.FindElement(By.XPath(dateTitleXpath)));

                _logger.LogInformation($"📆 Found date: {desiredDate}");

                // Buscar el botón de hora debajo de esa fecha
                //aqui todavía no funciona, pero se deja como referencia para el futuro

                _logger.LogInformation($"⏰ Selected time slot: {desiredTime}");
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogError("⏰ Timeout: No appointment times available.");
            }
            catch (NoSuchElementException)
            {
                _logger.LogError($"❌ Time slot {desiredTime} not found under date {desiredDate}.");
            }
        }

        public async Task CloseSurvey()
        {
            try
            {
                _logger.LogInformation("🔎 Checking for survey popup...");
                await Task.Delay(TimeSpan.FromSeconds(10));
                var dialog = _driver.FindElement(By.XPath("//div[@role='dialog' and contains(., 'Help us improve our appointment booking services')]"));
                if (dialog != null)
                {
                    _logger.LogInformation("📋 Survey popup found. Attempting to dismiss...");
                    var noThanksBtn = dialog.FindElement(By.XPath(".//button[normalize-space(text())='No thanks']"));
                    noThanksBtn.Click();
                    _logger.LogInformation("✅ Survey popup dismissed successfully.");
                }
            }
            catch (NoSuchElementException)
            {
                _logger.LogInformation("ℹ️ No survey popup displayed. Continuing normally.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Unexpected error while checking for survey popup.");
            }
        }
    }
}
