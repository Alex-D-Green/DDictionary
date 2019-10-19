using System;
using System.Globalization;
using System.Windows.Data;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Converters
{
    public sealed class WordGroupConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //https://stackoverflow.com/questions/3978937/how-to-pass-an-integer-as-converterparameter

            System.Diagnostics.Debug.Assert(value is WordGroup && targetType == typeof(string));
            System.Diagnostics.Debug.Assert(parameter is null || parameter is bool?);

            return ((bool?)parameter == true) ? ((WordGroup)value).ToFullStr() 
                                              : ((WordGroup)value).ToGradeStr();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
