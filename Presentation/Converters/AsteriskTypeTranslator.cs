using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Converters
{
    public static class AsteriskTypeTranslator
    {
        public static string ToFullStr(this AsteriskType type)
        {
            switch(type)
            {
                case AsteriskType.None: return PrgResources.AsteriskTypeNone;
                case AsteriskType.AllTypes: return PrgResources.AsteriskTypeAllTypes;
                case AsteriskType.Meaning: return PrgResources.AsteriskTypeMeaning;
                case AsteriskType.Spelling: return PrgResources.AsteriskTypeSpelling;
                case AsteriskType.Listening: return PrgResources.AsteriskTypeListening;

                default:
                    return type.ToString();
            }
        }

        public static string ToShortStr(this AsteriskType type)
        {
            switch(type)
            {
                case AsteriskType.None: return "";
                case AsteriskType.AllTypes: return "A";
                case AsteriskType.Meaning: return "M";
                case AsteriskType.Spelling: return "S";
                case AsteriskType.Listening: return "L";

                default:
                    return type.ToString();
            }
        }
    }
}
