using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services.Interfaces
{
    public interface IOpenAIClient
    {
        Task<string> GetChatCompletionAsync(Prompt prompt);
    }
}
