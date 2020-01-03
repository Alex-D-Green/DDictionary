using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

using DDictionary.DAL;
using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for ClauseEditDlg.xaml
    /// </summary>
    public partial class ClauseEditDlg: Window
    {
        /// <summary>The maximal count of handling translations.</summary>
        public const int MaxCountOfTranslations = 10;


        /// <summary>The editing clause (<c>null</c> for new one).</summary>
        private Clause clause;

        /// <summary>The list of removed translations' ids.</summary>
        private readonly List<int> removedTranslations = new List<int>();


        /// <summary>The count of translations in the dialog.</summary>
        private int countOfTranslations;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = new InMemoryMockStorage(); //Dependency Injection


        public ClauseEditDlg(int? clauseId)
        {
            if(clauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(clauseId));


            if(clauseId.HasValue)
                clause = dbFacade.GetClauseById(clauseId.Value);
            else
                CreateNewClause();

            InitializeComponent();

            PreviewKeyDown += (s, e) =>
            { //To close dialog by Escape key (cuz this dialog has no Cancel button)
                if(e.Key == Key.Escape)
                    Close();
            };

            //ComboBox with groups initialization
            foreach(WordGroup gr in Enum.GetValues(typeof(WordGroup)).Cast<WordGroup>().OrderByDescending(o => o))
                groupCBox.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });

            UpdateInfo();
        }

        /// <summary>
        /// Create a new empty clause and put it into <see cref="DDictionary.Presentation.ClauseEditDlg.clause"/>.
        /// </summary>
        private void CreateNewClause()
        {
            clause = new Clause
            {
                Id = 0, //A new clause mark
                Group = WordGroup.E_TotallyUnknown,
                Added = DateTime.Now,

                Relations = new List<Relation>(),
                Translations = new List<TranslationLink>()
            };
        }

        /// <summary>
        /// Any clause's data was changed.
        /// </summary>
        private void OnSomeDataWasChanged(object sender, EventArgs e)
        {
            ChangesHaveBeenMade();
        }

        /// <summary>
        /// Refill window's controls according to the <see cref="clause"/> data.
        /// </summary>
        private void UpdateInfo()
        {
            countOfTranslations = 0;
            removedTranslations.Clear();

            translationsPanel.Children[0].Visibility = Visibility.Collapsed; //To hide the "template" row
            deleteClauseBtn.IsEnabled = clause.Id != 0;

            Activated += OnClauseEditDlg_Activated; //To do initial actions when form will be shown

            groupCBox.SelectedItem = groupCBox.Items.Cast<CheckBoxItem<WordGroup>>()
                                                    .Single(o => o.ItemValue == clause.Group);

            wordLbl.Text = clause.Word;
            transcriptionLbl.Text = clause.Transcription;
            UpdatePlaySoundButtonState();
            contextEdit.Text = clause.Context;
            relationsLbl.Text = ClauseToDataGridClauseMapper.MakeRelationsString(clause.Relations);

            //Show rows of translations
            foreach(Translation tr in clause.Translations.OrderBy(o => o.Index).Select(o => o.Translation))
            {
                AddTranslationRow(tr.Id, tr.Text, tr.Part);

                if(countOfTranslations == MaxCountOfTranslations)
                    break; //The maximal amount has been shown
            }

            if(clause.Translations.Count > MaxCountOfTranslations)
                MessageBox.Show(this,
                    String.Format(PrgResources.ExceedMaxCountOfTranslations, MaxCountOfTranslations, clause.Translations.Count),
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateAddButtonState();

            saveClauseBtn.IsEnabled = false;
        }

        private void UpdateRelations()
        {
            relationsLbl.Text = ClauseToDataGridClauseMapper.MakeRelationsString(clause.Relations);
        }

        /// <summary>
        /// Update play sound button state depending on 
        /// <see cref="DDictionary.Presentation.ClauseEditDlg.clause"/>.
        /// </summary>
        private void UpdatePlaySoundButtonState()
        {
            playBtn.IsEnabled = !String.IsNullOrEmpty(clause?.Sound);
        }

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnClauseEditDlg_Activated(object sender, EventArgs e)
        {
            Activated -= OnClauseEditDlg_Activated; //Not need to replay

            FixHeight();
        }

        private void UpdateAddButtonState()
        {
            addTranslationBtn.IsEnabled = countOfTranslations < MaxCountOfTranslations;
        }

        /// <summary>
        /// Add new translation row (a panel with respective elements) to the dialog.
        /// </summary>
        private void AddTranslationRow(int id, string text, PartOfSpeech part)
        {
            var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(translationRow));
            int addButtonIdx = translationsPanel.Children.IndexOf(addTranslationBtn);

            var transaltionDTO = new Translation {
                Id = id,
                Text = text,
                Part = part
            };

            copy.Name = null; //To show that it's a new item
            copy.Visibility = Visibility.Visible;
            //copy.Tag = transaltionDTO; //???

            var newTranslationLbl = (TextBlock)copy.FindName(nameof(translationLbl));
            newTranslationLbl.Text = text;

            //var newDescrTBox = (TextBox)copy.FindName(nameof(descrTBox));
            //newDescrTBox.Text = descr;
            //newDescrTBox.TabIndex = addButtonIdx;
            //newDescrTBox.TextChanged += (s, e) =>
            //{
            //    transaltionDTO.Description = newDescrTBox.Text;
            //    transaltionDTO.DescriptionWasChanged = true;
            //    ChangesHaveBeenMade();
            //};

            //var newRemoveBtn = (Button)copy.FindName(nameof(removeBtn));
            //newRemoveBtn.Click += (s, e) =>
            //{
            //    LooseHeight();

            //    mainStackPanel.Children.Remove(copy);
            //    if(relId != 0)
            //        removedRelations.Add(relId);

            //    FixHeight();

            //    countOfRelations--;
            //    UpdateAddButtonState();
            //    ChangesHaveBeenMade();
            //};
            //newRemoveBtn.TabIndex = addButtonIdx + 1;

            LooseHeight();

            translationsPanel.Children.Insert(addButtonIdx, copy);

            if(Visibility == Visibility.Visible) //Only if form is already shown and knows its size
                FixHeight();

            countOfTranslations++;
            UpdateAddButtonState();
        }

        /// <summary>
        /// Allow to change the dialog height.
        /// </summary>
        private void LooseHeight()
        {
            MinHeight = 0;
            MaxHeight = Double.PositiveInfinity;
        }

        /// <summary>
        /// Fix current dialog height and prohibit it from changing.
        /// </summary>
        private void FixHeight()
        {
            //https://stackoverflow.com/questions/1256916/how-to-force-actualwidth-and-actualheight-to-update-silverlight

            //To fit dialog's size, especially its height
            SizeToContent = SizeToContent.Manual;
            SizeToContent = SizeToContent.WidthAndHeight;

            //To prevent height changing
            MinHeight = MaxHeight = ActualHeight; //ActualHeight should be updated by this time
        }

        /// <summary>
        /// Handle play sound button click.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.SoundManager"/>
        private async void OnPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            var ctrl = (FrameworkElement)sender;
            ctrl.IsEnabled = false; //Temporary disabled to prevent multiple clicking

            try
            { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound); }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { ctrl.IsEnabled = true; }
        }

        /// <summary>
        /// Handle change sound reference button click.
        /// </summary>
        private void OnSoundRefBtn_Click(object sender, RoutedEventArgs e)
        {
            //What to do when there is no saved clause yet?..

            var dlg = new SoundRefEditDlg(clause) { Owner = this };

            if(dlg.ShowDialog() == true)
            {
                clause.Sound = dlg.SoundRef;
                UpdatePlaySoundButtonState();

                ChangesHaveBeenMade();
            }
        }

        /// <summary>
        /// Some changes have been made in the clause's data.
        /// </summary>
        private void ChangesHaveBeenMade()
        {
            if(saveClauseBtn != null)
                saveClauseBtn.IsEnabled = true;
        }

        private void OnRelationsLbl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var dlg = new RelationsEditDlg(clause.Id) { Owner = this };

            if(dlg.ShowDialog() == true)
            {
                UpdateRelations();

                ChangesHaveBeenMade();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SoundManager.StopPlaying();

            base.OnClosed(e);
        }

        /// <summary>
        /// Handle New clause button click.
        /// </summary>
        private void OnNewClauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if(saveClauseBtn?.IsEnabled == true &&
               MessageBox.Show(this, PrgResources.ChangesDiscardingWarning, PrgResources.InformationCaption,
                               MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            { 
                return; 
            }

            CreateNewClause();
            UpdateInfo();

            //Remove all old added translations
            FrameworkElement[] toRemove = translationsPanel.Children.OfType<FrameworkElement>()
                                                                    .Where(o => o.Name == null) //The item that was added
                                                                    .ToArray();
            LooseHeight();

            foreach(FrameworkElement item in toRemove)
                translationsPanel.Children.Remove(item);

            FixHeight();
        }
    }
}
