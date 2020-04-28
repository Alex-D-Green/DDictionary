using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Base class for testing dialogs where one should choose answers from the list.
    /// </summary>
    public abstract class SelectiveTestDlgBase: Window
    {
        #region Internal types

        protected enum ButtonDecoration
        {
            ResetDecoration,
            UserRightAnswer,
            UserWrongAnswer,
            RightAnswer
        }

        protected enum CurrentAction
        {
            HidingAnswers,
            WaitingForUserAnswer,
            ShowingRoundResult
        }

        #endregion


        /// <summary>Random generator for this training.</summary>
        protected readonly Random random = new Random(DateTime.Now.Second * DateTime.Now.Millisecond);

        /// <summary>The answers given during the test by the user.</summary>
        protected readonly List<TestAnswer> answers = new List<TestAnswer>();


        /// <summary>The list of available for training clauses' ids.</summary>
        protected IList<int> clausesForTrainingList;

        /// <summary>All words in the dictionary, sorted by alphabet.</summary>
        protected JustWordDTO[] allWords;

        /// <summary>The index of the current round.</summary>
        protected int currentRound;

        /// <summary>Ids of the words that were selected for this training session.</summary>
        protected IList<int> wordsForTraining;

        /// <summary>The right answer for this round.</summary>
        protected Clause rightAnswerForRound;

        /// <summary>Current action.</summary>
        protected CurrentAction currentAction;

        /// <summary>Time since the beginning of the try.</summary>
        protected DateTime answerTime;


        /// <summary>Total rounds in the training considering amount of available words.</summary>
        public int TotalRounds
        { get => clausesForTrainingList.Count > MaxRounds ? MaxRounds : clausesForTrainingList.Count; }

        /// <summary>The count of possible answers per round.</summary>
        public virtual int AnswersPerRound { get; } = 5;

        /// <summary>The max amount of rounds per training.</summary>
        public virtual int MaxRounds { get; } = 10;

        /// <summary>The object to work with data storage.</summary>
        protected IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>The brush to highlight error answers.</summary>
        protected Brush errorBrush { get; }

        /// <summary>The brush to highlight correct answers.</summary>
        protected Brush correctBrush { get; }

        /// <summary>The button that is used for perform next round action.</summary>
        protected abstract Button actionButton { get; }


        protected SelectiveTestDlgBase(IList<int> clausesForTrainingList)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));


            this.clausesForTrainingList = clausesForTrainingList;

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

        /// <summary>
        /// Check ability to start a new training depend on available amount of words in the dictionary and 
        /// in the training list, then start the first round.
        /// </summary>
        protected virtual async Task StartTrainingAsync()
        {
            answers.Clear();

            int total = await dbFacade.GetTotalClausesAsync();

            //Conditions checking
            if(total < AnswersPerRound || clausesForTrainingList.Count < 1)
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

        /// <summary>
        /// Start the next round.
        /// Base method set up the <see cref="rightAnswerForRound"/> and <see cref="answerTime"/>,
        /// resets buttons' decorations and update their state 
        /// <see cref="DDictionary.Presentation.Testing.SelectiveTestDlgBase.UpdateActionButtons"/>.
        /// </summary>
        protected virtual async Task NextRoundAsync()
        {
            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);

            for(int i=0; i<AnswersPerRound; i++)
                DecorateButton(GetAnswerButton(i), ButtonDecoration.ResetDecoration);

            DecorateButton(actionButton, ButtonDecoration.ResetDecoration);

            UpdateActionButtons();

            answerTime = DateTime.UtcNow;
        }

        /// <summary>
        /// React on the given answer (considering <see cref="currentAction"/>).
        /// </summary>
        protected virtual async Task HandleAnswerAsync(int? answerId)
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
                            clausesForTrainingList = clausesForTrainingList
                                    .Union(dlg.Answers.Select(o => o.Word.Id)) //In case of new words (wrong answers)
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
                DecorateButton(actionButton, ButtonDecoration.UserWrongAnswer);

            //Collect answer info
            answers.Add(new TestAnswer {
                Word = rightAnswerForRound,
                GivenAnswer = givenAnswer,
                Correct = (answerId == rightAnswerForRound.Id),
                Time = DateTime.UtcNow - answerTime
            });

            actionButton.Focus(); //To save ability to use Space
            currentAction = CurrentAction.ShowingRoundResult;
            UpdateActionButtons();

            ShowRightAnswerData();

            //Capture the value for this method cuz rightAnswerForRound could be changed while the method running
            Clause rightAnswerForRoundCaptured = rightAnswerForRound;

            if(answerId != null && answerId != rightAnswerForRoundCaptured.Id)
            {
                SystemSounds.Hand.Play(); //Error sound
                await Task.Delay(500);
            }

            await PlaySoundAsync(rightAnswerForRoundCaptured);
        }

        /// <summary>
        /// Show the data of the right answer for this round.
        /// </summary>
        protected abstract void ShowRightAnswerData();

        /// <summary>
        /// Update the area of action buttons according to <see cref="currentAction"/>.
        /// </summary>
        protected abstract void UpdateActionButtons();

        /// <summary>
        /// Get one of the answers buttons by given index.
        /// </summary>
        protected abstract Button GetAnswerButton(int index);

        /// <summary>
        /// Set up the button (and its frame) appearance according to the given state decoration.
        /// </summary>
        protected virtual void DecorateButton(Button button, ButtonDecoration decoration)
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

        /// <summary>
        /// Get certain amount of words for training out of <see cref="clausesForTrainingList"/>.
        /// </summary>
        protected virtual IList<int> GetWordsForTraining(int count)
        {
            var ret = new List<int>(count);

            while(ret.Count < count)
            {
                //TODO: Select words by their training statistics not just random ones!
                int n = clausesForTrainingList[random.Next(clausesForTrainingList.Count)];

                if(!ret.Contains(n))
                    ret.Add(n);
            }

            return ret;
        }

        /// <summary>
        /// Play clause's sound or beep if it has no sound.
        /// </summary>
        protected async Task PlaySoundAsync(Clause clause)
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

        /// <summary>
        /// Handle numeric keys from 1-5 and 0+Space and call 
        /// <see cref="DDictionary.Presentation.Testing.SelectiveTestDlgBase.HandleAnswerAsync"/>.
        /// </summary>
        protected virtual async void OnWindow_KeyDown(object sender, KeyEventArgs e)
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
    }
}
