using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Base class for testing dialogs where one should choose answers from the list.
    /// </summary>
    public abstract class SelectiveTestDlgBase: TestDlgBase
    {
        /// <summary>The count of possible answers per round.</summary>
        public virtual int AnswersPerRound { get; } = 5;

        /// <summary>The button that is used for perform next round action.</summary>
        protected abstract Button actionButton { get; }


        protected SelectiveTestDlgBase(IEnumerable<int> clausesForTrainingList, TestType type)
            : base(clausesForTrainingList, type)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));
        }


        protected override async Task StartTrainingAsync()
        {
            await base.StartTrainingAsync();

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
        /// <see cref="DDictionary.Presentation.Testing.SelectiveTestDlgBase.UpdateActionButtonsAsync"/>.
        /// </summary>
        protected virtual async Task NextRoundAsync()
        {
            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);

            for(int i=0; i<AnswersPerRound; i++)
                DecorateButton(GetAnswerButton(i), ButtonDecoration.ResetDecoration);

            DecorateButton(actionButton, ButtonDecoration.ResetDecoration);

            await UpdateActionButtonsAsync();

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
                    await UpdateActionButtonsAsync();
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
                            await RefreshAllWords();

                            //Update the list of clauses
                            clausesForTrainingList = clausesForTrainingList
                                    .Union(dlg.Answers.Select(o => o.Word.Id)) //In case of new words (wrong answers)
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
            }

            if(answerId is null)
                DecorateButton(actionButton, ButtonDecoration.UserWrongAnswer);

            //Collect answer info
            await SaveAnswerAsync(new TestAnswer {
                Word = rightAnswerForRound,
                GivenAnswer = givenAnswer,
                Correct = (answerId == rightAnswerForRound.Id),
                Time = DateTime.UtcNow - answerTime
            });

            currentAction = CurrentAction.ShowingRoundResult;
            await UpdateActionButtonsAsync();

            ShowRightAnswerData();

            //Capture the value for this method cuz rightAnswerForRound could be changed while the method running
            Clause rightAnswerForRoundCaptured = rightAnswerForRound;

            if(answerId != null && answerId != rightAnswerForRoundCaptured.Id)
            {
                PlayErrorSound();
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
        protected abstract Task UpdateActionButtonsAsync();

        /// <summary>
        /// Get one of the answers buttons by given index.
        /// </summary>
        protected abstract Button GetAnswerButton(int index);

        /// <summary>
        /// Handle numeric keys from 1-5 and 0+Space and call 
        /// <see cref="DDictionary.Presentation.Testing.SelectiveTestDlgBase.HandleAnswerAsync"/>.
        /// </summary>
        protected virtual async void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape: Close(); break; //To close dialog by Escape key

                case Key.D1:
                case Key.NumPad1: 
                    await HandleAnswerAsync(((Clause)GetAnswerButton(0).Tag).Id);
                    e.Handled = true;
                    break;

                case Key.D2:
                case Key.NumPad2: 
                    await HandleAnswerAsync(((Clause)GetAnswerButton(1).Tag).Id);
                    e.Handled = true;
                    break;

                case Key.D3:
                case Key.NumPad3: 
                    await HandleAnswerAsync(((Clause)GetAnswerButton(2).Tag).Id);
                    e.Handled = true;
                    break;

                case Key.D4:
                case Key.NumPad4: 
                    await HandleAnswerAsync(((Clause)GetAnswerButton(3).Tag).Id);
                    e.Handled = true;
                    break;

                case Key.D5:
                case Key.NumPad5: 
                    await HandleAnswerAsync(((Clause)GetAnswerButton(4).Tag).Id);
                    e.Handled = true;
                    break;

                case Key.D0:
                case Key.NumPad0:
                case Key.Enter: 
                    await HandleAnswerAsync(null);
                    e.Handled = true;
                    break;

                case Key.Space when(currentAction == CurrentAction.ShowingRoundResult && 
                                    Keyboard.Modifiers == ModifierKeys.Control):
                    await PlaySoundAsync(rightAnswerForRound);
                    e.Handled = true;
                    break;
            }
        }
    }
}
