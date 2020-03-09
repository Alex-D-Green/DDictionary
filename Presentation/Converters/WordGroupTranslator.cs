using System;

using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Converters
{
    public static class WordGroupTranslator
    {
        public static string ToGradeStr(this WordGroup group)
        {
            switch(group)
            {
                case WordGroup.A_DefinitelyKnown: return "A";
                case WordGroup.B_WellKnown: return "B";
                case WordGroup.C_KindaKnown: return "C";
                case WordGroup.D_NeedToMemorize: return "D";
                case WordGroup.E_TotallyUnknown: return "E";

                default:
                    return group.ToString();
            }
        }

        public static string ToFullStr(this WordGroup group)
        {
            switch(group)
            {
                case WordGroup.A_DefinitelyKnown: return $"({group.ToGradeStr()}) {PrgResources.WGDefinitelyKnown}";
                case WordGroup.B_WellKnown:       return $"({group.ToGradeStr()}) {PrgResources.WGWellKnown}";
                case WordGroup.C_KindaKnown:      return $"({group.ToGradeStr()}) {PrgResources.WGKindOfKnown}";
                case WordGroup.D_NeedToMemorize:  return $"({group.ToGradeStr()}) {PrgResources.WGNeedToMemorize}";
                case WordGroup.E_TotallyUnknown:  return $"({group.ToGradeStr()}) {PrgResources.WGTotallyUnknown}";

                default:
                    return group.ToString();
            }
        }

        /// <exception cref="System.ArgumentOutOfRangeException" />
        public static WordGroup FromGradeStr(string grade)
        {
            if(String.Equals(grade, WordGroup.A_DefinitelyKnown.ToGradeStr(), StringComparison.OrdinalIgnoreCase))
                return WordGroup.A_DefinitelyKnown;
            else if(String.Equals(grade, WordGroup.B_WellKnown.ToGradeStr(), StringComparison.OrdinalIgnoreCase))
                return WordGroup.B_WellKnown;
            else if(String.Equals(grade, WordGroup.C_KindaKnown.ToGradeStr(), StringComparison.OrdinalIgnoreCase))
                return WordGroup.C_KindaKnown;
            else if(String.Equals(grade, WordGroup.D_NeedToMemorize.ToGradeStr(), StringComparison.OrdinalIgnoreCase))
                return WordGroup.D_NeedToMemorize;
            else if(String.Equals(grade, WordGroup.E_TotallyUnknown.ToGradeStr(), StringComparison.OrdinalIgnoreCase))
                return WordGroup.E_TotallyUnknown;
            else
                throw new ArgumentOutOfRangeException(nameof(grade), grade, "Unknown value.");
        }
    }
}
