using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Threading;

namespace SysMetrix
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            { 
                await DisplaySingleReadingAsync(false);

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task DisplaySingleReadingAsync(bool compact = false)
        {
            var cpuInfo = await SystemMonitor.GetCpuUsageAsync();

            string json = compact
                ? JsonHelper.SerializeToCompactJson(cpuInfo)
                : JsonHelper.SerializeToJson(cpuInfo);

            Console.WriteLine(json);
        } 
    }
}
