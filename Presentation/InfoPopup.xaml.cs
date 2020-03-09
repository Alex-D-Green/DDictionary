using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup: Window
    {
        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;


        public InfoPopup(DataGridClause clause)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));


            InitializeComponent();

            groupLbl.Content = clause.Group.ToFullStr();
            wordLbl.Content = clause.Word;
            translationsLbl.Content = clause.Translations;
            dateLbl.Content = String.Format(Properties.Resources.WatchedDateCount, clause.Watched, clause.WatchedCount);

            if(String.IsNullOrEmpty(clause.Transcription))
            {
                transcriptionLbl.Visibility = Visibility.Hidden;
                relationsLbl.Height = 0; //To correct row height
            }
            else
                transcriptionLbl.Content = $"[{clause.Transcription}]";

            if(clause.HasRelations)
            {
                var cl = dbFacade.GetClauseById(clause.Id);
                Debug.Assert(cl != null);

                var ret = new StringBuilder("");

                int maxCount = 10;
                foreach(Relation rl in cl.Relations)
                {
                    ret.AppendFormat("{0} - {1}\n", rl.ToClause?.Word, rl.Description);

                    if(--maxCount == 0)
                        break;
                }

                relationsLbl.Content = ret.ToString().TrimEnd('\n');
            }
            else
            {
                relationsLbl.Visibility = Visibility.Hidden;
                relationsLbl.Height = 0; //To correct row height
            }
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
