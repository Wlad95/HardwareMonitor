using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации об оперативной памяти
    /// </summary>
    public class MemoryInfo
    {
        // Общий объем оперативной памяти в байтах
        public long TotalMemoryBytes { get; set; }

        // Доступный объем памяти в байтах
        public long AvailableMemoryBytes { get; set; }

        // Список установленных модулей памяти
        public List<MemoryModule> Modules { get; set; }

        /// <summary>
        /// Вычисляемое свойство - процент использования памяти
        /// </summary>
        public double UsagePercentage
        {
            get
            {
                if (TotalMemoryBytes <= 0)
                {
                    return 0.0;
                }
                return (double)(TotalMemoryBytes - AvailableMemoryBytes) / TotalMemoryBytes * 100.0;
            }
            
        }

        /// <summary>
        /// Конструктор - инициализирует коллекцию модулей
        /// </summary>
        public MemoryInfo()
        {
            Modules = new List<MemoryModule>();
        }
    }

    /// <summary>
    /// Класс для хранения информации об отдельном модуле памяти (планке RAM)
    /// </summary>
    public class MemoryModule
    {
        // Производитель модуля памяти
        public string Manufacturer { get; set; }

        // Объем модуля в байтах
        public long CapacityBytes { get; set; }

        // Частота модуля в МГц
        public int SpeedMHz { get; set; }

        public MemoryModule()
        {
            Manufacturer = "Неизвестно";
        }
    }
}

