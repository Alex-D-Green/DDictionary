using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

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
        /// <summary>Shown clause.</summary>
        private readonly DataGridClause clause;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;


        public InfoPopup(DataGridClause clause)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));


            InitializeComponent();

            this.clause = clause;

            groupLbl.Content = clause.Group.ToFullStr();
            wordLbl.Content = clause.Word;
            translationsLbl.Content = clause.Translations;
            dateLbl.Content = String.Format(Properties.Resources.WatchedDateCount, clause.Watched, clause.WatchedCount);

            if(String.IsNullOrEmpty(clause.Transcription))
                transcriptionLbl.Visibility = Visibility.Hidden;
            else
                transcriptionLbl.Content = $"[{clause.Transcription}]";

            if(clause.HasRelations)
            {
                Clause cl = dbFacade.GetClauseByIdAsync(clause.Id).Result;
                Debug.Assert(cl != null);

                int maxCount = 10;
                foreach(Relation rl in cl.Relations)
                {
                    var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(relTemplatePanel));
                    copy.Name = null; //To show that it's a new item
                    copy.Visibility = Visibility.Visible;
                    copy.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(
                        dbFacade.GetClauseByIdAsync(rl.ToClause.Id).Result.Translations);

                    var newWordLbl = (Label)copy.FindName(nameof(relationLbl));
                    newWordLbl.Content = rl.ToClause.Word;

                    var newDescriptionLbl = (Label)copy.FindName(nameof(descriptionLbl));
                    newDescriptionLbl.Content = $" - {rl.Description}";

                    mainPanel.Children.Insert(mainPanel.Children.IndexOf(relTemplatePanel), copy);

                    if(--maxCount == 0)
                        break;
                }
            }

            relTemplatePanel.Visibility = Visibility.Hidden;
            relTemplatePanel.Height = 0; //To correct row height
        }


        private void OnWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Close(); //Close the window if any mouse button was pressed
        }

        private async void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.None && (e.Key == Key.Escape || (e.Key >= Key.A && e.Key <= Key.Z) ||
               (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)))
            { 
                Close();

                //HACK: Somehow resend this input to the parent window...
            }
            else if(!String.IsNullOrEmpty(clause.Sound) && e.Key == Key.Space)
            {
                try { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound, dbFacade.DataSource); }
                catch(FileNotFoundException) { }
            }
        }
    }
}
