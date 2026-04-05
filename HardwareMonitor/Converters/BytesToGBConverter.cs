using System;
using System.Globalization;
using System.Windows.Data;

namespace HardwareMonitor.Converters
{
    public class BytesToGBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "0";
            if (long.TryParse(value.ToString(), out long bytes))
            {
                double gb = bytes / 1024.0 / 1024.0 / 1024.0;
                return gb.ToString("F1", culture);
            }
            if (double.TryParse(value.ToString(), out double d))
            {
                double gb = d / 1024.0 / 1024.0 / 1024.0;
                return gb.ToString("F1", culture);
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}