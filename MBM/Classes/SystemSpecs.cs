using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace MBM.Classes
{
    /// <summary>
    /// This class will return the current system specifications
    /// Currently returning: User, machine name, OS, processor and available RAM
    /// </summary>
    public static class SystemSpecs
    {
        // Return system details as a string to be displayed
        internal static string SystemDetails()
        {
            try // Build and return string
            {
                StringBuilder systemDetails = new StringBuilder();
                systemDetails.AppendLine($"User: {Environment.UserName}");
                systemDetails.AppendLine($"Machine Name: {Environment.MachineName}");
                systemDetails.AppendLine($"Operating System: {GetOSInfo()}");
                systemDetails.AppendLine($"CPU Usage: {GetCPUInfo()}");
                systemDetails.AppendLine($"RAM: {ConvertBytesToGB(GetTotalMemoryInBytes())} ({ConvertBytesToGB(GetAvailableMemoryInBytes())} available)");
                systemDetails.AppendLine("----------------------------");
                systemDetails.AppendLine("HDD Information:");
                systemDetails.AppendLine(GetAvailableFreeSpace());
                systemDetails.AppendLine("----------------------------");

                return systemDetails.ToString();
            }
            catch (Exception)
            {
                return "Unable to retreive system specifications";
            }
        }

        private static string GetOSInfo() // Use LINQ to return the "friendly" OS name and type
        {
            string operatingSystemType;
            if (Environment.Is64BitOperatingSystem) { operatingSystemType = "64-Bit"; } else { operatingSystemType = "32-Bit"; }

            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();

            name += $" ({operatingSystemType})";

            return name != null ? name.ToString() : "Unknown";
        }

        private static ulong GetTotalMemoryInBytes() // Total memory
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }

        private static ulong GetAvailableMemoryInBytes() // Available memory
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
        }

        private static object GetCPUInfo() // Returns current CPU usage
        {
            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            // will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(100);

            // now matches task manager reading
            dynamic secondValue = Math.Round(cpuCounter.NextValue(), 2);

            return secondValue + "%";
        }

        private static string GetAvailableFreeSpace() // Returns HDD information for the current system
        {
            StringBuilder driveDetails = new StringBuilder();

            try
            {
                DriveInfo[] arrayOfDrives = DriveInfo.GetDrives();

                foreach (var d in arrayOfDrives)
                {
                    driveDetails.AppendLine($"Drive: {d.Name} ({d.DriveType})");
                    driveDetails.AppendLine($"Total Size: {ConvertBytesToGB((ulong)d.TotalSize)} bytes");
                    driveDetails.Append($"Available Free Space : {ConvertBytesToGB((ulong)d.AvailableFreeSpace)} bytes");
                }
            }
            catch (Exception)
            {
               return "Error getting HDD details";
            }

            return driveDetails.ToString();
        }

        private static string ConvertBytesToGB(ulong totalAmount) // Converts ulong bytes to a GB value
        {
            double totalAmountDouble = Convert.ToDouble(totalAmount / (1024 * 1024));
            int totalAmountInt = Convert.ToInt32(Math.Ceiling(totalAmountDouble / 1024).ToString());
            return totalAmountInt.ToString() + " GB";
        }

    }
}
