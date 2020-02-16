using DDictionary.DAL;
using DDictionary.Domain;


namespace DDictionary
{
    /// <summary>
    /// The main composition root - class to resolve dependency injection.
    /// </summary>
    public static class CompositionRoot
    {
        /// <summary>The data provider.</summary>
        public static IDBFacade DBFacade { get; } = new SQLiteStorage(); //new InMemoryMockStorage();
    }
}
