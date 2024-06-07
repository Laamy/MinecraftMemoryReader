using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MinecraftMemoryReader
{
    /// <summary>
    /// Credits: yeemi#1929, Founder#2998
    /// </summary>
    public class MMR
    {
        // Declare a variable to store the process and a variable to store the handle to the process
        public static Process proc;

        /// <summary>
        /// Check if the Minecraft process is running and open a handle to the process
        /// </summary>
        /// <exception cref="Exception">Failed to find the game window</exception>
        public static void CheckInject()
        {
            // Check if the Minecraft.Windows process is running
            if (Process.GetProcessesByName("Minecraft.Windows").Length == 0)
                throw new Exception("Please open the game window first");

            // Get the first instance of the running Minecraft.Windows process
            proc = Process.GetProcessesByName("Minecraft.Windows")[0];
        }

        /// <summary>
        /// Get minecrafts production version (For example 1.19.51.01)
        /// </summary>
        /// <returns>ProductVersion</returns>
        public static string GetVersion() => proc.MainModule.FileVersionInfo.ProductVersion;

        public static BinaryType GetArchitecture()
        {
            BinaryType bt;
            if (GetBinaryType(proc.MainModule.FileName, out bt))
                return bt;

            return BinaryType.UNKNOWN;
        }

        public static string GetArchitectureStr()
        {
            if (GetArchitecture() == BinaryType.SCS_64BIT_BINARY)
                return "x64";
            if (GetArchitecture() == BinaryType.SCS_32BIT_BINARY)
                return "x32";
            return "x16";
        }

        /// <summary>
        /// Reads memory from the target process and returns the value of type T at the specified offset.
        /// </summary>
        /// <typeparam name="T">The type of the value to be read from memory.</typeparam>
        /// <param name="offset">The offset in the target process's memory to read from.</param>
        /// <returns>The value read from memory of type T.</returns>
        public static T ReadMemory<T>(long offset, bool includeBase = true) where T : struct
        {
            CheckInject();

            // Get the size of the data to be read
            int size = Marshal.SizeOf<T>();

            // Allocate memory for the buffer
            IntPtr buffer = Marshal.AllocHGlobal(size);

            // Read the data from memory
            if (includeBase)
            {
                ReadProcessMemory(proc.Handle, proc.MainModule.BaseAddress.ToInt64() + offset, buffer, size, out _);
            }
            else
            {
                ReadProcessMemory(proc.Handle, offset, buffer, size, out _);
            }

            // Convert the data to the desired type
            T result = Marshal.PtrToStructure<T>(buffer);

            // Free the allocated memory
            Marshal.FreeHGlobal(buffer);

            // Return the result
            return result;
        }

        /// <summary>
        /// Writes the specified value of type T to memory in the target process at the specified offset.
        /// </summary>
        /// <typeparam name="T">The type of the value to be written to memory.</typeparam>
        /// <param name="offset">The offset in the target process's memory to write to.</param>
        /// <param name="value">The value to be written to memory of type T.</param>
        public static void WriteMemory<T>(long offset, T value, bool includeBase = true) where T : struct
        {
            CheckInject();

            // Get the size of the data to be written
            int size = Marshal.SizeOf<T>();

            // Allocate memory for the buffer
            IntPtr buffer = Marshal.AllocHGlobal(size);

            // Copy the value to be written into the buffer in the correct format
            Marshal.StructureToPtr(value, buffer, true);

            // Write the data to memory
            if (includeBase)
            {
                WriteProcessMemory(proc.Handle, proc.MainModule.BaseAddress.ToInt64() + offset, buffer, size, out _);
            }
            else
            {
                WriteProcessMemory(proc.Handle, offset, buffer, size, out _);
            }

            // Free the allocated memory
            Marshal.FreeHGlobal(buffer);
        }

        /// <summary>
        /// This method reads a memory and returns a string of a specified size starting from a given offset.
        /// </summary>
        /// <param name="offset">The starting point of memory to be read</param>
        /// <param name="includeBase">A flag to include or exclude the base address</param>
        /// <param name="size">The number of characters to be read</param>
        /// <returns>A string of characters read from memory</returns>
        public static string ReadMemory_str(long offset, bool includeBase = true, int size = -1)
        {
            CheckInject();

            // Initialize an empty string
            string result = "";

            // Checking if the size is -1
            if (size == -1)
            {
                // Index to keep track of current position of the memory
                int index = 0;
                // Iterates until the null character is found
                while (true)
                {
                    // Reads the current character
                    char chra = ReadMemory<char>(offset + index, includeBase);

                    // If the current character is null, break the loop
                    if (chra == 0)
                        break;

                    // Concatenates the current character to the result
                    result += chra;

                    // Just incase (it shouldnt ever reach this but just incase yk?)
                    if (index == 10000) break;

                    // Increment the index to move to next position
                    index++;
                }

                // Returns the final string
                return result;
            }

            // Iterates over the size
            for (int index = 0; index < size; index++)
                // Concatenates the result of each memory read
                result += ReadMemory<char>(offset + index, includeBase);

            // Returns the final string
            return result;
        }

        /// <summary>
        /// This method writes a string to a specified memory location starting from a given offset
        /// </summary>
        /// <param name="offset">The starting point of memory to be written</param>
        /// <param name="value">The string to be written to memory</param>
        /// <param name="includeBase">A flag to include or exclude the base address</param>
        public static void WriteMemory_str(long offset, string value, bool includeBase = true)
        {
            CheckInject();

            // Index to keep track of current position of the memory
            int index = 0;

            // Iterates through the string
            foreach (char chra in value)
            {
                // Writes each character to the memory
                WriteMemory(offset + index, chra, includeBase);

                // Increment the index to move to next position
                index++;
            }
        }

        /// <summary>
        /// This method reads a multi-level pointer in memory by following a series of offsets
        /// </summary>
        /// <param name="offset">The starting point of memory to be read</param>
        /// <param name="offsets">An array of offsets to follow to reach the final memory location</param>
        /// <returns>The final memory location</returns>
        public static long GetMultiLevelPtr(long offset, long[] offsets)
        {
            CheckInject();

            // Reads the first level of the pointer
            long curAddr = ReadMemory<long>(offset);

            // Iterates through the offsets array and follows the pointer to the final memory location
            foreach (long addr in offsets)
                curAddr = ReadMemory<long>(curAddr + addr, false);

            return curAddr;
        }

        /// <summary>
        /// Get the offset of a memory.dll string
        /// </summary>
        public static long GetOffset(string value) => Convert.ToInt64(value.Split(',')[0], 16);

        /// <summary>
        /// Get the sub offsets of a memory.dll string
        /// </summary>
        public static long[] GetSubOffsets(string value)
        {
            string[] offsets = value.Split(',');

            List<long> subOffsets = new List<long>();

            int index = 0;
            foreach (string str in offsets)
            {
                if (index != 0 && index != offsets.Length - 1)
                {
                    subOffsets.Add(Convert.ToInt64(str, 16));
                }

                index++;
            }

            return subOffsets.ToArray();
        }

        /// <summary>
        /// Scans for writable memory in the main model/base model of the process.
        /// Process split into 4 functions; ParseSignature, IsMatch, FindSignatureInChunk, FindSignature
        /// </summary>
        /// <param name="signature">The signature to search for.</param>
        /// <returns>The memory addresses where the signature was found.</returns>
        public static long[] SigScan(string signature, ProcessModule[] modules, bool stopEarly = false)
        {
            if (proc == null)
            {
                throw new InvalidOperationException("Process not set.");
            }

            // Helper function to parse the signature string
            byte[] ParseSignature(string sig)
            {
                string[] parts = sig.Split(' ');
                byte[] bytes = new byte[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    bytes[i] = parts[i] == "?" ? (byte)0x07 : Convert.ToByte(parts[i], 16);
                }
                return bytes;
            }

            // Helper function to check if the signature matches at a given position
            bool IsMatch(byte[] memory, int position, byte[] sig)
            {
                for (int i = 0; i < sig.Length; i++)
                {
                    if (sig[i] != 0x07 && memory[position + i] != sig[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            long FindSignatureInChunk(byte[] memory, byte[] sig, int start, int end)
            {
                for (int i = start; i < end - sig.Length + 1; i++)
                {
                    if (IsMatch(memory, i, sig))
                    {
                        return i;
                    }
                }
                return -1;
            }

            long FindSignature(byte[] memory, byte[] sig)
            {
                int chunkSize = memory.Length / Environment.ProcessorCount;
                long[] results = new long[Environment.ProcessorCount];

                Parallel.For(0, Environment.ProcessorCount, i =>
                {
                    int start = i * chunkSize;
                    int end = (i == Environment.ProcessorCount - 1) ? memory.Length - sig.Length + 1 : (i + 1) * chunkSize;
                    results[i] = FindSignatureInChunk(memory, sig, start, end);
                });

                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    if (results[i] != -1)
                    {
                        return results[i];
                    }
                }

                return -1;
            }

            byte[] signatureBytes = ParseSignature(signature);
            IntPtr processHandle = proc.Handle;

            List<long> addresses = new List<long>();

            foreach (ProcessModule module in modules)
            {
                IntPtr baseAddress = module.BaseAddress;
                int moduleSize = module.ModuleMemorySize;
                byte[] moduleMemory = new byte[moduleSize];

                if (ReadProcessMemory(processHandle, baseAddress, moduleMemory, moduleSize, out _))
                {
                    long result = FindSignature(moduleMemory, signatureBytes);
                    if (result >= 0)
                    {
                        addresses.Add(baseAddress.ToInt64() + result);

                        if (stopEarly)
                            return addresses.ToArray();
                        //return baseAddress.ToInt64() + result;
                    }
                }
            }

            //foreach (ProcessModule module in proc.Modules)
            //{
            //    IntPtr baseAddress = module.BaseAddress;
            //    int moduleSize = module.ModuleMemorySize;
            //    byte[] moduleMemory = new byte[moduleSize];
            //    if (ReadProcessMemory(processHandle, baseAddress, moduleMemory, moduleSize, out _))
            //    {
            //        long result = FindSignature(moduleMemory, signatureBytes);
            //        if (result >= 0)
            //        {
            //            return baseAddress.ToInt64() + result;
            //        }
            //    }
            //}

            return addresses.ToArray();
        }

        #region DllImport

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, [Out] IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool GetBinaryType(string lpApplicationName, out BinaryType lpBinaryType);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, long lpBaseAddress, IntPtr lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        #endregion

        public enum BinaryType : uint
        {
            SCS_32BIT_BINARY = 0,   // A 32-bit Windows-based application
            SCS_DOS_BINARY = 1,     // An MS-DOS � based application
            SCS_WOW_BINARY = 2,      // A 16-bit Windows-based application
            SCS_PIF_BINARY = 3,     // A PIF file that executes an MS-DOS � based application
            SCS_POSIX_BINARY = 4,   // A POSIX � based application
            SCS_OS216_BINARY = 5,   // A 16-bit OS/2-based application
            SCS_64BIT_BINARY = 6,   // A 64-bit Windows-based application.
            UNKNOWN
        }
    }
}