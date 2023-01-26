using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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

            return BinaryType.UNKNOWN; // impossible
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

        #region DllImport

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, [Out] IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
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