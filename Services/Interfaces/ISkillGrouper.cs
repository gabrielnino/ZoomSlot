using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ISkillGrouper
    {
        Dictionary<string, List<string>> GroupSkills(IEnumerable<string> skills);
    }
}
