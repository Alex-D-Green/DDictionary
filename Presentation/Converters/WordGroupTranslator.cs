using DDictionary.Domain.Entities;

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
                case WordGroup.A_DefinitelyKnown: return "(A) Definitely known";
                case WordGroup.B_WellKnown: return "(B) Well known";
                case WordGroup.C_KindaKnown: return "(C) Kinda known";
                case WordGroup.D_NeedToMemorize: return "(D) Need to memorize";
                case WordGroup.E_TotallyUnknown: return "(E) Totally unknown";

                default:
                    return group.ToString();
            }
        }
    }
}
