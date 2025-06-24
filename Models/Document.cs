using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Document
    {
        public Resume Resume { get; set; }
        public JobOffer JobOffer { get; set; }
        public CoverLetter CoverLetter { get; set; }
    }
}
