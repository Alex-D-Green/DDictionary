using System;
using System.Windows;
using System.Windows.Input;

using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup: Window
    {
        //TODO: Add info about using InfoPopup, as a tooltip to "show popup" option.
        //Tell how to hover over the main data grid without showing popup (Shift key, click, some columns)...

        //HACK: Show full information (in separate rows) about word's relations in InfoPopup?..

        public InfoPopup(DataGridClause clause)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));


            InitializeComponent();

            groupLbl.Content = clause.Group.ToFullStr();
            wordLbl.Content = clause.Word;
            translationsLbl.Content = clause.Translations;
            dateLbl.Content = String.Format(Properties.Resources.UpdateDate, clause.Updated);

            if(String.IsNullOrEmpty(clause.Transcription))
            {
                transcriptionLbl.Visibility = Visibility.Hidden;
                relationsLbl.Height = 0; //To correct row height
            }
            else
                transcriptionLbl.Content = $"[{clause.Transcription}]";

            if(!clause.HasRelations)
            {
                relationsLbl.Visibility = Visibility.Hidden;
                relationsLbl.Height = 0; //To correct row height
            }
            else
                relationsLbl.Content = clause.Relations;
        }


        private void OnWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Close(); //Close the window if any mouse button was pressed
        }

        private void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
                Close();
        }
    }
}
