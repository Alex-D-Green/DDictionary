using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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


        public TranslationsEditDlg(string translation = null, PartOfSpeech part = PartOfSpeech.Unknown)
        {
            InitializeComponent();
            ApplyGUIScale();

            PreviewKeyDown += (s, e) =>
            { //To close dialog by Escape key (cuz this dialog has no Cancel button)
                if(e.Key == Key.Escape)
                    Close();
            };

            //ComboBox with parts of the speech initialization
            foreach(PartOfSpeech pt in Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>())
                partOfSpeechCBox.Items.Add(new CheckBoxItem<PartOfSpeech> { Text = pt.ToFullString(), ItemValue = pt });

            partOfSpeechCBox.SelectedItem = 
                partOfSpeechCBox.Items.Cast<CheckBoxItem<PartOfSpeech>>().Single(o => o.ItemValue == part);

            translationEdit.Text = translation;
            translationEdit.Focus();

            acceptBtn.IsEnabled = false;

            Activated += OnDialog_Activated; //To do initial actions when form will be shown
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

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnDialog_Activated(object sender, EventArgs e)
        {
            Activated -= OnDialog_Activated; //Not need to replay

            FixHeight();
        }

        /// <summary>
        /// Fix current dialog height and prohibit it from changing.
        /// </summary>
        private void FixHeight()
        {
            //https://stackoverflow.com/questions/1256916/how-to-force-actualwidth-and-actualheight-to-update-silverlight

            //To fit dialog's size, especially its height
            SizeToContent = SizeToContent.Manual;
            SizeToContent = SizeToContent.WidthAndHeight;

            //To prevent height changing
            MinHeight = MaxHeight = ActualHeight; //ActualHeight should be updated by this time
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
