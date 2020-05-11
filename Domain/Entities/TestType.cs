
namespace DDictionary.Domain.Entities
{
    //              meaning  spelling  hearing
    // tr-word         *
    // word-tr         *
    // constr          *        *
    // listening                *         *
    // sprint          *

    public enum TestType
    {
        TranslationWord,
        WordTranslation,
        WordConstructor,
        Listening,
        Sprint
    }
}
