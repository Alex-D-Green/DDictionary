using System;
using System.Globalization;
using System.Windows.Data;

using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Converters
{
    public sealed class TranslationConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.Assert(value is Translation && targetType == typeof(string));

            return ConvertToString((Translation)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string ConvertToString(Translation val)
        {
            if(val is null)
                throw new ArgumentNullException(nameof(val));


            string ret = val.Text;

            if(val.Part != PartOfSpeech.Unknown)
                ret = String.Format("{0} ({1})", ret, val.Part.ToShortString());

            return ret;
        }
    }
}
