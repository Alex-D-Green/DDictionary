using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        /// <summary>The count of translations in the dialog.</summary>
        private int countOfShownTranslations;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = new InMemoryMockStorage(); //Dependency Injection

        /// <summary>All clause's relations.</summary>
        private List<RelationDTO> relations { get; } = new List<RelationDTO>();

        /// <summary>All clause's translations.</summary>
        private List<Translation> translations { get; } = new List<Translation>();


        public ClauseEditDlg(int? clauseId)
        {
            if(clauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(clauseId));


            if(clauseId.HasValue)
            {
                clause = dbFacade.GetClauseById(clauseId.Value);
                relations.AddRange(clause.Relations.Select(o => o.MapToRelationDTO()));
                translations.AddRange(clause.Translations);
            }
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
            countOfShownTranslations = 0;

            translationsPanel.Children[0].Visibility = Visibility.Collapsed; //To hide the "template" row
            deleteClauseBtn.IsEnabled = clause.Id != 0;

            Activated += OnClauseEditDlg_Activated; //To do initial actions when form will be shown

            groupCBox.SelectedItem = groupCBox.Items.Cast<CheckBoxItem<WordGroup>>()
                                                    .Single(o => o.ItemValue == clause.Group);

            wordEdit.Text = clause.Word;
            transcriptionEdit.Text = clause.Transcription;
            UpdatePlaySoundButtonState();
            contextEdit.Text = clause.Context;
            UpdateRelations();

            //Show rows of translations
            translations.Sort((x, y) => x.Index.CompareTo(y.Index));

            foreach(Translation tr in translations)
            {
                AddTranslationRow(tr);

                if(countOfShownTranslations == MaxCountOfTranslations)
                    break; //The maximal amount has been shown
            }

            if(translations.Count > MaxCountOfTranslations)
                MessageBox.Show(this,
                    String.Format(PrgResources.ExceedMaxCountOfTranslations, MaxCountOfTranslations, translations.Count),
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateAddButtonState();
            UpdateTranslationsArrowsState();

            saveClauseBtn.IsEnabled = false;
        }

        private void UpdateRelations()
        {
            relationsLbl.Text = ClauseToDataGridClauseMapper.MakeRelationsString(relations.Select(o => o.ToWord));
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
            addTranslationBtn.IsEnabled = countOfShownTranslations < MaxCountOfTranslations;
        }

        /// <summary>
        /// Add new translation row (a panel with respective elements) to the dialog.
        /// </summary>
        private void AddTranslationRow(Translation tr)
        {
            var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(translationRow));
            int addButtonIdx = translationsPanel.Children.IndexOf(addTranslationBtn);

            copy.Name = null; //To show that it's a new item
            copy.Visibility = Visibility.Visible;
            copy.Tag = tr;

            var newTranslationLbl = (TextBlock)copy.FindName(nameof(translationLbl));
            newTranslationLbl.Text = TranslationConverter.ConvertToString(tr);

            var newRemoveBtn = (Button)copy.FindName(nameof(trRemoveBtn));
            newRemoveBtn.Click += (s, e) =>
            {
                LooseHeight();

                translationsPanel.Children.Remove(copy);
                translations.Remove(tr);

                FixHeight();

                countOfShownTranslations--;
                UpdateAddButtonState();
                UpdateTranslationsArrowsState();
                ChangesHaveBeenMade();
            };

            var newTrUpBtn = (Button)copy.FindName(nameof(trUpBtn));
            newTrUpBtn.Click += (s, e) => MoveTranslationRow(copy, moveUp: true);

            var newTrDownBtn = (Button)copy.FindName(nameof(trDownBtn));
            newTrDownBtn.Click += (s, e) => MoveTranslationRow(copy, moveUp: false);

            LooseHeight();

            translationsPanel.Children.Insert(addButtonIdx, copy);

            if(Visibility == Visibility.Visible) //Only if form is already shown and knows its size
                FixHeight();

            countOfShownTranslations++;
            UpdateAddButtonState();
        }

        /// <summary>
        /// Update state of the arrows buttons and tab indices for all translation rows.
        /// </summary>
        private void UpdateTranslationsArrowsState()
        {
            //Tab indices start from the last button (remove) of the "template" row
            int tabIdx = ((Control)translationRow.FindName(nameof(trRemoveBtn))).TabIndex;

            //The child with index 0 is hidden "template" row so skip it
            foreach(var row in translationsPanel.Children.Cast<FrameworkElement>().Skip(1).Take(countOfShownTranslations))
            { 
                var upBtn = (Button)row.FindName(nameof(trUpBtn));
                var downBtn = (Button)row.FindName(nameof(trDownBtn));
                var removeBtn = (Button)row.FindName(nameof(trRemoveBtn));

                upBtn.IsEnabled = !isItTopMostRow(row);
                downBtn.IsEnabled = !isItBottomMostRow(row);

                upBtn.TabIndex = ++tabIdx;
                downBtn.TabIndex = ++tabIdx;
                removeBtn.TabIndex = ++tabIdx;
            }


            bool isItTopMostRow(FrameworkElement row) =>
                translationsPanel.Children.IndexOf(row) == 1;

            bool isItBottomMostRow(FrameworkElement row) =>
                translationsPanel.Children.IndexOf(row) == countOfShownTranslations;
        }

        /// <summary>
        /// Change the position of the translation row (which determines translation index for the clause).
        /// </summary>
        private void MoveTranslationRow(FrameworkElement row, bool moveUp)
        {
            int idx = translationsPanel.Children.IndexOf(row) + (moveUp ? -1 : 1);

            translationsPanel.Children.Remove(row);
            translationsPanel.Children.Insert(idx, row);

            //Correct items' order as well
            var tr = (Translation)row.Tag;
            idx = translations.IndexOf(tr) + (moveUp ? -1 : 1);

            translations.Remove(tr);
            translations.Insert(idx, tr);

            UpdateTranslationsArrowsState();
            ChangesHaveBeenMade();
        }

        /// <summary>
        /// Handle Add Translation button click.
        /// </summary>
        private void OnAddTranslationBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new TranslationsEditDlg() { Owner = this };

            if(dlg.ShowDialog() == true)
            {
                var trans = new Translation {
                    Index = translations.Count > 0 ? translations.Max(o => o.Index) + 1 : 1,
                    Part = dlg.Translation.Value.partOfSpeech,
                    Text = dlg.Translation.Value.translation,
                };

                //To save items visible order in case if not all translations are shown in the dialog
                translations.Insert(countOfShownTranslations, trans);

                AddTranslationRow(trans);

                UpdateTranslationsArrowsState();
                ChangesHaveBeenMade();
            }
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally { ctrl.IsEnabled = true; }
        }

        /// <summary>
        /// Handle change sound reference button click.
        /// </summary>
        private void OnSoundRefBtn_Click(object sender, RoutedEventArgs e)
        {
            //For all new clauses id = 0 is used, 
            //so firstly the external sound will be cached according to this id (for every new clause) 
            //but after the new clause will be saved the program needs to cache sound again with the correct clause's id.

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

        private void ShowEditRelationsDlg()
        {
            var dlg = new RelationsEditDlg(clause.Id, wordEdit.Text, relations) { Owner = this };

            if(dlg.ShowDialog() == true)
            {
                relations.Clear();
                relations.AddRange(dlg.Relations);

                UpdateRelations();

                ChangesHaveBeenMade();
            }
        }

        private void OnRelationsLbl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowEditRelationsDlg();
        }

        private void OnRelationsLbl_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Space || e.Key == Key.Enter)
            {
                ShowEditRelationsDlg();

                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SoundManager.StopPlaying();

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !DiscardingChangesIsApproved();

            base.OnClosing(e);
        }

        /// <summary>
        /// Handle New clause button click.
        /// </summary>
        private void OnNewClauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!DiscardingChangesIsApproved())
                return;

            relations.Clear();
            translations.Clear();

            //Remove all old added translations
            FrameworkElement[] toRemove = translationsPanel.Children.OfType<FrameworkElement>()
                                                                    .Where(o => o.Name == null) //The item that was added
                                                                    .ToArray();
            LooseHeight();

            foreach(FrameworkElement item in toRemove)
                translationsPanel.Children.Remove(item);

            FixHeight();

            CreateNewClause();
            UpdateInfo();
        }

        /// <summary>
        /// Discarding changes is approved or there are no changes in the clause's data.
        /// </summary>
        /// <remarks>If there are any changes then message box will be shown.</remarks>
        private bool DiscardingChangesIsApproved()
        {
            return saveClauseBtn?.IsEnabled != true ||
               MessageBox.Show(this, PrgResources.ChangesDiscardingWarning, PrgResources.InformationCaption,
                               MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
        }

        /// <summary>
        /// Handle Save button click.
        /// </summary>
        private void OnSaveClauseBtn_Click(object sender, RoutedEventArgs e)
        {
            saveClauseBtn.IsEnabled = false;

            var clauseDTO = new ClauseUpdateDTO {
                Id = clause.Id,
                Context = contextEdit.Text,
                Group = ((CheckBoxItem<WordGroup>)groupCBox.SelectedItem).ItemValue,
                Sound = clause.Sound,
                Transcription = transcriptionEdit.Text,
                Word = wordEdit.Text
            };

            int clauseId = dbFacade.AddOrUpdateClause(clauseDTO);

            //Handle relations
            dbFacade.RemoveRelations(clause.Relations.Select(o => o.Id)
                                                     .Except(relations.Select(o => o.Id))
                                                     .ToArray());

            foreach(RelationDTO rel in relations.Where(o => o.Id == 0 || o.DescriptionWasChanged))
            {
                dbFacade.AddOrUpdateRelation(rel.Id, clauseId, rel.ToWordId, rel.Description);

                if(rel.MakeInterconnected)
                { //Add relation to the other side
                    Debug.Assert(rel.Id == 0);

                    dbFacade.AddOrUpdateRelation(0, rel.ToWordId, clauseId, rel.Description);
                }
            }

            //Handle translations
            dbFacade.RemoveTranslations(clause.Translations.Select(o => o.Id)
                                                           .Except(translations.Select(o => o.Id))
                                                           .ToArray());

            for(int i=0; i<translations.Count; i++)
            {
                translations[i].Index = i; //Correcting items indices according to collection
                dbFacade.AddOrUpdateTranslation(translations[i], clauseId);
            }


            //Implement Next/Previous buttons.
            //Implement Delete button.

            DialogResult = true;
            Close();
        }
    }
}
