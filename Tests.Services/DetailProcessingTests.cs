namespace Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using global::Services;
    using global::Services.Interfaces;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using Moq;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;

    namespace Tests.Services
    {
        [TestClass]
        public class DetailProcessingTests
        {
            private ChromeDriver _driver;

            [TestInitialize]
            public void Setup()
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");

                _driver = new ChromeDriver(options);
            }

            [TestCleanup]
            public void Teardown()
            {
                _driver.Quit();
                _driver.Dispose();
            }

            [TestMethod]
            public async Task LoadHtmlFilesAndVerifyJobContainer()
            {
                // Locate output directory
                var outputDir = Path.GetDirectoryName(typeof(DetailProcessingTests).Assembly.Location)!;
                var detailDir = Path.Combine(outputDir, "Detail");

                Assert.IsTrue(Directory.Exists(detailDir), $"Detail directory not found: {detailDir}");

                var htmlFiles = Directory.GetFiles(detailDir, "*.html");
                Assert.IsTrue(htmlFiles.Length > 0, "No HTML files found in Detail directory.");

                foreach (var file in htmlFiles)
                {
                    var fileUri = new Uri(file).AbsoluteUri;
                    await Execute(_driver, fileUri);
                }
            }


            public static async Task Execute(IWebDriver driver,string url)
            {

                var mockLogger = new Mock<ILogger<DetailProcessing>>();
                var mockCapture = new Mock<ICaptureSnapshot>();
                var mockSecurity = new Mock<ISecurityCheck>();
                var mockDirCheck = new Mock<IDirectoryCheck>();
                var mockFactory = new Mock<IWebDriverFactory>();
                var mockChildElement = new Mock<IWebElement>();
                var childElements = new ReadOnlyCollection<IWebElement>(new List<IWebElement> { mockChildElement.Object });
                var mockParentElement = new Mock<IWebElement>();
                var mockNavigation = new Mock<INavigation>();
                var mockUtil =  new Mock<IUtil>();
                var mockLogin = new Mock<ILoginService>();
                var mockJobStorage = new Mock<IJobStorageService>();
                mockUtil.Setup(e => e.ExtractJobId(It.IsAny<string>())).Returns("12345");
                mockParentElement.Setup(e => e.Displayed).Returns(true);
                mockParentElement.Setup(e => e.FindElements(It.IsAny<By>())).Returns(childElements);
                var parentElements = new ReadOnlyCollection<IWebElement>(new List<IWebElement> { mockParentElement.Object });
                mockFactory.Setup(f => f.Create()).Returns(driver);
                mockCapture.Setup(c => c.CaptureArtifactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync("dummyTimestamp");
                mockSecurity.Setup(s => s.IsSecurityCheck()).Returns(false);
                var executionOptions = new ExecutionOptions();
                var detailProcessing = new DetailProcessing(
                    mockFactory.Object,
                    mockLogger.Object,
                    mockCapture.Object,
                    executionOptions,
                    mockLogin.Object,
                    mockJobStorage.Object
                );
                var offers = new List<string> { url };

                // Act
                var result = await detailProcessing.ProcessOffersAsync(offers, "search text");

                // Assert
                Assert.IsNotNull(result, "Result should not be null");
                Assert.AreEqual(1, result.Count, "Should have processed one job offer");

                mockCapture.Verify(c => c.CaptureArtifactsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
                mockLogger.Verify(l => l.Log(
                    It.IsAny<Microsoft.Extensions.Logging.LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                    Times.AtLeastOnce);
            }
        }
    }
}
