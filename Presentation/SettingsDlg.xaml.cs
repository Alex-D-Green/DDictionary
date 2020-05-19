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

            //Main settings
            showPopupChk.IsChecked = Properties.Settings.Default.ShowInfoPopup;
            autoPlayChk.IsChecked = Properties.Settings.Default.AutoplaySound;
            saveContextChk.IsChecked = Properties.Settings.Default.SaveContext;

            //Translation-Word
            trwHideAnswersChk.IsChecked = Properties.Settings.Default.TranslationWordHideAnswers;

            //Word-Translation
            wtrHideAnswersChk.IsChecked = Properties.Settings.Default.WordTranslationHideAnswers;

            //Constructor
            ctorHideLettersChk.IsChecked = Properties.Settings.Default.ConstructorHideLetters;

            //Listening
            listenSecondChanceChk.IsChecked = Properties.Settings.Default.ListeningAllowOneMistake;

            //Sprint
            sprintTimeCBox.SelectedIndex = (Properties.Settings.Default.SprintSecondsOnAnswer - 2); //2, 3, 4 sec
        }


        private void OnSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //Main settings
            Properties.Settings.Default.ShowInfoPopup = (showPopupChk.IsChecked == true);
            Properties.Settings.Default.AutoplaySound = (autoPlayChk.IsChecked == true);
            Properties.Settings.Default.SaveContext = (saveContextChk.IsChecked == true);

            //Translation-Word
            Properties.Settings.Default.TranslationWordHideAnswers = (trwHideAnswersChk.IsChecked == true);

            //Word-Translation
            Properties.Settings.Default.WordTranslationHideAnswers = (wtrHideAnswersChk.IsChecked == true);

            //Constructor
            Properties.Settings.Default.ConstructorHideLetters = (ctorHideLettersChk.IsChecked == true);

            //Listening
            Properties.Settings.Default.ListeningAllowOneMistake = (listenSecondChanceChk.IsChecked == true);

            //Sprint
            Properties.Settings.Default.SprintSecondsOnAnswer = (sprintTimeCBox.SelectedIndex + 2); //2, 3, 4 sec


            Properties.Settings.Default.Save();

            Close();
        }
    }
}
