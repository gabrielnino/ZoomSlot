using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class CommandArgs
    {
        public string MainCommand { get; }

        public CommandArgs(string[] args)
        {
            MainCommand = args.FirstOrDefault(arg =>
                arg == "--search" || arg == "--export") ?? "--help";
        }
    }
}
