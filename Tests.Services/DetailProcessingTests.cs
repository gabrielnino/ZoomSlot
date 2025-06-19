namespace Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using global::Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using Moq;
    using OpenQA.Selenium;
    using Services;

    namespace Tests.Services
    {
        [TestClass]
        public class DetailProcessingTests
        {
            [TestMethod]
            public async Task ProcessOffersAsync_ShouldProcessOffersAndReturnDetails()
            {
                // Arrange
                var mockDriver = new Mock<IWebDriver>();
                var mockLogger = new Mock<ILogger<DetailProcessing>>();
                var mockCapture = new Mock<ICaptureSnapshot>();
                var mockSecurity = new Mock<ISecurityCheck>();
                var mockDirCheck = new Mock<IDirectoryCheck>();
                var mockFactory = new Mock<IWebDriverFactory>();

                // Mock child elements returned by FindElements
                var mockChildElement = new Mock<IWebElement>();
                var childElements = new ReadOnlyCollection<IWebElement>(new List<IWebElement> { mockChildElement.Object });

                var mockParentElement = new Mock<IWebElement>();
                var mockNavigation = new Mock<INavigation>();
                mockParentElement.Setup(e => e.Displayed).Returns(true);
                mockParentElement.Setup(e => e.FindElements(It.IsAny<By>())).Returns(childElements);

                var parentElements = new ReadOnlyCollection<IWebElement>(new List<IWebElement> { mockParentElement.Object });

                // Setup WebDriver mocks
                mockDriver.Setup(d => d.Navigate()).Returns(mockNavigation.Object);
                mockDriver.Setup(d => d.FindElements(It.IsAny<By>())).Returns(parentElements);
                mockDriver.Setup(d => d.Url).Returns("https://www.linkedin.com/job");

                // Setup factory to return our mock driver
                mockFactory.Setup(f => f.Create()).Returns(mockDriver.Object);

                // Setup CaptureSnapshot and SecurityCheck
                mockCapture.Setup(c => c.CaptureArtifacts(It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync("dummyTimestamp");

                mockSecurity.Setup(s => s.IsSecurityChek()).Returns(false);

                var executionOptions = new ExecutionOptions();

                var detailProcessing = new DetailProcessing(
                    mockFactory.Object,
                    mockLogger.Object,
                    mockCapture.Object,
                    mockSecurity.Object,
                    executionOptions,
                    mockDirCheck.Object
                );

                var offers = new List<string> { "https://www.linkedin.com/jobs/view/123456" };

                // Act
                var result = await detailProcessing.ProcessOffersAsync(offers);

                // Assert
                Assert.IsNotNull(result, "Result should not be null");
                Assert.AreEqual(1, result.Count, "Should have processed one job offer");

                mockDriver.Verify(d => d.Navigate().GoToUrl(offers[0]), Times.Once);
                mockCapture.Verify(c => c.CaptureArtifacts(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
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
