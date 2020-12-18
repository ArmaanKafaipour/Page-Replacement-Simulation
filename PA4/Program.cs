using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PA4
{
    class Program
    {
        public const int PAGE_TABLE_SIZE = 128;

        static List<Process> processes = new List<Process>();

        static List<MainMemory> mainMem = new List<MainMemory>();

        static string path = @"Z:\\FALL 2020 SCHOOL\\COMPE 571\\PA4\\PA4\\data1.txt";

        static int[] vpn = new int[55000];

        //static int[] offset = new int[];

        static int pageFaultCount = 0;
        static int diskRefCount = 0;
        static int dirtyPageWriteCount = 0;

        static int queue = 0;

        static int maxProcessID = 0;

        public static void Main(string[] args)
        {
            Console.WriteLine("Select 1 for RAND");
            Console.WriteLine("Select 2 for FIFO");
            Console.WriteLine("Select 3 for LRU");
            Console.WriteLine("Select 4 for PER");
            //Console.WriteLine("Select 5 for Optimal");

            int inputOption = Int32.Parse(Console.ReadLine());

            switch (inputOption)
            {
                case 1:
                    Console.WriteLine("Random Page Replacement Selected...");
                    RAND();
                    break;
                case 2:
                    Console.WriteLine("FIFO Page Replacement Selected...");
                    FIFO();
                    break;
                case 3:
                    Console.WriteLine("LRU Selected...");
                    LRU();
                    break;
                case 4:
                    Console.WriteLine("PER Selected...");
                    PER();
                    break;
                //case 5:
                //    Console.WriteLine("Optimal Selected...");
                //    Optimal();
                //    break;
                default:
                    Console.WriteLine("No selection made.");
                    break;
            }

            Console.WriteLine("Page fault count: " + pageFaultCount);
            Console.WriteLine("Disk ref count: " + diskRefCount);
            Console.WriteLine("Dirty Page Write Count: " + dirtyPageWriteCount);

        }

        public static void parseInputData()
        {
            StreamReader sr = new StreamReader(path);
            while (!sr.EndOfStream)
            {
                string[] data = sr.ReadLine().Split('\t');
                Process newProcess = new Process();
                newProcess.ProcessID = Convert.ToInt32(data[0]);
                newProcess.MemAddr = Convert.ToInt32(data[1]);
                newProcess.RWMode = Convert.ToChar(data[2]);
                processes.Add(newProcess);

                if (newProcess.ProcessID > maxProcessID)
                {
                    maxProcessID = newProcess.ProcessID;
                }
            }

            for (int i = 0; i < processes.Count; i++)
            {
                vpn[i] = processes[i].MemAddr >> 9;
                //Console.WriteLine("Virtual Page Number: {0}", vpn[i]);  // For debug virtal page number
                //Console.WriteLine(processes[i]);

                //offset[i] = processes[i].MemAddr << 7;
                //Console.WriteLine("Offset: {0}", offset[i]);
            }
        }

        public static void RAND()
        {
            //Creates new list to find number of distinct processes...not using and hard coding number of processes for now 
            //List<Process> distinct = processes.GroupBy(Process => Process.ProcessID).Select(g => g.First()).ToList();

            parseInputData();

            // Creates a page table for each process , stored in 2D array of objects
            PageTableEntry[,] ptEntries = new PageTableEntry[PAGE_TABLE_SIZE, maxProcessID];

            // Initialize 128 page table entries for each process in input
            for (int processNum = 0; processNum < maxProcessID; processNum++)
            {
                for (int i = 0; i < 128; i++)
                {
                    ptEntries[i, processNum] = new PageTableEntry();
                }
            }

            for (int i = 0; i < processes.Count; i++)
            {
                // Read mode
                if (processes[i].RWMode == 'R')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {
                        pageFaultCount++;
                        diskRefCount++;
                        //ptEntries1[vpn[i]].PhysicalPageNum = i;
                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;

                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);
                        }
                        else
                        {
                            randomPageReplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }
                    }
                    else
                    {
                        //Do nothing if valid bit is set... meaning page is already loaded in to main memory
                        //Console.WriteLine("Page is already loaded in main mem.");
                    }
                }

                // Write mode
                else if (processes[i].RWMode == 'W')
                {
                    //Set dirty bit to 1 when writing
                    ptEntries[vpn[i], processes[i].ProcessID - 1].DirtyBit = 1;

                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {
                        // Page fault if valid bit is 0
                        pageFaultCount++;
                        diskRefCount++;
                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;

                        // Checks if main memory has free pages
                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);
                        }
                        // If main memory does not have free pages, replace one page
                        else
                        {
                            randomPageReplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }
                    }
                    else
                    {
                        //Do nothing if valid bit is set... meaning page is already loaded in to main memory
                        //Console.WriteLine("Page is already loaded in memory.");
                    }
                }

                // More debug info
                // Console.WriteLine("Physical page num: " + ptEntries1[vpn[i]].PhysicalPageNum);
                // Console.WriteLine("Valid bit: " + ptEntries1[vpn[i]].ValidBit);            
            }

            // Prints all 32 pages of main memory, includes process number and vpn that is mapped to the index(ppn)
            //foreach (MainMemory info in mainMem)
            //{
            //    Console.WriteLine(info);
            //}

        }

        public static void randomPageReplacement(PageTableEntry[,] ptEntries, int i, int processNum)
        {
            Random rand = new Random();
            int num = rand.Next(mainMem.Count);

            if (ptEntries[mainMem[num].VirtualPageNum, processNum].DirtyBit == 1)
            {
                // Extra disk reference to write back 
                diskRefCount++;
                // Dirty page write
                dirtyPageWriteCount++;
            }

            // Page that is being removed from main memory
            ptEntries[mainMem[num].VirtualPageNum, processNum].ValidBit = 0;
            ptEntries[mainMem[num].VirtualPageNum, processNum].DirtyBit = 0;
            ptEntries[mainMem[num].VirtualPageNum, processNum].PhysicalPageNum = 0;

            // Remove page from main memory
            mainMem.RemoveAt(num);

            // Add new page to memory with page number and process id
            MainMemory newPageInMem = new MainMemory();
            newPageInMem.ProcessNum = processes[i].ProcessID;
            newPageInMem.VirtualPageNum = vpn[i];
            mainMem.Insert(num, newPageInMem);

            // Set address translation in PTE to the index of the new page that holds that page in main memory
            ptEntries[mainMem[num].VirtualPageNum, processNum].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

        }


        public static void FIFO()
        {
            parseInputData();

            PageTableEntry[,] ptEntries = new PageTableEntry[PAGE_TABLE_SIZE, maxProcessID];

            // Initialize 128 page table entries for each process in input
            for (int processNum = 0; processNum < maxProcessID; processNum++)
            {
                for (int i = 0; i < 128; i++)
                {
                    ptEntries[i, processNum] = new PageTableEntry();
                }
            }

            for (int i = 0; i < processes.Count; i++)
            {
                // Read mode
                if (processes[i].RWMode == 'R')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;

                       
                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

                        }
                        else
                        {
                            FIFOreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }

                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
                    }
                }

                // Write mode
                if (processes[i].RWMode == 'W')
                {              
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;
                        
                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

                        }
                        else
                        {
                            FIFOreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }

                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
                    }

                    ptEntries[vpn[i], processes[i].ProcessID - 1].DirtyBit = 1;
                }

                // More debug info
                // Console.WriteLine("Physical page num: " + ptEntries1[vpn[i]].PhysicalPageNum);
                // Console.WriteLine("Valid bit: " + ptEntries1[vpn[i]].ValidBit);            
            }

        }

        public static void FIFOreplacement(PageTableEntry[,] ptEntries, int i, int processNum)
        {
            // Variabl to parse through main memory from start to end, then resets to beginning to act as FIFO
            if (queue == 32)
            {
                queue = 0;
            }
            if (ptEntries[mainMem[queue].VirtualPageNum, mainMem[queue].ProcessNum].DirtyBit == 1)
            {
                // Extra disk reference to write back 
                diskRefCount++;
                // Dirty page write
                dirtyPageWriteCount++;
            }

            // Page that is being removed from main memory
            ptEntries[mainMem[queue].VirtualPageNum, mainMem[queue].ProcessNum].ValidBit = 0;
            ptEntries[mainMem[queue].VirtualPageNum, mainMem[queue].ProcessNum].DirtyBit = 0;
            ptEntries[mainMem[queue].VirtualPageNum, mainMem[queue].ProcessNum].PhysicalPageNum = 0;

            // Remove next page in main memory
            mainMem.RemoveAt(queue);

            // Add new page to main memory with process ID and VPN
            MainMemory newPageInMem = new MainMemory();
            newPageInMem.ProcessNum = processNum;
            newPageInMem.VirtualPageNum = vpn[i];
            mainMem.Insert(queue, newPageInMem);

            // Set address translation in PTE to the index of the new page that holds that page in main memory
            ptEntries[mainMem[queue].VirtualPageNum, processNum].PhysicalPageNum = mainMem.IndexOf(newPageInMem);


            // Increment fifo counter
            queue++;
        }


        public static void LRU()
        {
            parseInputData();

            PageTableEntry[,] ptEntries = new PageTableEntry[PAGE_TABLE_SIZE, maxProcessID];

            // Initialize 128 page table entries for each process in input
            for (int processNum = 0; processNum < maxProcessID; processNum++)
            {
                for (int i = 0; i < 128; i++)
                {
                    ptEntries[i, processNum] = new PageTableEntry();
                }
            }

            for (int i = 0; i < processes.Count; i++)
            {
                // Read mode
                if (processes[i].RWMode == 'R')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;

                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;

                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            newPageInMem.TimeUsedLast = i;
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

                        }
                        else
                        {
                            LRUreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }
                    }
                    else if(ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 1)
                    {
                        mainMem[ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum].TimeUsedLast = i;
                    }
                }
                if (processes[i].RWMode == 'W')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;

                        ptEntries[vpn[i], processes[i].ProcessID - 1].DirtyBit = 1;
                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;

                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            newPageInMem.TimeUsedLast = i;
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

                        }
                        else
                        {
                            LRUreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }
                    }
                    else if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 1)
                    {
                        mainMem[ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum].TimeUsedLast = i;
                    }
                }
            }
        }

        public static void LRUreplacement(PageTableEntry[,] ptEntries, int i, int processNum)
        {
            int LRUindex = 0;
            int lowestRecentTimeUsed = 100000;

            for (int x = 0; x < mainMem.Count; x++)
            {   
                // Finds lowest recently time used and saves the index of that main mem page to variable 'LRUindex'.
                // Will default save value of lower numbered page if both or neither are dirty 
                if(mainMem[x].TimeUsedLast < lowestRecentTimeUsed)
                {
                    lowestRecentTimeUsed = mainMem[x].TimeUsedLast;
                    LRUindex = x;
                }
                // If two with same LRU time, then save the index of the one that is not dirty. 
                if(mainMem[x].TimeUsedLast == lowestRecentTimeUsed)
                {
                    if(ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].DirtyBit == 0)
                    {
                        LRUindex = x;
                    }
                }    
            }

            if (ptEntries[mainMem[LRUindex].VirtualPageNum, mainMem[LRUindex].ProcessNum].DirtyBit == 1)
            {
                // Extra disk reference to write back 
                diskRefCount++;
                // Dirty page write
                dirtyPageWriteCount++;
            }

            // Page that is being removed from main memory
            ptEntries[mainMem[LRUindex].VirtualPageNum, mainMem[LRUindex].ProcessNum].ValidBit = 0;
            ptEntries[mainMem[LRUindex].VirtualPageNum, mainMem[LRUindex].ProcessNum].DirtyBit = 0;
            ptEntries[mainMem[LRUindex].VirtualPageNum, mainMem[LRUindex].ProcessNum].PhysicalPageNum = 0;

            // Remove next page in main memory
            mainMem.RemoveAt(LRUindex);

            // Add new page to main memory with process ID and VPN and LRU time
            MainMemory newPageInMem = new MainMemory();
            newPageInMem.ProcessNum = processNum;
            newPageInMem.VirtualPageNum = vpn[i];
            newPageInMem.TimeUsedLast = i;
            mainMem.Insert(LRUindex, newPageInMem);

            // Set address translation in PTE to the index of the new page that holds that page in main memory
            ptEntries[mainMem[LRUindex].VirtualPageNum, mainMem[LRUindex].ProcessNum].PhysicalPageNum = mainMem.IndexOf(newPageInMem);
            
        }

        public static void PER()
        {
            parseInputData();

            PageTableEntry[,] ptEntries = new PageTableEntry[PAGE_TABLE_SIZE, maxProcessID];

            // Initialize 128 page table entries for each process in input
            for (int processNum = 0; processNum < maxProcessID; processNum++)
            {
                for (int i = 0; i < 128; i++)
                {
                    ptEntries[i, processNum] = new PageTableEntry();
                }
            }

            for (int i = 0; i < processes.Count; i++)
            {
                // Set all reference bits to 0 every 200 memory references
                if(i % 200 == 0)
                {
                    for (int processNumTemp = 0; processNumTemp < maxProcessID; processNumTemp++)
                    {
                        for (int j = 0; j < 128; j++)
                        {
                            ptEntries[j, processNumTemp].ReferenceBit = 0;
                        }
                    }
                }

                // Read mode
                if (processes[i].RWMode == 'R')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;

                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);
                        }
                        else
                        {
                            PERreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }

                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
                    }
                    ptEntries[vpn[i], processes[i].ProcessID - 1].ReferenceBit = 1;
                }

                // Write mode
                if (processes[i].RWMode == 'W')
                {
                    if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
                    {

                        pageFaultCount++;
                        diskRefCount++;

                        if (mainMem.Count < 32)
                        {
                            MainMemory newPageInMem = new MainMemory();
                            newPageInMem.ProcessNum = processes[i].ProcessID - 1;
                            newPageInMem.VirtualPageNum = vpn[i];
                            mainMem.Add(newPageInMem);

                            ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);
                        }
                        else
                        {
                            PERreplacement(ptEntries, i, processes[i].ProcessID - 1);
                        }

                        ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
                        
                    }
                    ptEntries[vpn[i], processes[i].ProcessID - 1].ReferenceBit = 1;
                    ptEntries[vpn[i], processes[i].ProcessID - 1].DirtyBit = 1;
                }         
            }

        }

        public static void PERreplacement(PageTableEntry[,] ptEntries, int i, int processNum)
        {
            int saveMemIndex = 0;

            for (int x = 0; x < mainMem.Count; x++)
            {
                // Unused page where ref bit is 0
                if (ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].ReferenceBit == 0)
                {
                    // Unused page where ref bit is 0 and dirty bit is 0
                    if (ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].DirtyBit == 0)
                    {
                        saveMemIndex = x;
                        break;
                    }
                    else if(ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].DirtyBit == 1)
                    {
                        saveMemIndex = x;
                    }
                }
                // Referenced page where ref bit is 1
                else if(ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].ReferenceBit == 1)
                {
                   // Ref bit is 1 and dirty bit is 0
                   if(ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].DirtyBit == 0)
                   {
                        saveMemIndex = x;
                        break;
                   }
                   // Ref bit is 1 and dirty bit is 1
                   else if(ptEntries[mainMem[x].VirtualPageNum, mainMem[x].ProcessNum].DirtyBit == 1)
                   {
                        saveMemIndex = x;
                    }
  
                }
            }

            if (ptEntries[mainMem[saveMemIndex].VirtualPageNum, mainMem[saveMemIndex].ProcessNum].DirtyBit == 1)
            {
                // Extra disk reference to write back 
                diskRefCount++;
                // Dirty page write
                dirtyPageWriteCount++;
            }

            // Page that is being removed from main memory
            ptEntries[mainMem[saveMemIndex].VirtualPageNum, mainMem[saveMemIndex].ProcessNum].ValidBit = 0;
            ptEntries[mainMem[saveMemIndex].VirtualPageNum, mainMem[saveMemIndex].ProcessNum].DirtyBit = 0;
            ptEntries[mainMem[saveMemIndex].VirtualPageNum, mainMem[saveMemIndex].ProcessNum].PhysicalPageNum = 0;

            // Remove next page in main memory
            mainMem.RemoveAt(saveMemIndex);

            // Add new page to main memory with process ID and VPN
            MainMemory newPageInMem = new MainMemory();
            newPageInMem.ProcessNum = processNum;
            newPageInMem.VirtualPageNum = vpn[i];
            mainMem.Insert(saveMemIndex, newPageInMem);

            // Set address translation in PTE to the index of the new page that holds that page in main memory
            ptEntries[mainMem[saveMemIndex].VirtualPageNum, processNum].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

        }


        //public static void Optimal()
        //{
        //    parseInputData();

        //    PageTableEntry[,] ptEntries = new PageTableEntry[PAGE_TABLE_SIZE, maxProcessID];

        //    // Initialize 128 page table entries for each process in input
        //    for (int processNum = 0; processNum < maxProcessID; processNum++)
        //    {
        //        for (int i = 0; i < 128; i++)
        //        {
        //            ptEntries[i, processNum] = new PageTableEntry();
        //        }
        //    }

        //    for (int i = 0; i < processes.Count; i++)
        //    {
        //        // Read mode
        //        if (processes[i].RWMode == 'R')
        //        {
        //            if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
        //            {

        //                pageFaultCount++;
        //                diskRefCount++;


        //                if (mainMem.Count < 32)
        //                {
        //                    MainMemory newPageInMem = new MainMemory();
        //                    newPageInMem.ProcessNum = processes[i].ProcessID - 1;
        //                    newPageInMem.VirtualPageNum = vpn[i];
        //                    mainMem.Add(newPageInMem);

        //                    ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

        //                }
        //                else
        //                {
        //                    OptimalReplacement(ptEntries, i, processes[i].ProcessID - 1);
        //                }

        //                ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
        //            }
        //        }

        //        // Write mode
        //        if (processes[i].RWMode == 'W')
        //        {
        //            if (ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit == 0)
        //            {

        //                pageFaultCount++;
        //                diskRefCount++;

        //                if (mainMem.Count < 32)
        //                {
        //                    MainMemory newPageInMem = new MainMemory();
        //                    newPageInMem.ProcessNum = processes[i].ProcessID - 1;
        //                    newPageInMem.VirtualPageNum = vpn[i];
        //                    mainMem.Add(newPageInMem);

        //                    ptEntries[vpn[i], processes[i].ProcessID - 1].PhysicalPageNum = mainMem.IndexOf(newPageInMem);

        //                }
        //                else
        //                {
        //                    OptimalReplacement(ptEntries, i, processes[i].ProcessID - 1);
        //                }

        //                ptEntries[vpn[i], processes[i].ProcessID - 1].ValidBit = 1;
        //            }

        //            ptEntries[vpn[i], processes[i].ProcessID - 1].DirtyBit = 1;
        //        }

        //        // More debug info
        //        // Console.WriteLine("Physical page num: " + ptEntries1[vpn[i]].PhysicalPageNum);
        //        // Console.WriteLine("Valid bit: " + ptEntries1[vpn[i]].ValidBit);            
        //    }

        //}

        //public static void OptimalReplacement(PageTableEntry[,] ptEntries, int i, int processNum)
        //{
        //    int physicalIndex = 0;
        //    int replaceIndex = 0;
        //    int processIndex = 0;

            
        //    for (physicalIndex = 0; physicalIndex < mainMem.Count; physicalIndex++)
        //    {
        //        for (int x = i+1; x < processes.Count; x++)
        //        {
        //            if (mainMem[physicalIndex].ProcessNum == processes[x].ProcessID - 1)
        //            {
        //                if (mainMem[physicalIndex].VirtualPageNum != vpn[x])
        //                {
        //                    replaceIndex = physicalIndex;
        //                }
        //            }           
        //        }
        //    }

        //    if (ptEntries[mainMem[replaceIndex].VirtualPageNum, mainMem[replaceIndex].ProcessNum].DirtyBit == 1)
        //    {
        //        // Extra disk reference to write back 
        //        diskRefCount++;
        //        // Dirty page write
        //        dirtyPageWriteCount++;
        //    }

        //    // Page that is being removed from main memory
        //    ptEntries[mainMem[replaceIndex].VirtualPageNum, mainMem[replaceIndex].ProcessNum].ValidBit = 0;
        //    ptEntries[mainMem[replaceIndex].VirtualPageNum, mainMem[replaceIndex].ProcessNum].DirtyBit = 0;
        //    ptEntries[mainMem[replaceIndex].VirtualPageNum, mainMem[replaceIndex].ProcessNum].PhysicalPageNum = 0;

        //    // Remove next page in main memory
        //    mainMem.RemoveAt(replaceIndex);

        //    // Add new page to main memory with process ID and VPN
        //    MainMemory newPageInMem = new MainMemory();
        //    newPageInMem.ProcessNum = processNum;
        //    newPageInMem.VirtualPageNum = vpn[i];
        //    mainMem.Insert(replaceIndex, newPageInMem);

        //    // Set address translation in PTE to the index of the new page that holds that page in main memory
        //    ptEntries[mainMem[replaceIndex].VirtualPageNum, processNum].PhysicalPageNum = mainMem.IndexOf(newPageInMem);



        //}

    }

}

