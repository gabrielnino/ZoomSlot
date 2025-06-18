using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public interface IJobOfferDetail
    {
        Task<List<Models.JobOfferDetail>> ProcessOffersAsync(IEnumerable<string> offers);
    }
}
