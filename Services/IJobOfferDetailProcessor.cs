using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public interface IJobOfferDetailProcessor
    {
        Task<List<JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers);
    }
}
