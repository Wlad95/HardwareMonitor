using System;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения информации об оперативной памяти через WMI
    /// </summary>
    public class MemoryMonitor
    {
        /// <summary>
        /// Получает информацию о памяти системы
        /// </summary>
        /// <returns>Объект MemoryInfo с данными о памяти</returns>
        public MemoryInfo GetMemoryInfo()
        {
            MemoryInfo memoryInfo = new MemoryInfo();

            try
            {
                string osQuery = "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem";
                ManagementObjectSearcher osSearcher = new ManagementObjectSearcher(osQuery);

                using (osSearcher)
                {
                    ManagementObjectCollection osResults = osSearcher.Get();
                    foreach (ManagementObject operatingSystem in osResults)
                    {
                        // Конвертируем из КБ в байты (* 1024)
                        memoryInfo.TotalMemoryBytes = Convert.ToInt64(operatingSystem["TotalVisibleMemorySize"] ?? 0) * 1024;
                        memoryInfo.AvailableMemoryBytes = Convert.ToInt64(operatingSystem["FreePhysicalMemory"] ?? 0) * 1024;
                        break;
                    }
                }

                string memQuery = "SELECT Manufacturer, Capacity, Speed FROM Win32_PhysicalMemory";
                ManagementObjectSearcher memSearcher = new ManagementObjectSearcher(memQuery);

                using (memSearcher)
                {
                    ManagementObjectCollection memResults = memSearcher.Get();
                    foreach (ManagementObject memoryModule in memResults)
                    {
                        MemoryModule module = new MemoryModule
                        {
                            Manufacturer = memoryModule["Manufacturer"]?.ToString() ?? "Неизвестно",
                            CapacityBytes = Convert.ToInt64(memoryModule["Capacity"] ?? 0),
                            SpeedMHz = Convert.ToInt32(memoryModule["Speed"] ?? 0)
                        };
                        memoryInfo.Modules.Add(module);
                    }
                }
            }
            catch (ManagementException ex)
            {
                throw new Exception($"Ошибка при получении информации о памяти: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("Недостаточно прав для доступа к WMI.", ex);
            }

            return memoryInfo;
        }
    }
}

