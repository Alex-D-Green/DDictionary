using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using DDictionary.Domain;
using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Base class for all testing dialogs.
    /// </summary>
    public abstract class TestDlgBase: Window
    {
        #region Internal types

        protected enum CurrentAction
        {
            WaitingForStart,
            HidingAnswers,
            WaitingForUserAnswer,
            ShowingRoundResult
        }

        protected enum ButtonDecoration
        {
            ResetDecoration,
            UserRightAnswer,
            UserWrongAnswer,
            RightAnswer
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

        /// <summary>The max amount of rounds per training.</summary>
        public virtual int MaxRounds { get; } = 10;

        /// <summary>The object to work with data storage.</summary>
        protected IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>The brush to highlight error answers.</summary>
        protected Brush errorBrush { get; }

        /// <summary>The brush to highlight correct answers.</summary>
        protected Brush correctBrush { get; }


        protected TestDlgBase(IList<int> clausesForTrainingList)
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
        /// in the training list, then start the first round or close the window.
        /// </summary>
        protected abstract Task StartTrainingAsync();

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
        /// Remove all elements without name from the given panel.
        /// </summary>
        protected static void ClearPanel(Panel panel)
        {
            //Remove all old added relations
            FrameworkElement[] toRemove = panel.Children.OfType<FrameworkElement>()
                                                        .Where(o => o.Name == null) //The item that was added
                                                        .ToArray();

            foreach(FrameworkElement item in toRemove)
                panel.Children.Remove(item);
        }

        /// <summary>
        /// Play clause's sound or beep if it has no sound.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "<Pending>")]
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
        /// Play the sound of error (user's error).
        /// </summary>
        protected static void PlayErrorSound()
        {
            SystemSounds.Hand.Play();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if(currentRound < TotalRounds &&
                MessageBox.Show(this, PrgResources.TrainingIsNotFinishedWarning, PrgResources.QuestionCaption,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
