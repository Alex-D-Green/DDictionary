using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for TranslationWordDlg.xaml
    /// </summary>
    public partial class TranslationWordDlg: Window
    {
        #region Internal types

        private enum ButtonDecoration
        {
            ResetDecoration,
            UserRightAnswer,
            UserWrongAnswer,
            RightAnswer
        }

        private enum CurrentAction
        {
            HidingAnswers,
            WaitingForUserAnswer,
            ShowingRoundResult
        }

        #endregion


        private const bool hideOptions = true; //TODO: Add option in the settings.


        /// <summary>Random generator for this training.</summary>
        private readonly Random random = new Random(DateTime.Now.Second * DateTime.Now.Millisecond);

        /// <summary>The answers given during the test by the user.</summary>
        private readonly List<TestAnswer> answers = new List<TestAnswer>();


        /// <summary>The list of available for training clauses' ids.</summary>
        private IList<int> clausesIdsLst;

        /// <summary>All words in the dictionary, sorted by alphabet.</summary>
        private JustWordDTO[] allWords;

        /// <summary>The index of the current round.</summary>
        private int currentRound;
        
        /// <summary>Ids of the words that were selected for this training session.</summary>
        private IList<int> wordsForTraining;

        /// <summary>The right answer for this round.</summary>
        private Clause rightAnswerForRound;

        /// <summary>Current action.</summary>
        private CurrentAction currentAction;

        /// <summary>Time since the beginning of the try.</summary>
        private DateTime answerTime;


        /// <summary>Total rounds in the training considering amount of available words.</summary>
        public int TotalRounds 
        { get => clausesIdsLst.Count > MaxRounds ? MaxRounds : clausesIdsLst.Count; }

        public int AnswersPerRound { get; } = 5;

        public int MaxRounds { get; } = 10;

        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>The brush to highlight error answers.</summary>
        private Brush errorBrush { get; }

        /// <summary>The brush to highlight correct answers.</summary>
        private Brush correctBrush { get; }


        public TranslationWordDlg(IList<int> clausesIdsLst)
        {
            if(clausesIdsLst is null)
                throw new ArgumentNullException(nameof(clausesIdsLst));

            this.clausesIdsLst = clausesIdsLst;

            InitializeComponent();

            errorBrush = Resources["ErrorBrush"] as Brush ??
                new SolidColorBrush(Colors.Coral);

            correctBrush = Resources["CorrectBrush"] as Brush ??
                new SolidColorBrush(Colors.LightGreen);

            Activated += OnRelationsEditDlg_Activated; //To auto start training when form will be shown
        }


        /// <summary>
        /// Auto start of the training.
        /// </summary>
        private async void OnRelationsEditDlg_Activated(object sender, EventArgs e)
        {
            Activated -= OnRelationsEditDlg_Activated; //Not need to replay

            await StartTrainingAsync();
        }

        private async Task StartTrainingAsync()
        {
            answers.Clear();
            
            int total = await dbFacade.GetTotalClausesAsync();

            //Conditions checking
            if(total < AnswersPerRound || clausesIdsLst.Count < 1)
            {
                Hide();

                MessageBox.Show(this, String.Format(PrgResources.NotEnoughWords, AnswersPerRound, 1),
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
            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);

            IList<Clause> answers = await GetAnswersForWordAsync(rightAnswerForRound, AnswersPerRound);
            answers.Insert(random.Next(AnswersPerRound), rightAnswerForRound);

            //Set up the controls
            counterLbl.Text = $"{currentRound + 1}/{TotalRounds}";

            for(int i=0; i<AnswersPerRound; i++)
            {
                Button btn = GetAnswerButton(i);

                SetWordOnButton(btn, answers[i]);
                DecorateButton(btn, ButtonDecoration.ResetDecoration);
            }

            DecorateButton(actionBtn, ButtonDecoration.ResetDecoration);

            translationLbl.Text = MakeTransaltionsString(rightAnswerForRound.Translations);

            currentAction = hideOptions ? CurrentAction.HidingAnswers : CurrentAction.WaitingForUserAnswer;
            UpdateActionButtons();

            transcriptionLbl.Visibility = relationsPanel.Visibility = contextLbl.Visibility = Visibility.Hidden;

            ClearRelationsArea();

            answerTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Handler of answers buttons (including "I don't know").
        /// </summary>
        private async void OnAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(sender is Button btn && (btn == actionBtn || btn.Tag is Clause));

            if(sender != actionBtn)
            {
                await HandleAnswerAsync(((Clause)((Button)sender).Tag).Id);

                actionBtn.Focus(); //To allow use space
            }
            else
                await HandleAnswerAsync(null);
        }

        private async Task HandleAnswerAsync(int? answerId)
        {
            if(currentAction == CurrentAction.HidingAnswers)
            {
                if(answerId is null)
                { //Space button was pressed
                    currentAction = CurrentAction.WaitingForUserAnswer;
                    UpdateActionButtons();
                }

                return;
            }
            else if(currentAction == CurrentAction.ShowingRoundResult)
            {
                if(answerId is null)
                { //Space button was pressed
                    if(++currentRound >= TotalRounds)
                    { //Showing final result
                        var dlg = new ResultDlg(answers) { Owner = this };

                        if(dlg.ShowDialog() == true)
                        {
                            //Update the list of clauses
                            clausesIdsLst =
                                clausesIdsLst.Union(dlg.Answers.Select(o => o.Word.Id)) //In case of new words (wrong answers)
                                             .Except(dlg.Answers.Where(o => o.Deleted).Select(o => o.Word.Id)) //Deleted words
                                             .ToList();

                            allWords = null;

                            await StartTrainingAsync();
                        }
                        else
                            Close();
                    }
                    else
                        await NextRoundAsync();
                }
                else //An answer button
                    await PlaySoundAsync(Enumerable.Range(0, AnswersPerRound)
                                                   .Select(o => (Clause)GetAnswerButton(o).Tag)
                                                   .Single(o => o.Id == answerId));

                return;
            }

            Clause givenAnswer = null;

            //Showing result
            foreach(Button btn in Enumerable.Range(0, AnswersPerRound).Select(o => GetAnswerButton(o)))
            {
                var clause = (Clause)btn.Tag;

                if(clause.Id == answerId)
                { //The pressed by the user button
                    givenAnswer = clause;

                    if(answerId == rightAnswerForRound.Id)
                        DecorateButton(btn, ButtonDecoration.UserRightAnswer);
                    else
                        DecorateButton(btn, ButtonDecoration.UserWrongAnswer);
                }
                else if(clause.Id == rightAnswerForRound.Id)
                    DecorateButton(btn, ButtonDecoration.RightAnswer);
                else
                    DecorateButton(btn, ButtonDecoration.ResetDecoration);

                //Show all words translations as tooltips
                btn.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(((Clause)btn.Tag).Translations);
            }

            if(answerId is null)
                DecorateButton(actionBtn, ButtonDecoration.UserWrongAnswer);

            //Collect answer info
            answers.Add(new TestAnswer { 
                Word = rightAnswerForRound, 
                GivenAnswer = givenAnswer,
                Correct = (answerId == rightAnswerForRound.Id),
                Time = DateTime.UtcNow - answerTime
            });

            actionBtn.Focus(); //To save ability to use Space
            currentAction = CurrentAction.ShowingRoundResult;
            UpdateActionButtons();

            if(!String.IsNullOrEmpty(rightAnswerForRound.Transcription))
            {
                transcriptionLbl.Text = $"[{rightAnswerForRound.Transcription}]";
                transcriptionLbl.Visibility = Visibility.Visible;
            }

            if(rightAnswerForRound.Relations.Count > 0)
            {
                UpdateRelations();
                relationsPanel.Visibility = Visibility.Visible;
            }

            if(!String.IsNullOrEmpty(rightAnswerForRound.Context) && rightAnswerForRound.Context.Length <= 200)
            {
                contextLbl.Text = rightAnswerForRound.Context;
                contextLbl.Visibility = Visibility.Visible;
            }

            if(answerId != null && answerId != rightAnswerForRound.Id)
            {
                SystemSounds.Hand.Play(); //Error sound
                await Task.Delay(500);
            }

            await PlaySoundAsync(rightAnswerForRound);
        }

        private void ClearRelationsArea()
        {
            //Remove all old added relations
            FrameworkElement[] toRemove = relationsPanel.Children.OfType<FrameworkElement>()
                                                                 .Where(o => o.Name == null) //The item that was added
                                                                 .ToArray();

            foreach(FrameworkElement item in toRemove)
                relationsPanel.Children.Remove(item);
        }

        private async void UpdateRelations()
        {
            ClearRelationsArea();

            int max = 7;
            foreach(Relation rel in rightAnswerForRound.Relations)
            {
                var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(relTemplatePanel));
                copy.Name = null; //To show that it's a new item
                copy.Visibility = Visibility.Visible;
                copy.MouseLeftButtonUp += async (s, e) => await PlaySoundAsync(rel.ToClause);
                copy.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(
                    (await dbFacade.GetClauseByIdAsync(rel.ToClause.Id)).Translations);

                var newWordLbl = (Label)copy.FindName(nameof(wordLbl));
                newWordLbl.Content = rel.ToClause.Word;

                var newDescriptionLbl = (Label)copy.FindName(nameof(descriptionLbl));
                newDescriptionLbl.Content = $" - {rel.Description}";

                relationsPanel.Children.Insert(relationsPanel.Children.IndexOf(relTemplatePanel), copy);

                if(--max == 0)
                    break;
            }
        }

        private void DecorateButton(Button button, ButtonDecoration decoration)
        {
            var frame = (Panel)button.Parent;

            switch(decoration)
            {
                case ButtonDecoration.RightAnswer:
                case ButtonDecoration.UserRightAnswer:
                    frame.Background = correctBrush;
                    break;

                case ButtonDecoration.UserWrongAnswer: 
                    frame.Background = errorBrush; 
                    break;

                case ButtonDecoration.ResetDecoration:
                    frame.Background = SystemColors.WindowBrush;
                    break;
            }
        }

        private string MakeTransaltionsString(IEnumerable<Translation> translations)
        {
            var ret = new StringBuilder();

            int max = 7;
            foreach(Translation tr in translations.OrderBy(o => random.Next()))
            {
                if(ret.Length > 0)
                    ret.AppendLine(";");

                ret.Append(tr.Text);

                if(--max == 0)
                    break;
            }

            return ret.ToString();
        }

        private static void SetWordOnButton(Button btn, Clause clause)
        {
            if(clause.Word.Length > 22)
                btn.FontSize = 9;
            else if(clause.Word.Length > 19)
                btn.FontSize = 11;
            else if(clause.Word.Length > 16)
                btn.FontSize = 13;
            else
                btn.FontSize = 16;

            btn.Content = clause.Word;
            btn.Tag = clause;
        }

        /// <summary>
        /// Get one of the answers buttons by given index.
        /// </summary>
        private Button GetAnswerButton(int index)
        {
            switch(index)
            {
                case 0: return btn01;
                case 1: return btn02;
                case 2: return btn03;
                case 3: return btn04;
                case 4: return btn05;

                default:
                    throw new InvalidProgramException();
            }
        }

        private void UpdateActionButtons()
        {
            switch(currentAction)
            {
                case CurrentAction.HidingAnswers:
                    buttonsPanel.Visibility = Visibility.Hidden;
                    eyePanel.Visibility = Visibility.Visible;
                    actionBtn.Content = PrgResources.ShowTheOptionsLabel;
                    break;

                case CurrentAction.WaitingForUserAnswer:
                    buttonsPanel.Visibility = Visibility.Visible;
                    eyePanel.Visibility = Visibility.Hidden;
                    actionBtn.Content = PrgResources.IDontKnowLabel;
                    break;

                case CurrentAction.ShowingRoundResult:
                    buttonsPanel.Visibility = Visibility.Visible;
                    eyePanel.Visibility = Visibility.Hidden;
                    actionBtn.Content = (currentRound + 1 == TotalRounds) ? PrgResources.FinishLabel 
                                                                          : PrgResources.NextQuestionLabel;
                    break;
            }
        }

        private IList<int> GetWordsForTraining(int count)
        {
            var ret = new List<int>(count);

            while(ret.Count < count)
            {
                //TODO: Select words by their training statistics not just random ones!
                int n = clausesIdsLst[random.Next(clausesIdsLst.Count)];

                if(!ret.Contains(n))
                    ret.Add(n);
            }

            return ret;
        }

        private async Task<IList<Clause>> GetAnswersForWordAsync(Clause word, int count)
        {
            var ret = new List<Clause>(count-1);

            if(allWords is null)
                allWords = (await dbFacade.GetJustWordsAsync())
                               .OrderBy(o => o.Word, StringComparer.CurrentCultureIgnoreCase)
                               .ToArray();

            //Get a similar word that placed nearly in alphabetical order
            for(int i=0; i<allWords.Length; i++)
            {
                if(allWords[i].Id == word.Id)
                {
                    if(i != 0)
                    {
                        Clause tmp = await dbFacade.GetClauseByIdAsync(allWords[i - 1].Id);
                        
                        if(IsAppropriateAnswer(word, tmp))
                            ret.Add(tmp);
                    }

                    break;
                }
            }

            //And some of random ones
            int maxTries = allWords.Length >= 15 ? 15 : allWords.Length; //Max amount of finding appropriate answers
            int tries = 0;
            while(ret.Count < count-1)
            {
                JustWordDTO w = allWords[random.Next(allWords.Length)];

                if(word.Id != w.Id && !ret.Any(o => o.Id == w.Id))
                {
                    Clause tmp = await dbFacade.GetClauseByIdAsync(w.Id);

                    if(++tries < maxTries && !IsAppropriateAnswer(word, tmp))
                        continue; //It's not appropriate answer, skip it

                    int idx = random.Next(ret.Count);
                    ret.Insert(idx, tmp);
                    tries = 0;
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="answer"/> is appropriate as <b>a wrong answer</b> 
        /// for <paramref name="word"/> (has no similar translations, etc).
        /// </summary>
        private static bool IsAppropriateAnswer(Clause word, Clause answer)
        {
            //HACK: Consider add relations analyzing.

            return answer.Group <= word.Group && //Same or less known word
                   !word.Translations
                        .Select(o => o.Text)
                        .Intersect(answer.Translations.Select(o => o.Text), StringComparer.CurrentCultureIgnoreCase)
                        .Any();
        }

        private async void OnWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape: Close(); break; //To close dialog by Escape key
                
                case Key.D1:
                case Key.NumPad1: await HandleAnswerAsync(((Clause)GetAnswerButton(0).Tag).Id); break;

                case Key.D2:
                case Key.NumPad2: await HandleAnswerAsync(((Clause)GetAnswerButton(1).Tag).Id); break;

                case Key.D3:
                case Key.NumPad3: await HandleAnswerAsync(((Clause)GetAnswerButton(2).Tag).Id); break;

                case Key.D4:
                case Key.NumPad4: await HandleAnswerAsync(((Clause)GetAnswerButton(3).Tag).Id); break;

                case Key.D5:
                case Key.NumPad5: await HandleAnswerAsync(((Clause)GetAnswerButton(4).Tag).Id); break;

                case Key.D0:
                case Key.NumPad0:
                case Key.Space: await HandleAnswerAsync(null); break;
            }
        }

        private async Task PlaySoundAsync(Clause clause)
        {
            if(String.IsNullOrEmpty(clause.Sound))
            {
                SystemSounds.Beep.Play();
                return;
            }

            try { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound); }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnEyePanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await HandleAnswerAsync(null); //As if action button was pressed
        }
    }
}
