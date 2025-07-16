using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services
{
    public class BookCoordinator(ILoginBook loginBook, ILogger<BookCoordinator> logger) : IBookCoordinator
    {
        private readonly ILoginBook _loginBook = loginBook;
        private readonly ILogger<BookCoordinator> _logger = logger;

        public async Task BookAsyn()
        {
            await _loginBook.LoginAsync();
        }
    }
}
