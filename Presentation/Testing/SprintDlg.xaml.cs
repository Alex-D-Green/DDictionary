using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for SprintDlg.xaml
    /// </summary>
    public partial class SprintDlg: TestDlgBase
    {
        #region Internal types

        private enum GivenAnswer
        {
            Unknow,
            Yes,
            No
        }

        #endregion


        /// <summary>Time is allocated for answer.</summary>
        private readonly TimeSpan timeForAnswer = TimeSpan.FromSeconds(3); //TODO: Add option in the settings.


        /// <summary>Timer that count time on answer.</summary>
        private readonly DispatcherTimer answerTimer;


        public SprintDlg(IEnumerable<int> clausesForTrainingList)
            : base(clausesForTrainingList, TestType.Sprint)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));


            answerTimer = new DispatcherTimer();
            answerTimer.Interval = TimeSpan.FromMilliseconds(50);
            answerTimer.Tick += OnAnswerTimerTick;

            InitializeComponent();
        }


        protected override async Task StartTrainingAsync()
        {
            await base.StartTrainingAsync();

            int total = await dbFacade.GetTotalClausesAsync();

            //Conditions checking
            if(total < 2 || clausesForTrainingList.Count < 1)
            {
                Hide();

                MessageBox.Show(this, String.Format(PrgResources.NotEnoughWords, 2, 1),
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

                Close(); //Can't start training
                return;
            }

            //Initializing
            currentRound = 0;
            wordsForTraining = GetWordsForTraining(TotalRounds);

            wordLbl.Text = PrgResources.GetReadyLabel;
            translationLbl.Visibility = yesNoBtnPanel.Visibility = Visibility.Hidden;
            actionBtnPanel.Visibility = Visibility.Visible;

            counterLbl.Text = $"0/{TotalRounds}";
            
            mainPBar.Value = 0;

            currentAction = CurrentAction.WaitingForStart;
        }

        private async Task NextRoundAsync()
        {
            if(currentRound == TotalRounds)
            { //The training is over

                //Showing final result
                var dlg = new ResultDlg(answers) { Owner = this };

                if(dlg.ShowDialog() == true)
                {
                    //Update the list of clauses
                    clausesForTrainingList = clausesForTrainingList
                            .Union(dlg.Answers.Select(o => o.Word.Id)) //In case of new words (wrong answers)
                            .Except(dlg.Answers.Where(o => o.Deleted).Select(o => o.Word.Id)) //Deleted words
                            .ToList();

                    await RefreshAllWords();

                    await StartTrainingAsync();
                }
                else
                    Close();

                return;
            }

            //Preparations for the round
            currentAction = CurrentAction.WaitingForUserAnswer;

            translationLbl.Visibility = yesNoBtnPanel.Visibility = Visibility.Visible;
            actionBtnPanel.Visibility = Visibility.Hidden;

            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);

            answerTime = DateTime.UtcNow;

            Clause wrongAnswerForRound = await GetWrongAnswerForWordAsync(rightAnswerForRound);
            bool showWrongAnswer = (random.Next(100) < 50);

            //Set up the controls
            counterLbl.Text = $"{currentRound + 1}/{TotalRounds}";

            wordLbl.Text = rightAnswerForRound.Word;

            translationLbl.Text = showWrongAnswer ? MakeTranslationsString(wrongAnswerForRound.Translations)
                                                  : MakeTranslationsString(rightAnswerForRound.Translations);

            yesBtn.Tag = showWrongAnswer ? wrongAnswerForRound : rightAnswerForRound;
            noBtn.Tag  = showWrongAnswer ? rightAnswerForRound : wrongAnswerForRound;

            DecorateButton(yesBtn, ButtonDecoration.ResetDecoration);
            DecorateButton(noBtn, ButtonDecoration.ResetDecoration);

            yesBtn.IsEnabled = noBtn.IsEnabled = true;

            mainPBar.Value = 0;

            await PlaySoundAsync(rightAnswerForRound); //Auto play sound

            answerTimer.Start();
        }

        private async Task<Clause> GetWrongAnswerForWordAsync(Clause word)
        {
            int maxTries = allWords.Count >= 15 ? 15 : allWords.Count; //Max amount of finding appropriate answer
            int tries = 0;
            while(true)
            {
                WordTrainingStatisticDTO w = allWords.Values.ElementAt(random.Next(allWords.Count));

                if(word.Id == w.Id)
                    continue;

                Clause ret = await dbFacade.GetClauseByIdAsync(w.Id);

                if(++tries < maxTries && !IsAppropriateAnswer(word, ret))
                    continue; //It's not appropriate answer, skip it

                return ret;
            }
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

        private string MakeTranslationsString(IEnumerable<Translation> translations)
        {
            var ret = new StringBuilder();

            int max = 7;
            foreach(string tr in translations.OrderBy(o => random.Next())
                                             .Select(o => o.Text)
                                             .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if(ret.Length > 0)
                    ret.Append("; ");

                ret.Append(tr);

                if(--max == 0)
                    break;
            }

            return ret.ToString();
        }

        private async void OnActionButton_Click(object sender, RoutedEventArgs e)
        {
            await NextRoundAsync();
        }

        private async Task HandleAnswer(GivenAnswer given)
        {
            currentAction = CurrentAction.ShowingRoundResult;
            answerTimer.Stop();

            Button pressedButton = null;

            if(given != GivenAnswer.Unknow)
                pressedButton = (given == GivenAnswer.Yes) ? yesBtn : noBtn;

            var givenAnswer = (Clause)pressedButton?.Tag;

            yesBtn.IsEnabled = noBtn.IsEnabled = false;

            if(pressedButton is null)
            {
                DecorateButton(yesBtn, ButtonDecoration.UserWrongAnswer);
                DecorateButton(noBtn, ButtonDecoration.UserWrongAnswer);
            }
            else
                DecorateButton(pressedButton, givenAnswer.Id == rightAnswerForRound.Id ? ButtonDecoration.UserRightAnswer
                                                                                       : ButtonDecoration.UserWrongAnswer);

            if(givenAnswer?.Id != rightAnswerForRound.Id)
                PlayErrorSound();

            //Collect answer info
            await SaveAnswerAsync(new TestAnswer {
                Word = rightAnswerForRound,
                GivenAnswer = (pressedButton == yesBtn ? givenAnswer : null),
                Correct = (givenAnswer?.Id == rightAnswerForRound.Id),
                Time = DateTime.UtcNow - answerTime
            });

            await Task.Delay(1000); //Delay for showing the answer

            if(currentAction != CurrentAction.ShowingRoundResult)
                return; //Probably the window is closed already

            //Automatically go to the next round
            currentRound++;
            await NextRoundAsync();
        }

        private async void OnYesNoButton_Click(object sender, RoutedEventArgs e)
        {
            await HandleAnswer(sender == yesBtn ? GivenAnswer.Yes : GivenAnswer.No);
        }

        private async void OnAnswerTimerTick(object sender, EventArgs e)
        {
            mainPBar.Value = ((DateTime.UtcNow - answerTime).TotalMilliseconds / timeForAnswer.TotalMilliseconds) * 100;

            if(mainPBar.Value >= 100)
                await HandleAnswer(GivenAnswer.Unknow);
        }

        private async void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape: Close(); break; //To close dialog by Escape key

                case Key.Left when(currentAction == CurrentAction.WaitingForUserAnswer):
                    await HandleAnswer(GivenAnswer.No);
                    e.Handled = true;
                    break;

                case Key.Right when(currentAction == CurrentAction.WaitingForUserAnswer):
                    await HandleAnswer(GivenAnswer.Yes);
                    e.Handled = true;
                    break;

                case Key.Enter when(currentAction == CurrentAction.WaitingForStart):
                    OnActionButton_Click(null, null);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            answerTimer.Stop();
            currentAction = CurrentAction.WaitingForStart; //To show that the training is finished

            base.OnClosed(e);
        }
    }
}
