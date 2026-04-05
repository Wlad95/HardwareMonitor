using System;
using System.Windows;
using HardwareMonitor.ViewModels;

namespace HardwareMonitor.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик кнопки справки, привязанный в XAML (Click="HelpButton_Click")
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string message =
                "Hardware Monitor\n\n" +
                "Кнопки:\n" +
                "- Обновить — обновление данных\n" +
                "- Экспорт данных — сохранить текстовый отчёт\n" +
                "- Экспорт графика — сохранить изображение графика CPU\n\n" +
                "Двойной клик по процессу или кнопка 'Снять задачу' — завершение процесса.";
            MessageBox.Show(message, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Гарантированно завершить фоновые операции VM при закрытии окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is MainViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
