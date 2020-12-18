using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4
{
    public class PageTableEntry
    {
        public int PhysicalPageNum { get; set; }
        public int DirtyBit { get; set; }
        public int ReferenceBit { get; set; }
        public int ValidBit { get; set; }
        public override string ToString()
        {
            return "Address Translation: " + PhysicalPageNum + "    Dirty Bit: " + DirtyBit + "     Valid Bit: " + ValidBit;
        }
    }
}
