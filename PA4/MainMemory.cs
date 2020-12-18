using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4
{
    public class MainMemory
    {
        public int ProcessNum { get; set; }
        public int VirtualPageNum { get; set; }

        public int TimeUsedLast { get; set; }

        public override string ToString()
        {
            return "Process Number: " + ProcessNum + "   Page number: " + VirtualPageNum + "    Time Last Used: " + TimeUsedLast;
        }
    }
}
