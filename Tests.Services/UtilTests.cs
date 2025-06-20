using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using Services;

namespace Tests.Services
{


    [TestClass]
    public class UtilTests
    {


        [TestInitialize]
        public void Setup()
        {

        }

        [TestCleanup]
        public void Teardown()
        {

        }

        [TestMethod]
        [DataRow("https://www.linkedin.com/jobs/view/4066112398/")]
        [DataRow("https://www.linkedin.com/jobs/view/4236262543/")]
        [DataRow("https://www.linkedin.com/jobs/view/4205250174/")]
        [DataRow("https://www.linkedin.com/jobs/view/4219985001/")]
        [DataRow("https://www.linkedin.com/jobs/view/4250559505/")]
        [DataRow("https://www.linkedin.com/jobs/view/4224847408/")]
        [DataRow("https://www.linkedin.com/jobs/view/4229315740/")]
        [DataRow("https://www.linkedin.com/jobs/view/4242930313/")]
        [DataRow("https://www.linkedin.com/jobs/view/4127166956/")]
        public async Task ExtractJobIdURL_ShouldReturnValidId(string url)
        {
            var util = new Util();
            var id = util.ExtractJobId(url);

            Assert.IsNotNull(id, "The job ID should not be null.");
            Assert.IsTrue(long.TryParse(id, out _), "The job ID should be numeric.");
            await Task.CompletedTask;  // This keeps your async signature; remove if unnecessary
        }
    }
}
