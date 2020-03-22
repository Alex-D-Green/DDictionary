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
    }
}
