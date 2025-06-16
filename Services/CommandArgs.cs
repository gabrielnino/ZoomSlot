using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class CommandArgs
    {
        public bool IsDebugMode { get; }
        public string MainCommand { get; }

        public CommandArgs(string[] args)
        {
            IsDebugMode = args.Contains("--debug");
            MainCommand = args.FirstOrDefault(arg =>
                arg == "--search" || arg == "--export") ?? "--help";
        }
    }
}
