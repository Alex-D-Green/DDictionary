using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for SettingsDlg.xaml
    /// </summary>
    public partial class SettingsDlg: Window
    {
        //Predefined values of GUI scale coefficient
        private readonly List<double> guiScales = 
            new List<double> { 0.5, 0.75, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3 };


        public SettingsDlg()
        {
            InitializeComponent();
            ApplyGUIScale();

            //Main settings
            showPopupChk.IsChecked = Properties.Settings.Default.ShowInfoPopup;
            autoPlayChk.IsChecked = Properties.Settings.Default.AutoplaySound;
            saveContextChk.IsChecked = Properties.Settings.Default.SaveContext;

            int idx = guiScales.IndexOf(Properties.Settings.Default.MainWindowScale);
            if(idx != -1)
                mainScaleCoefCBox.SelectedIndex = idx;
            
            idx = guiScales.IndexOf(Properties.Settings.Default.DialogsScale);
            if(idx != -1)
                dlgScaleCoefCBox.SelectedIndex = idx;
            
            idx = guiScales.IndexOf(Properties.Settings.Default.PopupScale);
            if(idx != -1)
                popupScaleCoefCBox.SelectedIndex = idx;


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
            Properties.Settings.Default.MainWindowScale = guiScales[mainScaleCoefCBox.SelectedIndex];
            Properties.Settings.Default.DialogsScale = guiScales[dlgScaleCoefCBox.SelectedIndex];
            Properties.Settings.Default.PopupScale = guiScales[popupScaleCoefCBox.SelectedIndex];

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

        private void ApplyGUIScale()
        {
            double guiScale = Properties.Settings.Default.DialogsScale;

            mainWindowGrid.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;
        }
    }
}
