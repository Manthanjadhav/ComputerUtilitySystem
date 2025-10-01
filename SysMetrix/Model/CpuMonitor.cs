using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysMetrix
{
    // Data model classes
    public class CpuUsageInfo
    {
        public string Timestamp { get; set; }
        public string MachineName { get; set; }
        public int ProcessorCount { get; set; }
        public double TotalCpuUsage { get; set; }
        public List<CoreUsage> Cores { get; set; }
        public SystemInfoSimple SystemInfo { get; set; }
        public List<DiskUsage> DiskUsage { get; set; } 
    }

    public class CoreUsage
    {
        public int CoreId { get; set; }
        public double Usage { get; set; }
    }

    public class SystemInfoSimple
    {
        public long AvailableMemoryMB { get; set; }
        public int ProcessCount { get; set; }
        public int ThreadCount { get; set; }
        public long CurrentProcessMemoryMB { get; set; }
        public int CurrentProcessThreads { get; set; }
        public double UptimeHours { get; set; }
    }

    public class SystemInfo
    {
        public long TotalMemoryMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public long UsedMemoryMB { get; set; }
        public double MemoryUsagePercent { get; set; }
        public int ProcessCount { get; set; }
        public int ThreadCount { get; set; }
        public double UptimeHours { get; set; }
    }

    public class DiskUsage
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public string DriveType { get; set; }
        public string DriveFormat { get; set; }
        public long TotalSizeGB { get; set; }
        public long UsedSpaceGB { get; set; }
        public long FreeSpaceGB { get; set; }
        public double UsagePercent { get; set; }
        public bool IsReady { get; set; }
    }
}
