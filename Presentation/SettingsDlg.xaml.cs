using System.Windows;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for SettingsDlg.xaml
    /// </summary>
    public partial class SettingsDlg: Window
    {
        public SettingsDlg()
        {
            InitializeComponent();

            showPopupChk.IsChecked = Properties.Settings.Default.ShowInfoPopup;
            autoPlayChk.IsChecked = Properties.Settings.Default.AutoplaySound;
            saveContextChk.IsChecked = Properties.Settings.Default.SaveContext;
        }


        private void OnSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowInfoPopup = showPopupChk.IsChecked == true;
            Properties.Settings.Default.AutoplaySound = autoPlayChk.IsChecked == true;
            Properties.Settings.Default.SaveContext = saveContextChk.IsChecked == true;

            Properties.Settings.Default.Save();

            Close();
        }
    }
}
