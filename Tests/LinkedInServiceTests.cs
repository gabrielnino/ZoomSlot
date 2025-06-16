using System.Threading.Tasks;

namespace Tests
{
    public class LinkedInServiceTests
    {
        //private readonly Mock<IWebDriver> _driverMock;
        //private readonly Mock<ILogger<LinkedInService>> _loggerMock;
        //private readonly Mock<IJobStorageService> _storageMock;
        //private readonly LinkedInService _service;

        //public LinkedInServiceTests()
        //{
        //    _driverMock = new Mock<IWebDriver>();
        //    _loggerMock = new Mock<ILogger<LinkedInService>>();
        //    _storageMock = new Mock<IJobStorageService>();

        //    var driverFactoryMock = new Mock<IWebDriverFactory>();
        //    driverFactoryMock.Setup(x => x.Create()).Returns(_driverMock.Object);

        //    var config = new AppConfig
        //    {
        //        LinkedInCredentials = new LinkedInCredentials
        //        {
        //            Email = "test@example.com",
        //            Password = "password"
        //        },
        //        JobSearch = new JobSearch
        //        {
        //            SearchText = "C# Developer",
        //            Location = "Remote",
        //            MaxPages = 2
        //        }
        //    };

        //    _service = new LinkedInService(
        //        driverFactoryMock.Object,
        //        config,
        //        _loggerMock.Object,
        //        _storageMock.Object);
        //}

        //[Fact]
        //public async Task SearchJobsAsync_ShouldCallLoginAndSearch()
        //{
        //    // Arrange
        //    var emailInputMock = new Mock<IWebElement>();
        //    var passwordInputMock = new Mock<IWebElement>();
        //    var loginButtonMock = new Mock<IWebElement>();

        //    _driverMock.Setup(d => d.FindElement(By.Id("username"))).Returns(emailInputMock.Object);
        //    _driverMock.Setup(d => d.FindElement(By.Id("password"))).Returns(passwordInputMock.Object);
        //    _driverMock.Setup(d => d.FindElement(By.CssSelector("button[type='submit']"))).Returns(loginButtonMock.Object);

        //    // Act
        //    await _service.SearchJobsAsync();

        //    // Assert
        //    emailInputMock.Verify(e => e.SendKeys("test@example.com"), Times.Once);
        //    passwordInputMock.Verify(e => e.SendKeys("password"), Times.Once);
        //    loginButtonMock.Verify(e => e.Click(), Times.Once);
        //}
    }
}
