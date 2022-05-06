﻿using System;
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
using DDictionary.Domain.DTO;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;
using PrgSettings = DDictionary.Properties.Settings;


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


        /// <summary>The sorted list of available for training clauses' ids.</summary>
        private List<int> clausesForTrainingListInternal;

        /// <summary>Ids of the words that were selected for this training session.</summary>
        protected IList<int> wordsForTraining;

        /// <summary>The index of the current round.</summary>
        protected int currentRound;

        /// <summary>The right answer for this round.</summary>
        protected Clause rightAnswerForRound;

        /// <summary>Current action.</summary>
        protected volatile CurrentAction currentAction;

        /// <summary>Time since the beginning of the try.</summary>
        protected DateTime answerTime;


        /// <summary>All words from the dictionary. Key it's clause's id.</summary>
        protected IReadOnlyDictionary<int, WordTrainingStatisticDTO> allWords { get; private set; }

        /// <summary>The sorted list of available for training clauses' ids.</summary>
        /// <value><see cref="clausesForTrainingListInternal"/></value>
        protected IReadOnlyList<int> clausesForTrainingList 
        { 
            get => clausesForTrainingListInternal;

            set
            {
                clausesForTrainingListInternal = (value is List<int> lst) ? lst : value.ToList();
                clausesForTrainingListInternal.Sort(Comparer); //Sort words according to their rating for this training
            }
        }

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

        /// <summary>Test's type.</summary>
        public TestType TrainingType { get; }

        /// <summary>"Command" that after dialogs closing it's needed to show statistic dialog.</summary>
        public bool GoToStatistic { get; protected set; }


        protected TestDlgBase(IEnumerable<int> clausesForTrainingList, TestType type)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));

            RefreshAllWords().Wait();

            this.clausesForTrainingList = clausesForTrainingList.ToList();
            TrainingType = type;

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
        /// Load all words from the data source into <see cref="allWords"/>.
        /// </summary>
        protected async Task RefreshAllWords()
        {
            allWords = (await dbFacade.GetWordTrainingStatisticsAsync(TrainingType)).ToDictionary(o => o.Id);
            
            clausesForTrainingListInternal?.Sort(Comparer); //Sort words according to their rating for this training
        }

        /// <summary>
        /// Check ability to start a new training depend on available amount of words in the dictionary and 
        /// in the training list, then start the first round or close the window.
        /// </summary>
        /// <remarks>Base version just clear the <see cref="DDictionary.Presentation.Testing.TestDlgBase.answers"/>.
        /// </remarks>
        protected virtual Task StartTrainingAsync()
        {
            answers.Clear();

            return Task.CompletedTask;
        }

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

            //Take one third of the words by random
            while(ret.Count < Math.Round(count * 0.3333))
            {
                int id = clausesForTrainingList[random.Next(clausesForTrainingList.Count)];

                if(!ret.Contains(id))
                    ret.Add(id);
            }


            //Then words with "active" asterisk for this type of training
            List<int> wordsWithAsterisk = clausesForTrainingList.Where(o => GetWordAsterisk(o) != null).ToList();

            foreach(int id in wordsWithAsterisk)
            {
                if(!ret.Contains(id))
                    ret.Add(id);

                if(ret.Count == count)
                    break;
            }


            if(ret.Count != count)
            { //Then words without statistics on this type of training
                List<int> wordsWithoutStat = clausesForTrainingList.Where(o => GetWordStatistics(o) == null).ToList();

                foreach(int id in wordsWithoutStat)
                {
                    if(!ret.Contains(id))
                        ret.Add(id);

                    if(ret.Count == count)
                        break;
                }
            }


            if(ret.Count != count)
            { //The rest ones (if needed) according to their statistic (the list is sorted)
                foreach(int id in clausesForTrainingList)
                {
                    if(!ret.Contains(id))
                        ret.Add(id);

                    if(ret.Count == count)
                        break;
                }
            }

            //Shuffle the list
            for(int i=0; i<3; i++)
                ret.Sort((x, y) => random.Next(3) - 1);

            return ret;
        }

        private TrainingStatisticDTO GetWordStatistics(int id)
        {
            return allWords[id].Statistics?.FirstOrDefault(o => o.TestType == TrainingType);
        }

        private AsteriskDTO GetWordAsterisk(int id)
        {
            var now = DateTime.Now;
            var ret = allWords[id].Asterisk;

            if( ((ret?.Type == AsteriskType.AllTypes || ret?.Type == AsteriskType.Meaning) && 
                     TestCategoryMapper.IsItMeaningCategory(TrainingType) && 
                     appropriateDate(ret.MeaningLastTrain)) ||
                ((ret?.Type == AsteriskType.AllTypes || ret?.Type == AsteriskType.Spelling) &&
                     TestCategoryMapper.IsItSpellingCategory(TrainingType) && 
                     appropriateDate(ret.SpellingLastTrain)) ||
                ((ret?.Type == AsteriskType.AllTypes || ret?.Type == AsteriskType.Listening) &&
                     TestCategoryMapper.IsItListeningCategory(TrainingType) && 
                     appropriateDate(ret.ListeningLastTrain)) )
            {
                return ret;
            }

            return null;


            bool appropriateDate(DateTime? date) => 
                date is null || (now - date.Value).TotalDays >= 1;
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

            try { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound, dbFacade.DataSource); }
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

        protected async Task SaveAnswerAsync(TestAnswer answer)
        {
            if(answer is null)
                throw new ArgumentNullException(nameof(answer));


            answers.Add(answer);

            await dbFacade.AddOrUpdateTrainingStatisticAsync(TrainingType, answer.Word.Id, answer.Correct);
         
            var now = DateTime.Now;
            var meaning = TestCategoryMapper.IsItMeaningCategory(TrainingType) ? now : (DateTime?)null;
            var spelling = TestCategoryMapper.IsItSpellingCategory(TrainingType) ? now : (DateTime?)null;
            var listening = TestCategoryMapper.IsItListeningCategory(TrainingType) ? now : (DateTime?)null;

            await dbFacade.UpdateTimestampsForAsteriskAsync(answer.Word.Id, meaning, spelling, listening);

            if(!answer.Correct && answer.GivenAnswer != null)
                await dbFacade.AddOrUpdateTrainingStatisticAsync(TrainingType, answer.GivenAnswer.Id, false);
        }

        /// <summary>
        /// Compare two words in terms of desirability for this training type.
        /// More desirable word will be "less".
        /// </summary>
        private int Comparer(int firstId, int secondId)
        {
            TrainingStatisticDTO w1 = GetWordStatistics(firstId);
            TrainingStatisticDTO w2 = GetWordStatistics(secondId);

            if(w1 is null && w2 is null)
                return 0; //Both words have no statistic
            else if(w1 is null || w2 is null)
                return w1 is null ? -1 : 1; //The word with no statistic is "less"

            //Both words have statistics, let's calculate the estimation
            int dPercent = getPercent(w1) - getPercent(w2); //The less successful word is "less"
            int dTries = getTotal(w1) - getTotal(w2); //The less trained word is "less"
            int dDays = (int)(w1.LastTraining - w2.LastTraining).TotalDays; //The word that was trained earlier is "less"

            int estimate = dPercent + dTries*10 + dDays;

            if(estimate != 0)
                return estimate; //Consider the estimate as words are not "equal"
            else //Words are "equal" by the estimation
                return DateTime.Compare(w1.LastTraining, w2.LastTraining); //Then let's consider their date and time


            //Get the total tries for the word
            int getTotal(TrainingStatisticDTO tr) => tr.Success + tr.Fail;

            //Get the percent of success for the word
            int getPercent(TrainingStatisticDTO tr) => (int)((double)tr.Success / getTotal(tr) * 100);
        }

        protected virtual void ApplyGUIScale(FrameworkElement mainPanel)
        {
            double guiScale = PrgSettings.Default.DialogsScale;

            mainPanel.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;

            Width *= guiScale;
            Height *= guiScale;
        }

        protected static void SetWordOnButton(Button btn, string text, double maxFont = 16, double minFont = 7)
        {
            var tb = (TextBlock)btn.Content;
            tb.Text = text;
            tb.TextWrapping = TextWrapping.NoWrap;

            double maxWidth = btn.ActualWidth - btn.Padding.Left - btn.Padding.Right -
                btn.BorderThickness.Left - btn.BorderThickness.Right -
                tb.Padding.Left - tb.Padding.Right - tb.Margin.Left - tb.Margin.Right;

            double fs = maxFont;

            while(fs >= minFont)
            {
                btn.FontSize = fs;

                tb.Measure(new Size(maxWidth, btn.ActualHeight));

                if(tb.DesiredSize.Width < maxWidth)
                    break;

                fs -= 0.5;
            }
        }
    }
}
