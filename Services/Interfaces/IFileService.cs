using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFileService
    {
        Task<T> ReadJsonAsync<T>(string path);
        Task WriteJsonAsync<T>(string path, T data);
        Task WriteTextAsync(string path, string content);
    }

}
