using System.Windows.Input;


namespace DDictionary.Presentation
{
    //https://www.wpf-tutorial.com/commands/implementing-custom-commands/

    /// <summary>
    /// User interface commands description.
    /// </summary>
    public static class UICommands
    {
        public static readonly RoutedUICommand ExitCommand = new RoutedUICommand(
            "Exit", "Exit", typeof(UICommands), 
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });

        public static readonly RoutedUICommand SettingsCommand = new RoutedUICommand(
            "Settings", "Settings", typeof(UICommands));

        public static readonly RoutedUICommand GoToSiteCommand = new RoutedUICommand(
            "Goto site", "Goto site", typeof(UICommands));

        public static readonly RoutedUICommand OnlineHelpCommand = new RoutedUICommand(
            "Online help", "Online help", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F1, ModifierKeys.None) });

        public static readonly RoutedUICommand AboutCommand = new RoutedUICommand(
            "About program", "About program", typeof(UICommands));

        public static readonly RoutedUICommand CreateMultirelationCommand = new RoutedUICommand(
            "Create multirelation", "Create multirelation", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.M, ModifierKeys.Control) });

        public static readonly RoutedUICommand SelectAllCommand = new RoutedUICommand(
            "Select all", "Select all", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Control) });

        public static readonly RoutedUICommand ClearFilterCommand = new RoutedUICommand(
            "Clear filter", "Clear filter", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F, ModifierKeys.Control) });

        public static readonly RoutedUICommand ClearSortingCommand = new RoutedUICommand(
            "Clear sorting", "Clear sorting", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F, ModifierKeys.Alt|ModifierKeys.Control) });

        public static readonly RoutedUICommand AddWordCommand = new RoutedUICommand(
            "Add word", "Add word", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Control) });

        public static readonly RoutedUICommand DeleteWordsCommand = new RoutedUICommand(
            "Delete selected words", "Delete selected words", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.Delete) });

        public static readonly RoutedUICommand EditRelationsCommand = new RoutedUICommand(
            "Edit word relations", "Edit word relations", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.R, ModifierKeys.Control) });

        public static readonly RoutedUICommand CreateNewDBCommand = new RoutedUICommand(
            "Create new data base", "Create new data base", typeof(UICommands));

        public static readonly RoutedUICommand OpenDBCommand = new RoutedUICommand(
            "Open data base", "Open data base", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.O, ModifierKeys.Control) });

        public static readonly RoutedUICommand ExportToCSVCommand = new RoutedUICommand(
            "Export to CSV", "Export to CSV", typeof(UICommands));

        public static readonly RoutedUICommand ExportToHtmlCommand = new RoutedUICommand(
            "Export to Html", "Export to Html", typeof(UICommands));

        public static readonly RoutedUICommand ImportFromCSVCommand = new RoutedUICommand(
            "Import from CSV", "Import from CSV", typeof(UICommands));

        public static readonly RoutedUICommand MoveWordsToAGroupCommand = new RoutedUICommand(
            "Move selected words to A group", "Move selected words to A group", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand MoveWordsToBGroupCommand = new RoutedUICommand(
            "Move selected words to B group", "Move selected words to B group", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand MoveWordsToCGroupCommand = new RoutedUICommand(
            "Move selected words to C group", "Move selected words to C group", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand MoveWordsToDGroupCommand = new RoutedUICommand(
            "Move selected words to D group", "Move selected words to D group", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand MoveWordsToEGroupCommand = new RoutedUICommand(
            "Move selected words to E group", "Move selected words to E group", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand TranslationWordTestCommand = new RoutedUICommand(
            "Translation - Word test", "Translation - Word test", typeof(UICommands), 
            new InputGestureCollection() { new KeyGesture(Key.F1, ModifierKeys.Control) });

        public static readonly RoutedUICommand WordTranslationTestCommand = new RoutedUICommand(
            "Word - Translation test", "Word - Translation test", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F2, ModifierKeys.Control) });

        public static readonly RoutedUICommand WordsConstructorTestCommand = new RoutedUICommand(
            "Words constructor test", "Words constructor test", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F3, ModifierKeys.Control) });

        public static readonly RoutedUICommand ListeningTestCommand = new RoutedUICommand(
            "Listening test", "Listening test", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Control) });

        public static readonly RoutedUICommand SprintTestCommand = new RoutedUICommand(
            "Sprint test", "Sprint test", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F5, ModifierKeys.Control) });

        public static readonly RoutedUICommand TestsStatisticsCommand = new RoutedUICommand(
            "Tests statistics", "Tests statistics", typeof(UICommands),
            new InputGestureCollection() { new KeyGesture(Key.F12, ModifierKeys.Control) });
    }
}
