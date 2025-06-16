using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configuration
{
    public class JobSearch
    {
        public string SearchText { get; set; }
        public string Location { get; set; }
        public int MaxPages { get; set; }
    }
}
