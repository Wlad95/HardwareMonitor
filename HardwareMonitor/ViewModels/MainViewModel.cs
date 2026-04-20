using HardwareMonitor.Models;
using HardwareMonitor.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HardwareMonitor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly CpuMonitor _cpuMonitor;
        private readonly MemoryMonitor _memoryMonitor;
        private readonly DiskMonitor _diskMonitor;
        private readonly ProcessMonitor _processMonitor;

        private DispatcherTimer _timer;
        private const int MaxPoints = 60;

        public ObservableCollection<double> CpuHistory { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<ProcessInfoModel> Processes { get; } = new ObservableCollection<ProcessInfoModel>();

        private CpuInfo _cpuInfo;
        public CpuInfo CpuInfo
        {
            get => _cpuInfo;
            set => SetProperty(ref _cpuInfo, value);
        }

        private MemoryInfo _memoryInfo;
        public MemoryInfo MemoryInfo
        {
            get => _memoryInfo;
            set => SetProperty(ref _memoryInfo, value);
        }

        private DiskInfo _diskInfo;
        public DiskInfo DiskInfo
        {
            get => _diskInfo;
            set => SetProperty(ref _diskInfo, value);
        }

        private ProcessInfoModel _selectedProcess;
        public ProcessInfoModel SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ExportCpuGraphCommand { get; }
        public ICommand KillProcessCommand { get; }

        public MainViewModel()
        {
            _cpuMonitor = new CpuMonitor();
            _memoryMonitor = new MemoryMonitor();
            _diskMonitor = new DiskMonitor();
            _processMonitor = new ProcessMonitor();

            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            ExportCommand = new RelayCommand(ExportToFile);
            ExportCpuGraphCommand = new RelayCommand(ExportCpuGraph);
            KillProcessCommand = new RelayCommand(KillSelectedProcess);

            // Сразу обновляем данные
            _ = RefreshDataAsync();

            // Таймер обновления каждую секунду
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (s, e) =>
            {
                await RefreshDataAsync();
                UpdateCpuHistory();
                DrawCpuGraph();
            };
            _timer.Start();
        }

        // Обновление всех данных
        public async Task RefreshDataAsync()
        {
            CpuInfo = await Task.Run(() => _cpuMonitor.GetCpuInfo());
            MemoryInfo = await Task.Run(() => _memoryMonitor.GetMemoryInfo());
            DiskInfo = await Task.Run(() => _diskMonitor.GetDiskInfo());

            // Запоминаем имя выбранного процесса ДО обновления
            string selectedName = SelectedProcess?.Name;

            // Получаем новые данные процессов
            List<ProcessInfoModel> newProcesses = await Task.Run(() =>
            {
                List<ProcessInfoModel> list = _processMonitor.GetProcesses();
                foreach (ProcessInfoModel p in list)
                {
                    p.MemoryBytes = p.MemoryBytes / 1024 / 1024;
                }
                return list;
            });

            // Обновляем существующие элементы вместо замены списка
            foreach (ProcessInfoModel newProc in newProcesses)
            {
                bool found = false;
                foreach (ProcessInfoModel existing in Processes)
                {
                    if (existing.Name == newProc.Name)
                    {
                        // Обновляем данные прямо в объекте - выбор не сбросится!
                        existing.CpuUsage = newProc.CpuUsage;
                        existing.MemoryBytes = newProc.MemoryBytes;
                        existing.Status = newProc.Status;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // Новый процесс - добавляем
                    Processes.Add(newProc);
                }
            }

            // Удаляем завершённые процессы
            for (int i = Processes.Count - 1; i >= 0; i--)
            {
                bool stillExists = false;
                foreach (ProcessInfoModel newProc in newProcesses)
                {
                    if (newProc.Name == Processes[i].Name)
                    {
                        stillExists = true;
                        break;
                    }
                }
                if (!stillExists)
                {
                    Processes.RemoveAt(i);
                }
            }

            // Восстанавливаем выбор если вдруг сбросился
            if (selectedName != null && SelectedProcess == null)
            {
                foreach (ProcessInfoModel p in Processes)
                {
                    if (p.Name == selectedName)
                    {
                        SelectedProcess = p;
                        break;
                    }
                }
            }

            OnPropertyChanged(nameof(CpuInfo));
            OnPropertyChanged(nameof(MemoryInfo));
            OnPropertyChanged(nameof(DiskInfo));
        }

        // Обновление истории CPU
        private void UpdateCpuHistory()
        {
            if (CpuInfo == null)
            {
                return;
            }
            CpuHistory.Add(CpuInfo.LoadPercentage);
            if (CpuHistory.Count > MaxPoints)
            {
                CpuHistory.RemoveAt(0);
            }
        }

        // Рисуем график CPU
        private void DrawCpuGraph()
        {
            System.Windows.Controls.Canvas canvas = Application.Current.MainWindow?.FindName("CpuGraphCanvas") as System.Windows.Controls.Canvas;
            if (canvas == null || CpuHistory.Count < 2)
            {
                return;
            }

            canvas.Children.Clear();
            double widthStep = canvas.ActualWidth / MaxPoints;
            double height = canvas.ActualHeight;

            Polyline line = new Polyline { Stroke = Brushes.LimeGreen, StrokeThickness = 2 };

            for (int i = 0; i < CpuHistory.Count; i++)
            {
                double x = i * widthStep;
                double y = height - (CpuHistory[i] / 100.0 * height);
                line.Points.Add(new System.Windows.Point(x, y));
            }

            canvas.Children.Add(line);
        }

        // Снять задачу (выбранный процесс)
        private void KillSelectedProcess()
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Выберите процесс из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string processName = SelectedProcess.Name;

            MessageBoxResult result = MessageBox.Show(
                $"Завершить процесс \"{processName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                foreach (Process proc in Process.GetProcessesByName(processName))
                {
                    proc.Kill();
                }

                SelectedProcess = null;
                MessageBox.Show($"Процесс \"{processName}\" завершён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось завершить процесс: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Снять задачу по двойному клику
        public void KillProcessOnDoubleClick(ProcessInfoModel proc)
        {
            if (proc == null)
            {
                return;
            }
            SelectedProcess = proc;
            KillSelectedProcess();
        }

        // Экспорт информации о компьютере
        private void ExportToFile()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                // Было: Filter = "Text files (*.txt)|*.txt", FileName = "ComputerInfo.txt"
                Filter = "Text files (*.txt)|*.txt|JSON files (*.json)|*.json|CSV files (*.csv)|*.csv",
                FileName = "ComputerInfo"
            };

            if (dialog.ShowDialog() != true) return;

            StringBuilder sb = new StringBuilder();
            string ext = System.IO.Path.GetExtension(dialog.FileName).ToLower();

            if (ext == ".json")
            {
                sb.AppendLine("{");
                sb.AppendLine($"  \"date\": \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",");
                sb.AppendLine($"  \"cpu\": {{ \"name\": \"{CpuInfo?.Name}\", \"cores\": {CpuInfo?.CoreCount}, \"threads\": {CpuInfo?.ThreadCount}, \"load\": {CpuInfo?.LoadPercentage:F1} }},");
                sb.AppendLine($"  \"memory\": {{ \"totalMB\": {MemoryInfo?.TotalMemoryBytes / 1024 / 1024}, \"availableMB\": {MemoryInfo?.AvailableMemoryBytes / 1024 / 1024}, \"usage\": {MemoryInfo?.UsagePercentage:F1} }},");
                sb.AppendLine("  \"disks\": [");
                if (DiskInfo?.LogicalDisks != null)
                    for (int i = 0; i < DiskInfo.LogicalDisks.Count; i++)
                    {
                        var d = DiskInfo.LogicalDisks[i];
                        string comma = i < DiskInfo.LogicalDisks.Count - 1 ? "," : "";
                        sb.AppendLine($"    {{ \"drive\": \"{d.DriveLetter}\", \"totalGB\": {d.TotalSizeBytes / 1024 / 1024 / 1024}, \"freeGB\": {d.FreeSizeBytes / 1024 / 1024 / 1024}, \"usage\": {d.UsagePercent:F1}, \"fs\": \"{d.FileSystem}\" }}{comma}");
                    }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
            }
            else if (ext == ".csv")
            {
                sb.AppendLine("Параметр;Значение");
                sb.AppendLine($"CPU;{CpuInfo?.Name}");
                sb.AppendLine($"Ядер;{CpuInfo?.CoreCount}");
                sb.AppendLine($"Потоков;{CpuInfo?.ThreadCount}");
                sb.AppendLine($"Загрузка (%);{CpuInfo?.LoadPercentage:F1}");
                sb.AppendLine($"Память всего (МБ);{MemoryInfo?.TotalMemoryBytes / 1024 / 1024}");
                sb.AppendLine($"Память доступно (МБ);{MemoryInfo?.AvailableMemoryBytes / 1024 / 1024}");
                sb.AppendLine($"Память использовано (%);{MemoryInfo?.UsagePercentage:F1}");
                sb.AppendLine();
                sb.AppendLine("Диск;Всего (ГБ);Свободно (ГБ);Занято (%);ФС");
                if (DiskInfo?.LogicalDisks != null)
                    foreach (var d in DiskInfo.LogicalDisks)
                        sb.AppendLine($"{d.DriveLetter};{d.TotalSizeBytes / 1024 / 1024 / 1024};{d.FreeSizeBytes / 1024 / 1024 / 1024};{d.UsagePercent:F1};{d.FileSystem}");
            }
            else
            {
                // Оригинальный TXT — ваш старый код без изменений
                sb.AppendLine("=== ИНФОРМАЦИЯ О КОМПЬЮТЕРЕ ===\n");
                sb.AppendLine("---- ПРОЦЕССОР ----");
                sb.AppendLine($"Название: {CpuInfo?.Name}");
                sb.AppendLine($"Производитель: {CpuInfo?.Manufacturer}");
                sb.AppendLine($"Архитектура: {CpuInfo?.Architecture}");
                sb.AppendLine($"Ядер: {CpuInfo?.CoreCount}");
                sb.AppendLine($"Потоков: {CpuInfo?.ThreadCount}");
                sb.AppendLine($"Загрузка: {CpuInfo?.LoadPercentage:F1}%\n");
                sb.AppendLine("---- ПАМЯТЬ ----");
                sb.AppendLine($"Всего (МБ): {MemoryInfo?.TotalMemoryBytes / 1024 / 1024}");
                sb.AppendLine($"Доступно (МБ): {MemoryInfo?.AvailableMemoryBytes / 1024 / 1024}");
                sb.AppendLine($"Использовано: {MemoryInfo?.UsagePercentage:F1}%\n");
                sb.AppendLine("---- ДИСКИ ----");
                if (DiskInfo?.LogicalDisks != null)
                    foreach (var disk in DiskInfo.LogicalDisks)
                        sb.AppendLine($"{disk.DriveLetter} | Всего: {disk.TotalSizeBytes / 1024 / 1024 / 1024} ГБ | Свободно: {disk.FreeSizeBytes / 1024 / 1024 / 1024} ГБ | Использование: {disk.UsagePercent:F1}% | FS: {disk.FileSystem}");
                sb.AppendLine("\n---- ПРОЦЕССЫ ----");
                if (Processes != null)
                    foreach (var proc in Processes)
                        sb.AppendLine($"{proc.Name} | CPU: {proc.CpuUsage:F1}% | Память: {proc.MemoryMB:F1} МБ | Статус: {proc.Status}");
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        }

        // Экспорт графика CPU
        private void ExportCpuGraph()
        {
            System.Windows.Controls.Canvas canvas = Application.Current.MainWindow?.FindName("CpuGraphCanvas") as System.Windows.Controls.Canvas;
            if (canvas == null)
            {
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog { Filter = "PNG Image (*.png)|*.png", FileName = "CpuGraph.png" };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            System.Windows.Media.Imaging.RenderTargetBitmap rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)canvas.ActualWidth, (int)canvas.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(canvas);

            System.Windows.Media.Imaging.PngBitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

            FileStream stream = new FileStream(dialog.FileName, FileMode.Create);
            encoder.Save(stream);
            stream.Close();
        }

        // Остановка таймера при закрытии
        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
        }
    }
}
