using System.Globalization;
using System.Text.RegularExpressions;
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
        private readonly IGmailCodeReader _gmailCodeReader;
        public Booking(AppConfig config,
                       IWebDriverFactory driverFactory,
                       ILogger<LoginService> logger,
                       ExecutionOptions executionOptions,
                       IDirectoryCheck directoryCheck,
                       IGmailCodeReader gmailCodeReader)
        {
            _driver = driverFactory.Create();
            _logger = logger;
            _executionOptions = executionOptions;
            _directoryCheck = directoryCheck;
            _directoryCheck.EnsureDirectoryExists(FolderPath);
            _gmailCodeReader = gmailCodeReader;
        }

        public async Task Search()
        {
            _logger.LogInformation($"🔍 ID:{_executionOptions.TimeStamp} Starting search for booking appointments...");
            if (IsRescheduleButtonPresent())
            {
                _logger.LogInformation("🔁 Reschedule button is present. Proceeding with rescheduling...");
                await CloseSurvey();
                await Reschedule();
                await ConfirmReschedule();
            }
            else
            {
                _logger.LogInformation("🔍 Reschedule button is not present. Proceeding with new booking...");
                await CloseSurvey();
            }
            await FindAppointments();
            await CloseSurvey();
            //"Monday, November 24th, 2025", "8:35 AM"
            var result = new DateTime(2025,11,24,8,35,0);
            await SelectEarlierClosestAppointmentAsync(result);
            await CloseSurvey();
            await SendVerificationCodeAsync();
            await CloseSurvey();
            await SetVerificationCodeAsync();
            await CloseSurvey();

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


        public async Task SelectEarlierClosestAppointmentAsync(DateTime currentAppointmentDateTime)
        {
            _logger.LogInformation("⏳ Searching for closest earlier appointment than current one: {Current}", currentAppointmentDateTime);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            try
            {
                var listings = wait.Until(driver => driver.FindElements(By.XPath("//div[contains(@class,'appointment-listings')]")));

                DateTime? bestCandidate = null;
                IWebElement? bestButton = null;

                foreach (var listing in listings)
                {
                    var dateElement = listing.FindElement(By.XPath(".//div[contains(@class,'date-title')]"));
                    if (!DateTime.TryParse(dateElement.Text.Trim(), out var parsedDate))
                    {
                        continue;
                    }
                    var buttons = listing.FindElements(By.XPath(".//button[contains(@class,'mat-button-toggle-button')]"));
                    foreach (var button in buttons)
                    {
                        var timeText = button.Text.Trim();
                        if (!DateTime.TryParse(timeText, out var parsedTime))
                        {
                            continue;
                        }
                        var fullDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, parsedTime.Hour, parsedTime.Minute, 0);
                        if (fullDateTime >= currentAppointmentDateTime)
                        {
                            continue; // Only interested in earlier times
                        }
                        if (bestCandidate == null || fullDateTime > bestCandidate)
                        {
                            bestCandidate = fullDateTime;
                            bestButton = button;
                        }
                    }
                }

                if (bestButton == null)
                {
                    _logger.LogInformation("🛑 No earlier appointment found.");
                    return;
                }

                _logger.LogInformation("✅ Found better (earlier) appointment: {NewTime}", bestCandidate.Value);

                bestButton.Click();
                var reviewButtonXpath = "//button[.//span[normalize-space(text())='Review Appointment']]";
                var reviewButton = wait.Until(driver =>  driver.FindElement(By.XPath(reviewButtonXpath)));
                reviewButton.Click();
                _logger.LogInformation("📥 Clicked 'Review Appointment' button.");
                await Task.Delay(1000);
                 var xpathMatDialog = "//mat-dialog-container[contains(@class,'mat-dialog-container')]";
                wait.Until(driver =>  driver.FindElement(By.XPath(xpathMatDialog)));
                _logger.LogInformation("💬 Dialog appeared for booking review.");
                var xpathDialogContainer = "//mat-dialog-container//button[contains(@class, 'primary') and contains(., 'Next')]";
                var nextButton = wait.Until(driver =>  driver.FindElement(By.XPath(xpathDialogContainer)));
                nextButton.Click();
                _logger.LogInformation("✅ Clicked 'Next' to confirm new appointment.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while selecting earlier appointment.");
            }
        }



        public async Task SendVerificationCodeAsync(string method = "Email")
        {
            _logger.LogInformation("🔐 Waiting for verification code dialog...");

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            try
            {
                // Esperar a que aparezca el diálogo del OTP
                var dialogXpath = "//mat-dialog-container[contains(@class,'mat-dialog-container')]//app-onetime-password";
                wait.Until(driver => driver.FindElement(By.XPath(dialogXpath)));
                _logger.LogInformation("📬 Verification code dialog appeared.");
                // Seleccionar el método deseado: "Email" o "SMS"
                string radioLabelXpath = method.ToLower() switch
                {
                    "sms" => "//label[@for='mat-radio-9-input']", _ => "//label[@for='mat-radio-8-input']"
                };
                var radioLabel = wait.Until(driver => driver.FindElement(By.XPath(radioLabelXpath)));
                radioLabel.Click();
                _logger.LogInformation($"📨 Selected {method} as delivery method for verification code.");
                // Hacer clic en el botón "Send"
                var sendButtonXpath = "//button[contains(@class,'primary') and normalize-space(text())='Send']";
                var sendButton = wait.Until(driver => driver.FindElement(By.XPath(sendButtonXpath)));
                sendButton.Click();

                _logger.LogInformation("✅ Verification code sent successfully.");
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogError("⏰ Timeout: Verification code dialog or buttons not found.");
            }
            catch (NoSuchElementException ex)
            {
                _logger.LogError(ex, $"❌ Failed to locate element for method '{method}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error during verification code step.");
            }

            // Esperar opcional para cargar el siguiente paso
            await Task.Delay(2000);
        }


        public async Task SetVerificationCodeAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            string? code = await _gmailCodeReader.GetVerificationCodeAsync();
            if (!string.IsNullOrEmpty(code))
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                // Step 1: Wait for input and enter code
                var input = wait.Until(driver => driver.FindElement(By.XPath("//input[@maxlength='6']")));
                input.Clear();
                input.SendKeys(code);
                _logger.LogInformation("✅ Verification code entered.");
                // Step 2: Wait for and click the submit button
                var submitButton = wait.Until(driver => driver.FindElement(By.XPath("//button[contains(@class, 'submit-code-button')]")));
                submitButton.Click();
                _logger.LogInformation("📤 Submit button clicked to finalize booking.");
            }
            else
            {
                _logger.LogWarning("❌ No verification code retrieved from Gmail.");
            }

            await Task.Delay(1000); // Optional: small delay to allow navigation to next screen
        }

        public async Task CloseSurvey()
        {
            try
            {
                _logger.LogInformation("🔎 Checking for survey popup...");
                //await Task.Delay(TimeSpan.FromSeconds(10));
                var dialogs = _driver.FindElements(By.XPath("//div[@role='dialog' and contains(., 'Help us improve our appointment booking services')]"));
                if (dialogs != null && dialogs.Any())
                {
                    _logger.LogInformation("📋 Survey popup found. Attempting to dismiss...");
                    var noThanksBtn = dialogs[0].FindElement(By.XPath(".//button[normalize-space(text())='No thanks']"));
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

        public bool IsRescheduleButtonPresent()
        {
            try
            {
                var buttons = _driver.FindElements(By.XPath("//button[contains(text(),'Reschedule appointment')]"));
                bool exists = buttons.Count > 0;
                _logger.LogInformation(exists ? "🔁 Reschedule button is present." : "❌ Reschedule button is not present.");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error checking for 'Reschedule appointment' button.");
                return false;
            }
        }


        public async Task Reschedule()
        {
            try
            {
                await Task.Delay(3000);
                var buttons = _driver.FindElements(By.XPath("//button[contains(text(),'Reschedule appointment')]"));
                if (buttons.Any())
                {
                    _logger.LogInformation("🔁 Reschedule button found. Clicking to proceed...");
                    buttons[0].Click();
                    _logger.LogInformation("✅ Reschedule button clicked successfully.");
                }
                else
                {
                    _logger.LogWarning("❌ Reschedule button not found. Cannot proceed with rescheduling.");
                    return;
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

        public async Task ConfirmReschedule()
        {
            try
            {
                _logger.LogInformation("🔍 Waiting for reschedule confirmation dialog...");
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                // Wait for the "Yes" button to appear inside the dialog
                var buttonYesXpath = "//mat-dialog-container//button[normalize-space(text())='Yes']";
                var yesButton = wait.Until(driver => driver.FindElement(By.XPath(buttonYesXpath)));
                yesButton.Click();
                _logger.LogInformation("✅ Clicked 'Yes' to confirm reschedule.");
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogWarning("⚠️ Timeout: 'Yes' button in reschedule dialog did not appear.");
            }
            catch (NoSuchElementException)
            {
                _logger.LogWarning("❌ 'Yes' button in reschedule dialog not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error while clicking 'Yes' in reschedule dialog.");
            }
            await Task.Delay(500); // Optional: slight delay after click
        }
    }
}
