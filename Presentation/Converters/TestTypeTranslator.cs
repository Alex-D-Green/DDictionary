using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Converters
{
    public static class TestTypeTranslator
    {
        public static string ToFullString(this TestType type)
        {
            switch(type)
            {
                case TestType.Listening:       return PrgResources.TestTypeListening;
                case TestType.Sprint:          return PrgResources.TestTypeSprint;
                case TestType.TranslationWord: return PrgResources.TestTypeTranslationWord;
                case TestType.WordConstructor: return PrgResources.TestTypeWordConstructor;
                case TestType.WordTranslation: return PrgResources.TestTypeWordTranslation;

                default:
                    return type.ToString();
            }
        }
    }
}
