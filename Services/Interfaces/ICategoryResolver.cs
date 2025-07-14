using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICategoryResolver
    {
        string ResolveCategory(string skill);
        Task InitializeAsync(string categoryFilePath);
        Task WriteAsync(string categoryFilePath, List<string> uncategorized);
    }
}
