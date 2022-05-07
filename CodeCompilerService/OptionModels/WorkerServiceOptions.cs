using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilerService.OptionModels
{
    public class WorkerServiceOptions
    {
        public int Interval { get; set; }
        public int InternalBufferSize { get; set; }
        public bool SendMessagesToManager { get; set; }
        public int SendMessagesPort { get; set; }
    }
}
