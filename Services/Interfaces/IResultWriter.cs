using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IResultWriter
    {
        Task WriteResultsAsync(
            Dictionary<string, List<string>> finalGroups,
            string normalizedOutputPath,
            string summaryOutputPath);
    }

}
