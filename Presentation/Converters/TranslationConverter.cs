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

        /// <seealso cref="DDictionary.Presentation.Converters.TranslationConverter.Parse"/>
        public static string ConvertToString(Translation val)
        {
            if(val is null)
                throw new ArgumentNullException(nameof(val));


            string ret = val.Text;

            if(val.Part != PartOfSpeech.Unknown)
                ret = String.Format("{0} ({1})", ret, val.Part.ToShortString());

            return ret;
        }

        /// <summary>
        /// Parses the string trying to find out the part of the speech as if it was written by
        /// <see cref="DDictionary.Presentation.Converters.TranslationConverter.ConvertToString"/> method.
        /// </summary>
        public static Translation Parse(string translation)
        {
            if(translation is null)
                throw new ArgumentNullException(nameof(translation));


            foreach(PartOfSpeech p in Enum.GetValues(typeof(PartOfSpeech)))
            {
                if(translation.EndsWith($" ({p.ToShortString()})"))
                {
                    return new Translation {
                        Text = translation.Substring(0, translation.LastIndexOf('(')).TrimEnd(),
                        Part = p
                    };
                }
            }

            return new Translation { Text = translation, Part = PartOfSpeech.Unknown };
        }
    }
}
