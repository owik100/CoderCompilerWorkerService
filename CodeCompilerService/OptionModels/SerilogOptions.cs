using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilerService.OptionModels
{
    public class SerilogOptions
    {
        public List<string> Using { get; set; }
        public string MinimumLevel { get; set; }
        public List<WriteTo> WriteTo { get; set; }
    }

    public class WriteTo
    {
        public string Name { get; set; }
        public Args Args { get; set; }
    }

    public class Args
    {
        public string source { get; set; }
        public string restrictedToMinimumLevel { get; set; }
        public bool manageEventSource { get; set; }
        public string outputTemplate { get; set; }
        public string path { get; set; }
    }
}
