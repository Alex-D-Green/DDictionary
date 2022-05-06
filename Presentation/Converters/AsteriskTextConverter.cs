using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using DDictionary.Domain.Entities;

namespace DDictionary.Presentation.Converters
{
    public sealed class AsteriskTextConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AsteriskType? input = null;
            
            if(value is AsteriskType type)
                input = type;

            switch(input)
            {
                case null:
                case AsteriskType.None:
                    return DependencyProperty.UnsetValue;

                case AsteriskType.AllTypes:
                case AsteriskType.Meaning:
                case AsteriskType.Spelling:
                case AsteriskType.Listening:
                    return input.Value.ToShortStr();

                default:
                    return "✶";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
