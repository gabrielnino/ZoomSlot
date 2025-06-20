using System.Text.RegularExpressions;
using Services.Interfaces;

namespace Services
{
    public class Util: IUtil
    {
        public string? ExtractJobId(string url)
        {
            var pattern = @"linkedin\.com/jobs/view/(\d+)";
            var match = Regex.Match(url, pattern);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
    }
}
