using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for TranslationsEditDlg.xaml
    /// </summary>
    public partial class TranslationsEditDlg: Window
    {
        /// <summary>Result the text of translation and the part of the speech for it.</summary>
        public (string translation, PartOfSpeech partOfSpeech)? Translation { get; private set; }


        public TranslationsEditDlg()
        {
            InitializeComponent();

            PreviewKeyDown += (s, e) =>
            { //To close dialog by Escape key (cuz this dialog has no Cancel button)
                if(e.Key == Key.Escape)
                    Close();
            };

            //ComboBox with parts of the speech initialization
            foreach(PartOfSpeech part in Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>())
                partOfSpeechCBox.Items.Add(new CheckBoxItem<PartOfSpeech> { Text = part.ToFullString(), ItemValue = part });

            partOfSpeechCBox.SelectedIndex = 0;
            acceptBtn.IsEnabled = false;

            translationEdit.Focus();
        }

        /// <summary>
        /// Any data was changed.
        /// </summary>
        private void OnSomeDataWasChanged(object sender, EventArgs e)
        {
            ChangesHaveBeenMade();
        }

        /// <summary>
        /// Some changes have been made in the data.
        /// </summary>
        private void ChangesHaveBeenMade()
        {
            if(acceptBtn != null)
                acceptBtn.IsEnabled = !String.IsNullOrWhiteSpace(translationEdit?.Text);
        }

        /// <summary>
        /// Handle Accept button click.
        /// </summary>
        private void OnAcceptBtn_Click(object sender, RoutedEventArgs e)
        {
            Translation = (translationEdit.Text, ((CheckBoxItem<PartOfSpeech>)partOfSpeechCBox.SelectedItem).ItemValue);

            DialogResult = true;
            Close();
        }
    }
}
