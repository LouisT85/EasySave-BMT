using Avalonia.Data.Converters;
using easySave_BMT.Model_;
using easySave_BMT.Resources_;
using System;
using System.Globalization;

namespace easySave_BMT.Avalonia.Converters
{
    public class BackupTypeToLocalizedStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is BackupType type)
            {
                return type == BackupType.FULL
                    ? ResourceManager.GetString("FullBackup")
                    : ResourceManager.GetString("DifferentialBackup");
            }

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
