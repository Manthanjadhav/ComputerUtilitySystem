using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace SysMetrix
{
    public static class SystemMonitor
    {
        public static async Task<CpuUsageInfo> GetCpuUsageAsync()
        {
            var cpuUsageInfo = new CpuUsageInfo
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                Cores = new List<CoreUsage>(),
                DiskUsage = new List<DiskUsage>(),
                IpAddress = string.Empty
            };

            try
            {
                // Run all monitoring tasks in parallel
                var tasks = new List<Task>
                {
                    Task.Run(async () => await GetCpuMetricsAsync(cpuUsageInfo)),
                    Task.Run(async () => cpuUsageInfo.SystemInfo = await GetSystemInfoSimpleAsync()),
                    Task.Run(async () => cpuUsageInfo.DiskUsage = await GetDiskUsageAsync()),
                    Task.Run(async () => cpuUsageInfo.IpAddress = await GetLocalIPAddress())
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting system info: {ex.Message}");
                //cpuUsageInfo.Error = $"Error getting system usage: {ex.Message}";
            }

            return cpuUsageInfo;
        }

        private static async Task GetCpuMetricsAsync(CpuUsageInfo cpuUsageInfo)
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Initialize all performance counters
                    var totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    var coreCpus = new List<PerformanceCounter>();

                    for (int i = 0; i < Environment.ProcessorCount; i++)
                    {
                        try
                        {
                            coreCpus.Add(new PerformanceCounter("Processor", "% Processor Time", i.ToString()));
                        }
                        catch
                        {
                            coreCpus.Add(null);
                        }
                    }

                    // First call to all counters (returns 0)
                    totalCpu.NextValue();
                    var coreTasks = coreCpus.Where(c => c != null).Select(c => Task.Run(() => c.NextValue())).ToArray();
                    await Task.WhenAll(coreTasks);

                    // Wait for counters to update
                    await Task.Delay(100);

                    // Get all values in parallel
                    var totalTask = Task.Run(() => totalCpu.NextValue());
                    var coreValueTasks = coreCpus.Select(c => c != null ? Task.Run(() => c.NextValue()) : Task.FromResult(0f)).ToArray();

                    await Task.WhenAll(coreValueTasks);
                    cpuUsageInfo.TotalCpuUsage = Math.Round(await totalTask, 2);

                    for (int i = 0; i < coreCpus.Count; i++)
                    {
                        if (coreCpus[i] != null)
                        {
                            var value = await coreValueTasks[i];
                            cpuUsageInfo.Cores.Add(new CoreUsage
                            {
                                CoreId = i,
                                Usage = Math.Round(value, 2)
                            });
                        }
                        else
                        {
                            cpuUsageInfo.Cores.Add(new CoreUsage { CoreId = i, Usage = -1 });
                        }
                    }

                    // Dispose counters
                    totalCpu.Dispose();
                    foreach (var counter in coreCpus.Where(c => c != null))
                    {
                        counter.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error getting CPU metrics: {ex.Message}", ex);
                }
            });
        }

        public static async Task<SystemInfoSimple> GetSystemInfoSimpleAsync()
        {
            return await Task.Run(() =>
            {
                var systemInfo = new SystemInfoSimple();

                try
                {
                    // Get available memory
                    using (var availableMemory = new PerformanceCounter("Memory", "Available MBytes"))
                    {
                        systemInfo.AvailableMemoryMB = (long)availableMemory.NextValue();
                    }

                    // Get process info
                    var processes = Process.GetProcesses();
                    systemInfo.ProcessCount = processes.Length;
                    systemInfo.ThreadCount = processes.Sum(p =>
                    {
                        try { return p.Threads.Count; }
                        catch { return 0; }
                    });

                    // Get current process info
                    using (var currentProcess = Process.GetCurrentProcess())
                    {
                        systemInfo.CurrentProcessMemoryMB = currentProcess.WorkingSet64 / 1048576;
                        systemInfo.CurrentProcessThreads = currentProcess.Threads.Count;
                    }

                    // Get system uptime
                    var uptimeMs = Environment.TickCount;
                    systemInfo.UptimeHours = Math.Round(uptimeMs / 3600000.0, 2);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error getting system info: {ex.Message}");
                }

                return systemInfo;
            });
        }

        public static async Task<List<DiskUsage>> GetDiskUsageAsync()
        {
            return await Task.Run(async () =>
            {
                var diskUsageList = new List<DiskUsage>();

                try
                {
                    // Get all drives on the system
                    DriveInfo[] allDrives = DriveInfo.GetDrives();

                    // Process each drive asynchronously
                    var diskTasks = allDrives.Select(async drive =>
                    {
                        return await Task.Run(() =>
                        {
                            var diskUsage = new DiskUsage
                            {
                                DriveLetter = drive.Name,
                                DriveType = drive.DriveType.ToString(),
                                IsReady = drive.IsReady
                            };

                            if (drive.IsReady)
                            {
                                try
                                {
                                    diskUsage.VolumeLabel = string.IsNullOrEmpty(drive.VolumeLabel)
                                        ? "Local Disk"
                                        : drive.VolumeLabel;
                                    diskUsage.DriveFormat = drive.DriveFormat;

                                    // Calculate sizes in GB
                                    diskUsage.TotalSizeGB = drive.TotalSize / (1024L * 1024L * 1024L);
                                    diskUsage.FreeSpaceGB = drive.AvailableFreeSpace / (1024L * 1024L * 1024L);
                                    diskUsage.UsedSpaceGB = diskUsage.TotalSizeGB - diskUsage.FreeSpaceGB;

                                    // Calculate usage percentage
                                    if (diskUsage.TotalSizeGB > 0)
                                    {
                                        diskUsage.UsagePercent = Math.Round(
                                            (double)diskUsage.UsedSpaceGB / diskUsage.TotalSizeGB * 100, 2);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    diskUsage.VolumeLabel = "Error reading drive";
                                    Console.Error.WriteLine($"Error reading drive {drive.Name}: {ex.Message}");
                                }
                            }
                            else
                            {
                                diskUsage.VolumeLabel = "Not Ready";
                                diskUsage.DriveFormat = "N/A";
                            }

                            return diskUsage;
                        });
                    }).ToArray();

                    var results = await Task.WhenAll(diskTasks);
                    diskUsageList.AddRange(results);

                    // Sort by drive letter for consistent output
                    diskUsageList = diskUsageList.OrderBy(d => d.DriveLetter).ToList();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error getting disk usage: {ex.Message}");
                }

                return diskUsageList;
            });
        }

        private static async Task<string> GetLocalIPAddress()
        {
            try
            {
                // Get all host addresses asynchronously
                var host = await Dns.GetHostEntryAsync(Dns.GetHostName());

                // Pick the first valid IPv4 that isn't loopback
                var ip = host.AddressList
                             .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork
                                                  && !IPAddress.IsLoopback(a));

                if (ip != null)
                    return ip.ToString();

                // Fallback: try outbound UDP socket (real NIC only)
                return await Task.Run(() =>
                {
                    try
                    {
                        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                        {
                            socket.Connect("8.8.8.8", 65530); // doesn't actually send packets
                            if (socket.LocalEndPoint is IPEndPoint endPoint
                                && !IPAddress.IsLoopback(endPoint.Address))
                            {
                                return endPoint.Address.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // log fallback error if needed
                        Console.Error.WriteLine($"[IP Lookup Fallback Error] {ex.Message}");
                    }

                    return string.Empty; // nothing found
                });
            }
            catch (Exception ex)
            {
                // top-level error handling
                Console.Error.WriteLine($"[IP Lookup Error] {ex.Message}");
                return string.Empty;
            }
        }

        // Synchronous wrapper methods for backward compatibility
        public static CpuUsageInfo GetCpuUsage()
        {
            return GetCpuUsageAsync().GetAwaiter().GetResult();
        }

        public static SystemInfoSimple GetSystemInfoSimple()
        {
            return GetSystemInfoSimpleAsync().GetAwaiter().GetResult();
        }

        public static List<DiskUsage> GetDiskUsage()
        {
            return GetDiskUsageAsync().GetAwaiter().GetResult();
        }

        // Optimized version with parallel performance counter initialization
        public static CpuUsageInfo GetCpuUsageOptimized()
        {
            return GetCpuUsageAsync().GetAwaiter().GetResult();
        }
    }
}
