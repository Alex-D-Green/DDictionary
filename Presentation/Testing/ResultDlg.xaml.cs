using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for ResultDlg.xaml
    /// </summary>
    public partial class ResultDlg: Window
    {
        /// <summary>The list of answers with information about changes in the dictionary.</summary>
        public IList<TestAnswer> Answers { get; }

        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;


        public ResultDlg(IEnumerable<TestAnswer> answers)
        {
            if(answers is null)
                throw new ArgumentNullException(nameof(answers));

            if(answers.Count() < 1)
                throw new ArgumentException("The enumerator has to contain at least one item.", nameof(answers));


            //Include words that were given as wrong answers
            Answers = answers.SelectMany(o => 
                (o.Correct || o.GivenAnswer is null) ? new[] { o } 
                                                     : new[] { o, new TestAnswer { 
                                                         Correct = false, 
                                                         Word = o.GivenAnswer, 
                                                         Time = o.Time, 
                                                         GivenAnswer = o.Word 
                                                     } })
                             .Distinct(new TestAnswerSameWordComparer())
                             .OrderBy(o => o.Correct) //To have the similar order in the editing dialog
                             .ToList();

            InitializeComponent();

            UpdateResult();
        }

        private void UpdateResult()
        {
            int correct = Answers.Count(o => o.Correct);
            int total = Answers.Count;

            resultLbl.Content = String.Format(PrgResources.RightTestAnswers, correct, total, 
                                              (int)Math.Round((double)correct / total * 100));
            
            mistakesLbl.Text = String.Format(PrgResources.MistakesLabel, total - correct);
            correctLbl.Text = String.Format(PrgResources.CorrectLabel, correct);

            //Remove all old rows
            FrameworkElement[] toRemove = resultsPanel.Children.OfType<FrameworkElement>()
                                                               .Where(o => o.Name == null) //The item that was added
                                                               .ToArray();

            foreach(FrameworkElement item in toRemove)
                resultsPanel.Children.Remove(item);

            resultPanel.Visibility = Visibility.Visible;

            foreach(TestAnswer ans in Answers.Reverse()) //To show answers in the order as they were given
            {
                var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(resultPanel));
                copy.Name = null; //To show that it's a new item
                
                var newTimeLbl = (TextBlock)copy.FindName(nameof(timeLbl));
                newTimeLbl.Text = $"{ans.Time.TotalSeconds:F0} s";

                var newgroupLbl = (TextBlock)copy.FindName(nameof(groupLbl));
                newgroupLbl.Text = ans.Word.Group.ToGradeStr();

                var newPlayBtn = (Button)copy.FindName(nameof(playBtn));
                newPlayBtn.IsEnabled = !String.IsNullOrEmpty(ans.Word.Sound);
                newPlayBtn.Click += OnPlayBtn_Click;
                newPlayBtn.Tag = ans.Word;

                var newWordLbl = (TextBlock)copy.FindName(nameof(wordLbl));
                newWordLbl.Text = ans.Word.Word;
                newWordLbl.MouseLeftButtonUp += OnWordLbl_MouseLeftButtonUp;
                newWordLbl.Tag = ans.Word;

                var newTranslationsLbl = (TextBlock)copy.FindName(nameof(translationsLbl));
                newTranslationsLbl.Text = ClauseToDataGridClauseMapper.MakeTranslationsString(ans.Word.Translations);

                if(newTranslationsLbl.Text?.Length > 55) //Truncate too long string
                {
                    newTranslationsLbl.ToolTip = newTranslationsLbl.Text;
                    newTranslationsLbl.Text = $"{newTranslationsLbl.Text.Substring(0, 50)}...";
                }

                //Maybe to show the word data in the popup?..

                if(ans.Deleted)
                { //For the deleted clause
                    newWordLbl.TextDecorations.Clear();
                    newWordLbl.TextDecorations.Add(TextDecorations.Strikethrough);
                    newTranslationsLbl.TextDecorations.Add(TextDecorations.Strikethrough);

                    copy.IsEnabled = false;
                }

                int idx = resultsPanel.Children.IndexOf(ans.Correct ? correctLbl : mistakesLbl);
                resultsPanel.Children.Insert(idx + 1, copy);
            }

            resultPanel.Visibility = Visibility.Collapsed;
        }

        private void OnContinueButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private async void OnPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            var clause = (Clause)((FrameworkElement)sender).Tag;

            try { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound); }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnWordLbl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clause = (Clause)((FrameworkElement)sender).Tag;
            bool needToUpdate = false;

            var lst = Answers.Where(o => !o.Deleted).Select(o => o.Word.Id).ToList();

            var dlg = new ClauseEditDlg(clause.Id, lst) { Owner = this };
            dlg.ClausesWereUpdated += () => needToUpdate = true;

            await Task.Delay(150); //To prevent mouse event from catching in the just opened dialog

            dlg.ShowDialog();

            if(needToUpdate)
            {
                foreach(TestAnswer ans in Answers)
                { //Fetch new data from the DB
                    Clause newClause = await dbFacade.GetClauseByIdAsync(ans.Word.Id);

                    if(newClause is null)
                        ans.Deleted = true; //The clause was deleted
                    else
                        ans.Word = newClause; //Update
                }

                UpdateResult(); //Redraw window data
            }
        }
    }
}
