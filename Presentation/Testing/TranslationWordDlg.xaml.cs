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
    /// Interaction logic for TranslationWordDlg.xaml
    /// </summary>
    public partial class TranslationWordDlg: SelectiveTestDlgBase
    {
        private const bool hideOptions = true; //TODO: Add option in the settings.


        protected override Button actionButton { get => actionBtn; }


        public TranslationWordDlg(IList<int> clausesForTrainingList)
            : base(clausesForTrainingList)
        {
            if(clausesForTrainingList is null)
                throw new ArgumentNullException(nameof(clausesForTrainingList));

            InitializeComponent();
        }


        protected override async Task NextRoundAsync()
        {
            //Preparations for the round
            currentAction = hideOptions ? CurrentAction.HidingAnswers : CurrentAction.WaitingForUserAnswer;

            await base.NextRoundAsync();

            IList<Clause> answersForRound = await GetAnswersForWordAsync(rightAnswerForRound, AnswersPerRound);
            answersForRound.Insert(random.Next(AnswersPerRound), rightAnswerForRound);

            //Set up the controls
            counterLbl.Text = $"{currentRound + 1}/{TotalRounds}";

            for(int i=0; i<AnswersPerRound; i++)
                SetWordOnButton(GetAnswerButton(i), answersForRound[i]);

            translationLbl.Text = MakeTranslationsString(rightAnswerForRound.Translations);
            transcriptionLbl.Visibility = relationsPanel.Visibility = contextLbl.Visibility = Visibility.Hidden;
            ClearPanel(relationsPanel);
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

        protected override void ShowRightAnswerData()
        {
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
        }

        private async void UpdateRelations()
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

        private string MakeTranslationsString(IEnumerable<Translation> translations)
        {
            var ret = new StringBuilder();

            int max = 7;
            foreach(string tr in translations.OrderBy(o => random.Next())
                                             .Select(o => o.Text)
                                             .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if(ret.Length > 0)
                    ret.AppendLine(";");

                ret.Append(tr);

                if(--max == 0)
                    break;
            }

            return ret.ToString();
        }

        private static void SetWordOnButton(Button btn, Clause clause)
        {
            if(clause.Word.Length > 29)
                btn.FontSize = 9;
            else if(clause.Word.Length > 24)
                btn.FontSize = 11;
            else if(clause.Word.Length > 19)
                btn.FontSize = 13;
            else
                btn.FontSize = 16;

            ((TextBlock)btn.Content).Text = clause.Word;
            btn.Tag = clause;
        }

        protected override Button GetAnswerButton(int index)
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

        protected override void UpdateActionButtons()
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

                    foreach(Button btn in Enumerable.Range(0, AnswersPerRound).Select(o => GetAnswerButton(o)))
                        btn.ToolTip = null;

                    break;

                case CurrentAction.ShowingRoundResult:
                    buttonsPanel.Visibility = Visibility.Visible;
                    eyePanel.Visibility = Visibility.Hidden;
                    actionBtn.Content = (currentRound + 1 == TotalRounds) ? PrgResources.FinishLabel 
                                                                          : PrgResources.NextQuestionLabel;

                    //Show all words translations as tooltips
                    foreach(Button btn in Enumerable.Range(0, AnswersPerRound).Select(o => GetAnswerButton(o)))
                        btn.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(((Clause)btn.Tag).Translations);

                    break;

                default:
                    throw new InvalidProgramException($"Unexpected current action value ({currentAction}).");
            }
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

        private async void OnEyePanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await HandleAnswerAsync(null); //As if action button was pressed
        }
    }
}
