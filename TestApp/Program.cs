﻿using MinecraftMemoryReader;
using System;

namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MMR.CheckInject();

                Console.WriteLine(MMR.GetArchitectureStr());
                Console.WriteLine(MMR.GetVersion());

                //04973070,0,20,0

                string memorydllPtr = "04973070,0,20,0";

                long addr = MMR.GetMultiLevelPtr(MMR.GetOffset(memorydllPtr), MMR.GetSubOffsets(memorydllPtr)); // V

                if (false == true)
                    addr = MMR.GetMultiLevelPtr(0x04973070, new long[] { 0x0, 0x20 }); // these do the same thing ^

                Console.WriteLine(addr.ToString("X"));
                Console.WriteLine(MMR.ReadMemory_str(addr, false)); // should print ur DID (STRING) on 1.19.51.01

                Console.ReadKey();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }
}
