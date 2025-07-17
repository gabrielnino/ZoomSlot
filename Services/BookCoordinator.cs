using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services
{
    public class BookCoordinator(ILoginBook loginBook, ILogger<BookCoordinator> logger, IBooking booking) : IBookCoordinator
    {
        private readonly ILoginBook _loginBook = loginBook;
        private readonly ILogger<BookCoordinator> _logger = logger;
        private readonly IBooking _booking = booking;

        public async Task BookAsyn()
        {
            await _loginBook.LoginAsync();
            await _booking.Search();
        }
    }
}
