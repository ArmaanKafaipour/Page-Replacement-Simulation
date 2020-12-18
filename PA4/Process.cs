using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4
{
    public class Process
    {

        public int ProcessID { get; set; }

        public int MemAddr { get; set; }

        public char RWMode { get; set; }

        public override string ToString()
        {
            return "ID: " + ProcessID + "   Memory Address: " + MemAddr + "   Read or Write: " + RWMode;
        }
    }
}
