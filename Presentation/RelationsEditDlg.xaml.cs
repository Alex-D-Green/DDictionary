﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

using DDictionary.Domain;
using DDictionary.Domain.DTO;
using DDictionary.Presentation.Converters;

using PrgResources = DDictionary.Properties.Resources;
using PrgSettings = DDictionary.Properties.Settings;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for RelationsEditDlg.xaml
    /// </summary>
    public partial class RelationsEditDlg: Window
    {
        /// <summary>The maximal count of handling relations.</summary>
        public const int MaxCountOfRelations = 10;


        /// <summary>The count of relations in the dialog.</summary>
        private int countOfRelations;

        /// <summary>The tab index for the new row of relation.</summary>
        private int newRelationRowTabIdx;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>All clause's relations.</summary>
        private List<RelationDTO> relations { get; } = new List<RelationDTO>();

        /// <summary>All clause's relations.</summary>
        /// <value><see cref="DDictionary.Presentation.RelationsEditDlg.relations"/></value>
        public IReadOnlyCollection<RelationDTO> Relations { get => relations; }

        /// <summary>The brush to mark errors text.</summary>
        private Brush errorTextBrush { get; }


        public RelationsEditDlg(int clauseId, string word, IEnumerable<RelationDTO> relations)
        {
            if(clauseId < 0)
                throw new ArgumentOutOfRangeException(nameof(clauseId));


            if(relations?.Any() == true)
                this.relations.AddRange(relations);

            InitializeComponent();
            ApplyGUIScale();

            errorTextBrush = Resources["TextErrorBrush"] as Brush ??
                new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80));

            wordLbl.Content = String.Format(PrgResources.WordRelationTo, word);

            //Initialize the list of all words in the dictionary except this clause's word itself
            listOfWordsCBox.ItemsSource = dbFacade.GetJustWordsAsync().Result
                                                  .Where(o => o.Id != clauseId)
                                                  .OrderBy(o => o.Word);

            //Show rows of relations
            foreach(RelationDTO rel in Relations)
            {
                AddRelationRow(rel);

                if(countOfRelations == MaxCountOfRelations)
                    break; //The maximal amount has been shown
            }

            if(Relations.Count > MaxCountOfRelations)
                MessageBox.Show(this, 
                    String.Format(PrgResources.ExceedMaxCountOfRelations, MaxCountOfRelations, Relations.Count), 
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

            mainStackPanel.Children[1].Visibility = Visibility.Collapsed; //To hide the "template" row
            UpdateAddButtonState();

            Activated += OnRelationsEditDlg_Activated; //To do initial actions when form will be shown
        }

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnRelationsEditDlg_Activated(object sender, EventArgs e)
        {
            Activated -= OnRelationsEditDlg_Activated; //Not need to replay

            FixHeight();

            listOfWordsCBox.Focus();
        }

        /// <summary>
        /// Add new relation row (a panel with respective elements) to the dialog.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.RelationsEditDlg.AddRelationRow(int, string, int, string, bool)"/>
        /// <seealso cref="DDictionary.Presentation.RelationsEditDlg.newRelationRowTabIdx"/>
        private async void AddRelationRow(RelationDTO relationDTO)
        {
            var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(relationRow));
            int addPanelIdx = mainStackPanel.Children.IndexOf(addNewRelationPanel);

            copy.Visibility = Visibility.Visible;
            copy.Tag = relationDTO;
            var cl = await dbFacade.GetClauseByIdAsync(relationDTO.ToWordId);
            var newToWordLbl = (Label)copy.FindName(nameof(toWordLbl));
            newToWordLbl.Content = relationDTO.ToWord;
            newToWordLbl.ToolTip = ClauseToDataGridClauseMapper.MakeTranslationsString(cl.Translations);

            if(PrgSettings.Default.AutoplaySound && !String.IsNullOrEmpty(cl.Sound))
                newToWordLbl.ToolTipOpening += async (s, e) =>
                    await SoundManager.PlaySoundAsync(cl.Id, cl.Sound, dbFacade.DataSource);

            var newDescrTBox = (TextBox)copy.FindName(nameof(descrTBox));
            newDescrTBox.Text = relationDTO.Description;
            newDescrTBox.TabIndex = ++newRelationRowTabIdx;
            newDescrTBox.TextChanged += (s, e) =>
            {
                relationDTO.Description = newDescrTBox.Text;
                relationDTO.DescriptionWasChanged = true;
                ChangesHaveBeenMade();
            };

            var newRemoveBtn = (Button)copy.FindName(nameof(removeBtn));
            newRemoveBtn.Click += (s, e) =>
            {
                LooseHeight();

                mainStackPanel.Children.Remove(copy);

                FixHeight();

                countOfRelations--;
                UpdateAddButtonState();
                ChangesHaveBeenMade();
            };
            newRemoveBtn.TabIndex = ++newRelationRowTabIdx;

            LooseHeight();

            mainStackPanel.Children.Insert(addPanelIdx, copy);

            if(Visibility == Visibility.Visible) //Only if form is already shown and knows its size
                FixHeight();

            countOfRelations++;
            UpdateAddButtonState();
        }

        /// <summary>
        /// Add new relation row (a panel with respective elements) to the dialog.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.RelationsEditDlg.AddRelationRow(RelationDTO)"/>
        private void AddRelationRow(int relId, string word, int wordId, string descr, bool makeInterconnected = false)
        {
            var relationDTO = new RelationDTO {
                Id = relId,
                ToWordId = wordId,
                ToWord = word,
                Description = descr,
                MakeInterconnected = makeInterconnected
            };

            AddRelationRow(relationDTO);
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

        #region Handling of "add new relative" button's state

        private void OnNewRelationDescrTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddButtonState();
        }

        private async void OnListOfWordsCBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAddButtonState();

            await UpdateTranslationHint(((JustWordDTO)listOfWordsCBox.SelectedItem)?.Id);
        }

        private void UpdateAddButtonState(bool forceDisable = false)
        {
            addRelationBtn.IsEnabled = !forceDisable &&
                                       countOfRelations < MaxCountOfRelations &&
                                       listOfWordsCBox.SelectedItem != null &&
                                       !String.IsNullOrEmpty(newRelationDescrTBox.Text);
        }

        #endregion

        /// <summary>
        /// Create new relation.
        /// </summary>
        private void OnAddRelationBtn_Click(object sender, RoutedEventArgs e)
        {
            var word = (JustWordDTO)listOfWordsCBox.SelectedItem;

            AddRelationRow(0, word.Word, word.Id, newRelationDescrTBox.Text, interconnectedCheck.IsChecked == true);

            newRelationDescrTBox.Text = ""; //To prevent duplication
            ChangesHaveBeenMade();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Some changes have been made in the records.
        /// </summary>
        private void ChangesHaveBeenMade()
        {
            acceptBtn.IsEnabled = true;
        }

        /// <summary>
        /// Save all changes in relations into the storage.
        /// </summary>
        private void SaveChanges()
        {
            int relationRowIdx = mainStackPanel.Children.IndexOf(relationRow);
            int addPanelIdx = mainStackPanel.Children.IndexOf(addNewRelationPanel);

            if(relations.Count > MaxCountOfRelations)
                relations.RemoveRange(0, MaxCountOfRelations); //Clear only "editable" range of the collection
            else
                relations.Clear();

            for(int i=relationRowIdx+1, idx=0; i<addPanelIdx; i++, idx++)
            {
                var rowPanel = (FrameworkElement)mainStackPanel.Children[i];
                var relationDTO = (RelationDTO)rowPanel.Tag;

                relations.Insert(idx, relationDTO); //To save order in case if the collection exceed "editable" range
            }
        }

        /// <summary>
        /// Put the clause translations into the translation hint, 
        /// or clear it if <paramref name="clauseId"/> is <c>null</c>.
        /// </summary>
        private async Task UpdateTranslationHint(int? clauseId)
        {
            if(clauseId != null)
            {
                translationLbl.Content = ClauseToDataGridClauseMapper.MakeTranslationsString(
                    (await dbFacade.GetClauseByIdAsync(clauseId.Value)).Translations);
            }
            else
                translationLbl.Content = "";
        }

        private async void OnListOfWordsCBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var word = (JustWordDTO)listOfWordsCBox.SelectedItem;

            if(word is null || listOfWordsCBox.Text != word.Word)
            {
                await UpdateTranslationHint(null);
                UpdateAddButtonState(forceDisable: true);

                listOfWordsCBox.Foreground = errorTextBrush;
            }
            else
            {
                await UpdateTranslationHint(word.Id);
                UpdateAddButtonState();

                listOfWordsCBox.Foreground = SystemColors.WindowTextBrush;
            }
        }

        private void ApplyGUIScale()
        {
            double guiScale = PrgSettings.Default.DialogsScale;

            mainStackPanel.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;

            Width *= guiScale;
            Height *= guiScale;
        }
    }
}
