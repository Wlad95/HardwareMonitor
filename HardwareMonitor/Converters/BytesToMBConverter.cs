using System;
using System.Globalization;
using System.Windows.Data;

namespace HardwareMonitor.Converters
{
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "0";
            if (long.TryParse(value.ToString(), out long bytes))
            {
                double mb = bytes / 1024.0 / 1024.0;
                return mb.ToString("F0", culture);
            }
            if (double.TryParse(value.ToString(), out double d))
            {
                double mb = d / 1024.0 / 1024.0;
                return mb.ToString("F0", culture);
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}