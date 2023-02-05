using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;
using PrgSettings = DDictionary.Properties.Settings;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for ListeningDlg.xaml
    /// </summary>
    public partial class ListeningDlg: TestDlgBase
    {
        #region Internal types

        private enum AnswerCheckResult
        {
            Incorrect,
            Absent,
            Correct
        }

        #endregion


        /// <summary>How many additional tries were made by this round.</summary>
        private int triesWereMade;


        public ListeningDlg(IEnumerable<int> clausesForTrainingList)
            : base(clausesForTrainingList, TestType.Listening)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));


            //Exclude clauses without sound
            var withoutSound = 
                dbFacade.GetClausesAsync(new Domain.FiltrationCriteria { HasSound = false }).Result
                                                                                            .Select(o => o.Id)
                                                                                            .ToArray();

            this.clausesForTrainingList = clausesForTrainingList.Except(withoutSound).ToList();

            InitializeComponent();
            ApplyGUIScale(mainPanel);
        }


        protected override async Task StartTrainingAsync()
        {
            await base.StartTrainingAsync();

            int total = await dbFacade.GetTotalClausesAsync();

            //Conditions checking
            if(total < 1 || clausesForTrainingList.Count < 1)
            {
                Hide();

                MessageBox.Show(this, String.Format(PrgResources.NotEnoughWordsWithSounds, 1),
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

                Close(); //Can't start training
                return;
            }

            //Initializing
            currentRound = 0;
            wordsForTraining = GetWordsForTraining(TotalRounds);

            await NextRoundAsync();
        }

        private async Task NextRoundAsync()
        {
            //Preparations for the round
            currentAction = CurrentAction.WaitingForUserAnswer;

            UpdateActionButton();
            DecorateButton(actionBtn, ButtonDecoration.ResetDecoration);

            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);

            answerTime = DateTime.UtcNow;

            triesWereMade = 0;

            //Set up the controls
            counterLbl.Text = $"{currentRound + 1}/{TotalRounds}";

            translationLbl.Text = ClauseToDataGridClauseMapper.MakeTranslationsString(rightAnswerForRound.Translations);

            if(!String.IsNullOrEmpty(rightAnswerForRound.Transcription))
                transcriptionLbl.Text = $"[{rightAnswerForRound.Transcription}]";
            else
                transcriptionLbl.Text = "";

            contextLbl.Text = rightAnswerForRound.Context;

            relationsPanel.Visibility = contextLbl.Visibility = transcriptionLbl.Visibility = 
                translationLbl.Visibility = Visibility.Hidden;

            correctAnsLbl.Visibility = wrongAnsLbl.Visibility = Visibility.Collapsed;

            ClearPanel(relationsPanel);

            answerEdit.Text = "";
            answerEdit.Visibility = enterHereLbl.Visibility = Visibility.Visible;
            answerEdit.Focus();

            await PlaySoundAsync(rightAnswerForRound); //Auto play sound
        }

        private void UpdateActionButton()
        {
            switch(currentAction)
            {
                case CurrentAction.WaitingForUserAnswer:
                    actionBtn.Content = String.IsNullOrEmpty(answerEdit.Text) ? PrgResources.IDontKnowLabel
                                                                              : PrgResources.CheckLabel;
                    break;

                case CurrentAction.ShowingRoundResult:
                    actionBtn.Content = (currentRound + 1 == TotalRounds) ? PrgResources.FinishLabel
                                                                          : PrgResources.NextQuestionLabel;
                    break;

                default:
                    throw new InvalidProgramException($"Unexpected current action value ({currentAction}).");
            }
        }

        private async Task HandleActionButtonAsync()
        {
            if(currentAction == CurrentAction.ShowingRoundResult)
            {
                if(++currentRound >= TotalRounds)
                { //Showing final result
                    var dlg = new ResultDlg(answers) { Owner = this };

                    if(dlg.ShowDialog() == true)
                    {
                        await RefreshAllWords(dlg.Answers.Where(x => x.Deleted).Select(x => x.Word.Id));

                        //Update the list of clauses
                        clausesForTrainingList = clausesForTrainingList
                                .Union(dlg.Answers.Where(o => !String.IsNullOrEmpty(o.Word.Sound))
                                                  .Select(o => o.Word.Id)) //In case of new words (wrong answers)
                                .Except(dlg.Answers.Where(o => o.Deleted).Select(o => o.Word.Id)) //Deleted words
                                .ToList();

                        await StartTrainingAsync();
                    }
                    else
                    {
                        GoToStatistic = dlg.GoToStatistic; //"Command" retranslation
                        Close();
                    }
                }
                else
                    await NextRoundAsync();

                return;
            }

            Debug.Assert(currentAction == CurrentAction.WaitingForUserAnswer);

            if(IsAnswerCorrect() == AnswerCheckResult.Incorrect && 
               PrgSettings.Default.ListeningAllowOneMistake && 
               triesWereMade < 1)
            { //Give a second chance
                triesWereMade++;
                PlayErrorSound();

                return;
            }

            await ShowRoundResultAsync();
        }

        private AnswerCheckResult IsAnswerCorrect()
        {
            if(String.IsNullOrEmpty(answerEdit.Text))
                return AnswerCheckResult.Absent;

            return String.Equals(prepareWord(answerEdit.Text), prepareWord(rightAnswerForRound.Word)) 
                ? AnswerCheckResult.Correct 
                : AnswerCheckResult.Incorrect;


            string prepareWord(string word)
            { //Do not count spaces and punctuation
                if(word is null)
                    return null;

                var ret = new StringBuilder();

                foreach(char ch in word.Where(o => !Char.IsWhiteSpace(o) && !Char.IsPunctuation(o)))
                    ret.Append(ch);

                return ret.ToString().ToLower();
            }
        }

        private async Task ShowRoundResultAsync()
        {
            AnswerCheckResult answerResult = IsAnswerCorrect();

            currentAction = CurrentAction.ShowingRoundResult;

            UpdateActionButton();

            if(answerResult == AnswerCheckResult.Correct)
                DecorateButton(actionBtn, ButtonDecoration.UserRightAnswer);
            else
                DecorateButton(actionBtn, ButtonDecoration.UserWrongAnswer);

            answerEdit.Visibility = enterHereLbl.Visibility = Visibility.Collapsed;

            correctAnsLbl.Text = rightAnswerForRound.Word;
            correctAnsLbl.Visibility = Visibility.Visible;

            if(answerResult != AnswerCheckResult.Correct)
            {
                wrongAnsLbl.Text = (answerResult == AnswerCheckResult.Absent) ? PrgResources.NoAnswer : answerEdit.Text;
                
                wrongAnsLbl.TextDecorations.Clear();

                if(answerResult != AnswerCheckResult.Absent)
                    wrongAnsLbl.TextDecorations.Add(TextDecorations.Strikethrough);

                wrongAnsLbl.Visibility = Visibility.Visible;
            }

            transcriptionLbl.Visibility =
                !String.IsNullOrEmpty(rightAnswerForRound.Transcription) ? Visibility.Visible : Visibility.Hidden;

            translationLbl.Visibility = Visibility.Visible;

            UpdateRelationsPanel();
            relationsPanel.Visibility = Visibility.Visible;

            contextLbl.Visibility = Visibility.Visible;

            await PlaySoundAsync(rightAnswerForRound);

            int wrongWordId = 0;

            if(answerResult == AnswerCheckResult.Incorrect)
                wrongWordId = await dbFacade.GetClauseIdByWordAsync(wrongAnsLbl.Text);

            //Collect answer info
            await SaveAnswerAsync(new TestAnswer {
                Word = rightAnswerForRound,
                GivenAnswer = (wrongWordId == 0 ? null : await dbFacade.GetClauseByIdAsync(wrongWordId)),
                Correct = (answerResult == AnswerCheckResult.Correct),
                Time = DateTime.UtcNow - answerTime,
                Tries = triesWereMade + 1
            });
        }

        private async void UpdateRelationsPanel()
        {
            ClearPanel(relationsPanel);

            int max = 7;
            foreach(Relation rel in rightAnswerForRound.Relations)
            {
                var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(relTemplatePanel));
                copy.Name = null; //To show that it's a new item
                copy.Visibility = Visibility.Visible;
                copy.MouseLeftButtonUp += async (s, e) => await PlaySoundAsync(rel.ToClause);
                copy.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(
                    (await dbFacade.GetClauseByIdAsync(rel.ToClause.Id)).Translations);
                
                if(PrgSettings.Default.AutoplaySound && !String.IsNullOrEmpty(rel.ToClause.Sound))
                    copy.ToolTipOpening += async (s, e) => await PlaySoundAsync(rel.ToClause);

                var newWordLbl = (Label)copy.FindName(nameof(wordLbl));
                newWordLbl.Content = rel.ToClause.Word;

                var newDescriptionLbl = (Label)copy.FindName(nameof(descriptionLbl));
                newDescriptionLbl.Content = $" - {rel.Description}";

                relationsPanel.Children.Insert(relationsPanel.Children.IndexOf(relTemplatePanel), copy);

                if(--max == 0)
                    break;
            }
        }

        private async void OnPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await PlaySoundAsync(rightAnswerForRound);
        }

        private async void OnActionButton_Click(object sender, RoutedEventArgs e)
        {
            await HandleActionButtonAsync();
        }

        private void OnAnswerEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(currentAction == CurrentAction.WaitingForUserAnswer)
                UpdateActionButton();
        }

        private async void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape: Close(); break; //To close dialog by Escape key

                case Key.Space when(Keyboard.Modifiers == ModifierKeys.Control):
                    await PlaySoundAsync(rightAnswerForRound);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    OnActionButton_Click(null, null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
