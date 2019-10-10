using System;
using System.Globalization;
using System.Windows.Data;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Converters
{
    public class JustWordDTOConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.Assert(value is JustWordDTO && targetType == typeof(string));

            return ((JustWordDTO)value).Word;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
