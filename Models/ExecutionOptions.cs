using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ExecutionOptions
    {
        public ExecutionOptions()
        {
            TimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public string ExecutionFolder => Path.Combine(Directory.GetCurrentDirectory(), $"{FolderName}_{TimeStamp}");
        public static string FolderName => "Execution";
        public string TimeStamp { get; }
    }
}
