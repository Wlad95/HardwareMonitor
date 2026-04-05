using HardwareMonitor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HardwareMonitor.Services
{
    public class ProcessMonitor
    {
        private Dictionary<int, TimeSpan> _previousCpuTimes = new Dictionary<int, TimeSpan>();
        private DateTime _lastCheckTime = DateTime.UtcNow;

        public List<ProcessInfoModel> GetProcesses()
        {
            List<ProcessInfoModel> processes = new List<ProcessInfoModel>();

            DateTime now = DateTime.UtcNow;
            double elapsedMs = (now - _lastCheckTime).TotalMilliseconds;

            if (elapsedMs <= 0)
                elapsedMs = 1;

            _lastCheckTime = now;

            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    TimeSpan currentCpuTime = proc.TotalProcessorTime;
                    double cpuUsage = 0;

                    if (_previousCpuTimes.TryGetValue(proc.Id, out TimeSpan previousCpuTime))
                    {
                        double cpuUsedMs = (currentCpuTime - previousCpuTime).TotalMilliseconds;

                        cpuUsage = cpuUsedMs /
                                   (Environment.ProcessorCount * elapsedMs) * 100;
                    }

                    _previousCpuTimes[proc.Id] = currentCpuTime;

                    processes.Add(new ProcessInfoModel
                    {
                        Name = proc.ProcessName,
                        Status = proc.Responding ? "Запущен" : "Не отвечает",
                        CpuUsage = Math.Round(cpuUsage, 1),
                        MemoryBytes = proc.WorkingSet64
                    });
                }
                catch
                {
                    continue;
                }
            }

            return processes
                .OrderByDescending(p => p.CpuUsage)
                .ToList();
        }
    }
}
