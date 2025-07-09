using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    using System.Text.Json;
    using Services.Interfaces;

    public class FileService : IFileService
    {
        public async Task<T> ReadJsonAsync<T>(string path)
        {
            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream) ?? throw new InvalidOperationException("Failed to parse JSON");
        }

        public async Task WriteJsonAsync<T>(string path, T data)
        {
            using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task WriteTextAsync(string path, string content)
        {
            await File.WriteAllTextAsync(path, content);
        }
    }

}
