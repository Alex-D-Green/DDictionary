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


namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for WordsConstructorDlg.xaml
    /// </summary>
    public partial class WordsConstructorDlg: TestDlgBase
    {
        #region Internal types

        private class LetterButtonDescription
        {
            public char Letter { get; set; }

            public int Count { get; set; }
        }

        #endregion


        private const bool hideLetters = true; //TODO: Add option in the settings.


        /// <summary>What the user correctly typed so far.</summary>
        private string givenAnswer = "";

        /// <summary>The amount of hints that allowed before the round will be failed.</summary>
        private int hintsAllowed;

        /// <summary>The amount of hints that left before the round will be failed.</summary>
        private int hintsCount;


        public WordsConstructorDlg(IEnumerable<int> clausesForTrainingList)
            : base(clausesForTrainingList, TestType.WordConstructor)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));


            InitializeComponent();

            //Hiding these here to have ability to see layout in the editor
            emptyLetterLbl.Visibility = Visibility.Collapsed;
            filledLetterLbl.Visibility = Visibility.Collapsed;
            letterBtn.Visibility = Visibility.Collapsed;
        }


        protected override async Task StartTrainingAsync()
        {
            await base.StartTrainingAsync();

            int total = await dbFacade.GetTotalClausesAsync();

            //Conditions checking
            if(total < 1 || clausesForTrainingList.Count < 1)
            {
                Hide();

                MessageBox.Show(this, String.Format(PrgResources.NotEnoughWords, 1, 1),
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

            ShowHideLettersButtons(!hideLetters);

            rightAnswerForRound = await dbFacade.GetClauseByIdAsync(wordsForTraining[currentRound]);
            givenAnswer = "";

            answerTime = DateTime.UtcNow;

            //Set up the controls
            counterLbl.Text = $"{currentRound + 1}/{TotalRounds}";

            translationLbl.Text = MakeTranslationsString(rightAnswerForRound.Translations);

            if(!String.IsNullOrEmpty(rightAnswerForRound.Transcription))
                transcriptionLbl.Text = $"[{rightAnswerForRound.Transcription}]";
            else
                transcriptionLbl.Text = "";

            contextLbl.Text = rightAnswerForRound.Context;

            relationsPanel.Visibility = contextLbl.Visibility = soundPanel.Visibility = Visibility.Hidden;
            ClearPanel(relationsPanel);

            SetupWordPanel();
            SetupLettersPanel();

            hintsAllowed = hintsCount = (int)(rightAnswerForRound.Word.Length * 0.1) + 1;
            UpdateTipButton();
        }

        private string MakeTranslationsString(IEnumerable<Translation> translations)
        {
            var ret = new StringBuilder();

            foreach(string tr in translations.OrderBy(o => random.Next())
                                             .Select(o => o.Text)
                                             .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if(ret.Length > 0)
                {
                    if(ret.Length + tr.Length > 128)
                        continue; //Too long let's try to find and add another translation

                    ret.Append("; ");
                }

                ret.Append(tr);
            }

            return ret.ToString();
        }

        private void SetupWordPanel()
        {
            ClearPanel(wordPanel);

            foreach(char letter in rightAnswerForRound.Word.ToLower())
            {
                var copyEmpty = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(emptyLetterLbl));
                copyEmpty.Name = null; //To show that it's a new item
                copyEmpty.Visibility = Visibility.Visible;

                wordPanel.Children.Insert(wordPanel.Children.IndexOf(emptyLetterLbl), copyEmpty);

                var copyFilled = (Border)XamlReader.Parse(XamlWriter.Save(filledLetterLbl));
                copyFilled.Name = null; //To show that it's a new item
                ((TextBlock)copyFilled.Child).Text = letter.ToString();

                wordPanel.Children.Insert(wordPanel.Children.IndexOf(filledLetterLbl), copyFilled);
            }
        }

        private void SetupLettersPanel()
        {
            ClearPanel(lettersPanel);

            foreach(var letter in rightAnswerForRound.Word.ToLower()
                                                          .GroupBy(o => o)
                                                          .Select(gr => new { ch = gr.Key, cnt = gr.Count() })
                                                          .OrderBy(o => o.ch))
            {
                var copy = (Button)XamlReader.Parse(XamlWriter.Save(letterBtn));
                copy.Name = null; //To show that it's a new item
                copy.Visibility = Visibility.Visible;
                copy.Click += OnLetterBtn_Click;
                copy.Tag = new LetterButtonDescription { Letter = letter.ch, Count = letter.cnt };

                //The main button's label
                ((TextBlock)((Grid)copy.Content).Children[0]).Text = letter.ch.ToString();
                
                //The count label of the button
                ((TextBlock)((Grid)copy.Content).Children[1]).Text = letter.cnt > 1 ? letter.cnt.ToString() : ""; 

                lettersPanel.Children.Insert(lettersPanel.Children.IndexOf(letterBtn), copy);
            }
        }

        /// <summary>
        /// Show or hide letters buttons depending on <paramref name="show"/>.
        /// </summary>
        private void ShowHideLettersButtons(bool show)
        {
            lettersPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            unhidePanel.Visibility = show ? Visibility.Collapsed : Visibility.Visible; 
        }

        private void ShowNextCorrectLetter()
        {
            char letter = GetNextCorrectAnswerSymbol();

            givenAnswer += letter;

            Button btn = GetLetterButton(letter) ??
                throw new InvalidProgramException($"Can't find button for this letter ({letter}).");

            var data = (LetterButtonDescription)btn.Tag;
            data.Count--;

            if(data.Count > 0)
            { //Update button counter
                //The count label of the button
                ((TextBlock)((Grid)btn.Content).Children[1]).Text = data.Count > 1 ? data.Count.ToString() : "";
            }
            else
                btn.Visibility = Visibility.Collapsed; //There are no more such letters in the answer

            //Show the next correct letter
            var idx = wordPanel.Children.IndexOf(emptyLetterLbl);
            wordPanel.Children[idx - givenAnswer.Length].Visibility = Visibility.Collapsed;

            idx = wordPanel.Children.IndexOf(filledLetterLbl);
            wordPanel.Children[idx - (rightAnswerForRound.Word.Length - givenAnswer.Length + 1)].Visibility =
                Visibility.Visible;
        }

        /// <summary>
        /// Is this a punctuation mark or a space symbol.
        /// </summary>
        private bool IsPunctuation(char letter)
        {
            return Char.IsPunctuation(letter) || Char.IsWhiteSpace(letter);
        }

        private async Task HandleLetterAsync(char letter)
        {
            letter = Char.ToLower(letter);
            char? nextCorrectLetter = GetNextCorrectAnswerLetter();

            if(letter != GetNextCorrectAnswerSymbol())
            { //Wrong answer
                PlayErrorSound();

                //The following cases are not considered as errors:
                //- Substitute one punctuation mark by another;
                //- Put punctuation mark instead of a letter;
                //- Put the correct letter but without previous punctuation marks.
                if(IsPunctuation(letter) || letter == nextCorrectLetter)
                    return;
                
                hintsCount--;
                UpdateTipButton();

                return;
            }

            //Correct answer
            ShowNextCorrectLetter();

            if(givenAnswer.Length == rightAnswerForRound.Word.Length) //All letters are shown
            {
                currentAction = CurrentAction.ShowingRoundResult;

                await ShowRoundResultAsync();
            }
        }

        private async Task ShowRoundResultAsync()
        {
            bool correctAnswer = (givenAnswer.Length == rightAnswerForRound.Word.Length && hintsCount >= 0);

            while(givenAnswer.Length != rightAnswerForRound.Word.Length)
                ShowNextCorrectLetter();

            currentAction = CurrentAction.ShowingRoundResult;

            UpdateActionButton();

            if(correctAnswer)
                DecorateButton(actionBtn, ButtonDecoration.UserRightAnswer);
            else
                DecorateButton(actionBtn, ButtonDecoration.UserWrongAnswer);

            UpdateTipButton();

            unhidePanel.Visibility = Visibility.Collapsed;

            soundPanel.Visibility = Visibility.Visible;

            transcriptionLbl.Visibility =
                !String.IsNullOrEmpty(rightAnswerForRound.Transcription) ? Visibility.Visible : Visibility.Hidden;

            playBtn.Visibility = 
                !String.IsNullOrEmpty(rightAnswerForRound.Sound) ? Visibility.Visible : Visibility.Hidden;

            UpdateRelationsPanel();
            relationsPanel.Visibility = Visibility.Visible;

            contextLbl.Visibility = Visibility.Visible;

            await PlaySoundAsync(rightAnswerForRound);

            //Collect answer info
            await SaveAnswerAsync(new TestAnswer {
                Word = rightAnswerForRound,
                GivenAnswer = null,
                Correct = correctAnswer,
                Time = DateTime.UtcNow - answerTime,
                Tries = hintsAllowed - hintsCount + 1
            });
        }

        private void UpdateActionButton()
        {
            switch(currentAction)
            {
                case CurrentAction.WaitingForUserAnswer:
                    actionBtn.Content = PrgResources.IDontKnowLabel;
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
                        //Update the list of clauses
                        clausesForTrainingList = clausesForTrainingList
                                .Except(dlg.Answers.Where(o => o.Deleted).Select(o => o.Word.Id)) //Deleted words
                                .ToList();

                        await RefreshAllWords();

                        await StartTrainingAsync();
                    }
                    else
                        Close();
                }
                else
                    await NextRoundAsync();

                return;
            }

            Debug.Assert(currentAction == CurrentAction.WaitingForUserAnswer);
            await ShowRoundResultAsync();
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

                var newWordLbl = (Label)copy.FindName(nameof(wordLbl));
                newWordLbl.Content = rel.ToClause.Word;

                var newDescriptionLbl = (Label)copy.FindName(nameof(descriptionLbl));
                newDescriptionLbl.Content = $" - {rel.Description}";

                relationsPanel.Children.Insert(relationsPanel.Children.IndexOf(relTemplatePanel), copy);

                if(--max == 0)
                    break;
            }
        }

        /// <summary>
        /// Get next <b>symbol</b> of the right answer for the round (no matter what kind of symbol).
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.Testing.WordsConstructorDlg.GetNextCorrectAnswerLetter"/>
        private char GetNextCorrectAnswerSymbol()
        {
            return rightAnswerForRound.Word.ToLower()[givenAnswer.Length];
        }

        /// <summary>
        /// Get next <b>letter</b> of the right answer for the round (all punctuation marks and spaces will be skipped).
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.Testing.WordsConstructorDlg.GetNextCorrectAnswerSymbol"/>
        private char? GetNextCorrectAnswerLetter()
        {
            for(int i=givenAnswer.Length; i<rightAnswerForRound.Word.Length; i++)
                if(!IsPunctuation(rightAnswerForRound.Word[i]))
                    return rightAnswerForRound.Word[i];

            return null;
        }

        /// <summary>
        /// Get the button (if any) with the given letter on it from the letters panel.
        /// </summary>
        private Button GetLetterButton(char letter)
        {
            return lettersPanel.Children.OfType<Button>()
                                        .Where(o => o.Name == null) //The item that was added
                                        .FirstOrDefault(o => ((LetterButtonDescription)o.Tag).Letter == letter);
        }

        private void UpdateTipButton()
        {
            tipBtn.IsEnabled = (currentAction == CurrentAction.WaitingForUserAnswer);

            triesLbl.Content = hintsCount.ToString();
            triesLbl.Foreground = hintsCount >= 0 ? correctBrush : errorBrush;
        }

        private async void OnPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await PlaySoundAsync(rightAnswerForRound);
        }

        private void OnUnhideBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowHideLettersButtons(true);
        }

        private async void OnLetterBtn_Click(object sender, RoutedEventArgs e)
        {
            await HandleLetterAsync(((LetterButtonDescription)((Button)sender).Tag).Letter);
        }

        private async void OnActionButton_Click(object sender, RoutedEventArgs e)
        {
            await HandleActionButtonAsync();
        }

        private async void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if(currentAction != CurrentAction.WaitingForUserAnswer || String.IsNullOrEmpty(e.Text))
                return;

            char ch = e.Text[0];

            if(Char.IsLetterOrDigit(ch) || ch == ' ' || Char.IsPunctuation(ch))
                await HandleLetterAsync(ch);
        }

        private async void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape: Close(); break; //To close dialog by Escape key

                case Key.Space:
                    if(currentAction == CurrentAction.WaitingForUserAnswer)
                    {
                        await HandleLetterAsync(' '); //To prevent using Space as the selected button press
                        e.Handled = true;
                    }

                    if(currentAction == CurrentAction.ShowingRoundResult && Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        await PlaySoundAsync(rightAnswerForRound);
                        e.Handled = true;
                    }
                    
                    break;
                
                case Key.Enter:
                    OnActionButton_Click(null, null);
                    e.Handled = true;
                    break;

                case Key.Back when(unhidePanel.Visibility == Visibility.Visible):
                    OnUnhideBtn_Click(null, null);
                    e.Handled = true;
                    break;

                case Key.F12 when(currentAction == CurrentAction.WaitingForUserAnswer):
                    OnTipBtn_Click(null, null);
                    e.Handled = true;
                    break;
            }
        }

        private async void OnTipBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayErrorSound();
            await HandleLetterAsync(GetNextCorrectAnswerSymbol());

            hintsCount--;
            UpdateTipButton();
        }
    }
}
