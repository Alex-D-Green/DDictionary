using System.Windows;
using System.Windows.Threading;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App: Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;

            Shutdown();
        }
    }
}
