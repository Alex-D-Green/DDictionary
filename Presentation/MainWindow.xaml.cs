using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using DDictionary.DAL;
using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        /// <summary>The minimal main data grid's columns width that used when a column is "hidden".</summary>
        public const double ZeroColumnWidth = 3; //This width could be used in the future to indicate skipped columns 
                                                 //while importing data (e.g. printing).


        /// <summary>Folder to place downloaded sounds' files.</summary>
        private static readonly DirectoryInfo soundsCacheFolder = new DirectoryInfo(".\\sndCache");

        //https://www.wpf-tutorial.com/audio-video/playing-audio/
        private readonly MediaPlayer mediaPlayer = new MediaPlayer { Volume = 1 };

        /// <summary>The currently applied filter.</summary>
        private readonly FiltrationCriteria currentFilter = new FiltrationCriteria();


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = new InMemoryMockStorage(); //Dependency Injection

        /// <summary>The brush to highlight cells.</summary>
        private Brush highlightBrush
        { get => mainDataGrid.Resources["HighlightBrush"] as Brush ?? Brushes.Yellow; }


        public MainWindow()
        {
            InitializeComponent();

            #region ComboBoxes with groups initialization

            foreach(WordGroup gr in Enum.GetValues(typeof(WordGroup)).Cast<WordGroup>().OrderByDescending(o => o))
            {
                groupFilterCBox.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });
                toGroupCBox.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });
            }

            UpdateGroupFilterText();

            #endregion

            UpdateDataGrid();

            textFilterEdit.Focus();
        }

        /// <summary>
        /// Get the list of checked groups for clauses' filtration.
        /// </summary>
        private IEnumerable<WordGroup> GetFilterGroups()
        {
            return groupFilterCBox.Items.Cast<CheckBoxItem<WordGroup>>().Where(o => o.IsSelected).Select(o => o.ItemValue);
        }

        /// <summary>
        /// Make ClauseDataGridDTO from Clause.
        /// </summary>
        private static DataGridClause MakeClauseDataGridDTO(Clause cl)
        {
            var ret = new DataGridClause() {
                Id = cl.Id,
                Sound = cl.Sound,
                Word = cl.Word,
                Transcription = cl.Transcription,
                Translations = cl.Translations.Aggregate("", (s, o) => s += $"{TranslationConverter.ConvertToString(o)}; ")
                                              .TrimEnd(' ', ';'),
                Context = cl.Context,
                Relations = cl.Relations.Select(o => o.To.Word)
                                        .Distinct()
                                        .OrderBy(o => o)
                                        .Aggregate("", (s, o) => s += $"{o}; ")
                                        .TrimEnd(' ', ';'),
                HasRelations = (cl.Relations.Count > 0),
                Added = cl.Added,
                Updated = cl.Updated,
                Group = cl.Group
            };

            if(!ret.HasRelations) //There are no relations let's add the placeholder to allow user to add some
                ret.Relations = $"[{PrgResources.AddRelationPlaceholder}]";

            return ret;
        }

        /// <summary>
        /// Get all clauses that satisfy current filter (see <see cref="DDictionary.MainWindow.currentFilter"/>).
        /// </summary>
        private IEnumerable<DataGridClause> LoadData()
        {
            return dbFacade.GetClauses(currentFilter).Select(o => MakeClauseDataGridDTO(o));
        }

        /// <summary>
        /// Refill main data grid with accordance to current filter.
        /// </summary>
        /// <param name="clearSorting">Set clauses in the default order.</param>
        private void UpdateDataGrid(bool clearSorting = false)
        {
            //Only one column could have sorting
            Debug.Assert(mainDataGrid.Items.SortDescriptions.Count <= 1);

            //Remember sorting if any
            SortDescription? sorting = mainDataGrid.Items.SortDescriptions.Count > 0
                ? mainDataGrid.Items.SortDescriptions[0]
                : (SortDescription?)null;

            mainDataGrid.Items.Clear();
            
            foreach(DataGridClause item in LoadData())
                mainDataGrid.Items.Add(item);

            ClearSorting();

            if(!clearSorting && sorting.HasValue)
            { //Restore sorting
                mainDataGrid.Items.SortDescriptions.Add(sorting.Value); //Sorting itself

                mainDataGrid.Columns.First(o => o.SortMemberPath == sorting.Value.PropertyName)
                                    .SortDirection = sorting.Value.Direction; //Column header arrow

                mainDataGrid.Items.Refresh();
            }

            UpdateStatusBar();
        }

        /// <summary>
        /// Highlight main data grid's cells (in certain columns) that contain given text.
        /// </summary>
        private void HighlightCells(string substring)
        {
            foreach(DataGridClause item in mainDataGrid.Items.Cast<DataGridClause>())
            {
                updateHighlight(item.Word, (TextBlock)mainDataGridWordColumn.GetCellContent(item));
                updateHighlight(item.Translations, (TextBlock)mainDataGridTranslationsColumn.GetCellContent(item));
                updateHighlight(item.Relations, (TextBlock)mainDataGridRelationsColumn.GetCellContent(item));
                updateHighlight(item.Context, (TextBlock)mainDataGridContextColumn.GetCellContent(item));
            }


            void updateHighlight(string text, TextBlock tb)
            {
                if(tb == null)
                {
                    Debug.WriteLine($"Wrong type of the cell content in {nameof(HighlightCells)}()!");
                    return;
                }

                if(substring?.Length > 0 && text?.Contains(substring) == true)
                    tb.Background = highlightBrush;
                else
                    tb.Background = Brushes.Transparent;
            }
        }

        /// <summary>
        /// Set the default clauses order (without sorting by grid header).
        /// </summary>
        private void ClearSorting()
        {
            mainDataGrid.Items.SortDescriptions.Clear();

            foreach(DataGridColumn col in mainDataGrid.Columns)
                col.SortDirection = null;

            mainDataGrid.Items.Refresh();
        }

        private void UpdateStatusBar()
        {
            totalWordsLbl.Content = dbFacade.GetTotalClauses();
            shownWordsLbl.Content = mainDataGrid.Items.Count;
            selectedWordsLbl.Content = mainDataGrid.SelectedItems.Count;
        }

        /// <summary>
        /// Show the count of selected clauses.
        /// </summary>
        private void OnMainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedWordsLbl.Content = mainDataGrid.SelectedItems.Count;

            toGroupCBox.IsEnabled = deleteBtn.IsEnabled = mainDataGrid.SelectedItems.Count > 0;
        }

        /// <summary>
        /// Move selected words to the selected group and clear combo box selection.
        /// </summary>
        private void OnToGroupCBox_DropDownClosed(object sender, EventArgs e)
        {
            if(toGroupCBox.SelectedItem is null)
                return;

            try
            {
                var toGroup = (CheckBoxItem<WordGroup>)toGroupCBox.SelectedItem;

                if(MessageBox.Show(this,
                    String.Format(PrgResources.GroupChangeConfirmation, toGroup, mainDataGrid.SelectedItems.Count),
                    PrgResources.QuestionCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                    return;

                dbFacade.MoveClausesToGroup(toGroup.ItemValue, mainDataGrid.SelectedItems.Cast<DataGridClause>()
                                                                                         .Select(o => o.Id)
                                                                                         .ToArray());

                UpdateDataGrid();
            }
            finally { toGroupCBox.SelectedItem = null; }
        }

        /// <summary>
        /// Show all checked groups (in short form) separated by comma in group filter combo box.
        /// And update main data grid.
        /// </summary>
        private void OnGroupFilter_DropDownClosed(object sender, EventArgs e)
        {
            currentFilter.RelatedFrom = null;
            currentFilter.ShownGroups = GetFilterGroups();

            UpdateDataGrid();
            UpdateGroupFilterText();
            UpdateClearFilterButtonState();
        }

        /// <summary>
        /// Show all checked groups (in short form) separated by comma in group filter combo box.
        /// </summary>
        private void UpdateGroupFilterText()
        {
            if(currentFilter.RelatedFrom != null)
            {
                groupFilterCBox.Text = String.Format(PrgResources.RelationsFilterTextTmpl, currentFilter.RelatedFrom.Word);
                return;
            }

            WordGroup[] selected = GetFilterGroups().ToArray();

            if(selected.Length > 0)
                groupFilterCBox.Text = selected.Aggregate("", (s, o) => s += $"{o.ToGradeStr()}, ").TrimEnd(' ', ',');
            else
                groupFilterCBox.Text = "";
        }

        /// <summary>
        /// Select all rows or clear selection.
        /// </summary>
        private void OnSelectAllBtn_Clicked(object sender, RoutedEventArgs e)
        {
            if(!Equals(shownWordsLbl.Content, selectedWordsLbl.Content))
                mainDataGrid.SelectAll();
            else
                mainDataGrid.UnselectAll();
        }

        /// <summary>
        /// Apply the filter.
        /// </summary>
        private void OnTextFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFilterEdit.Background = textFilterEdit.Text?.Length > 0 ? highlightBrush 
                                                                        : Brushes.Transparent;

            if(textFilterEdit.Text == currentFilter.TextFilter)
                return;

            currentFilter.RelatedFrom = null;
            currentFilter.TextFilter = textFilterEdit.Text;

            UpdateDataGrid(clearSorting: true); //Cuz clauses in the special order after text filtration
            UpdateGroupFilterText();
            UpdateClearFilterButtonState();
        }

        /// <summary>
        /// Enable/disable "Clear filter" button depending on current filter state.
        /// </summary>
        private void UpdateClearFilterButtonState()
        {
            clearFilterBtn.IsEnabled = currentFilter.TextFilter?.Length > 0 ||
                                       currentFilter.RelatedFrom != null ||
                                       currentFilter.ShownGroups?.Any() == true;
        }

        /// <summary>
        /// Clear the filter.
        /// </summary>
        private void OnClearFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearFilter();
        }

        /// <summary>
        /// Clear the current filter and related to filtration controls.
        /// </summary>
        /// <param name="updateGrid">Refill the main data grid.</param>
        private void ClearFilter(bool updateGrid = true)
        {
            foreach(CheckBoxItem<WordGroup> item in groupFilterCBox.Items.Cast<CheckBoxItem<WordGroup>>())
                item.IsSelected = false; //Uncheck all groups in dropdown

            groupFilterCBox.Items.Refresh();

            currentFilter.Clear();

            textFilterEdit.Text = "";

            UpdateGroupFilterText();

            if(updateGrid)
                UpdateDataGrid();

            UpdateClearFilterButtonState();
        }

        /// <summary>
        /// Provide check selection on text click for combo box's items.
        /// </summary>
        private void OnGroupFilterItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var checkBox = (CheckBox)((StackPanel)sender).Children[0];

            checkBox.IsChecked = checkBox.IsChecked != true;

            e.Handled = true; //To prevent the dropdown closing
        }

        /// <summary>
        /// Keyboard handler for groupFilter combo box.
        /// </summary>
        private void OnGroupFilterCBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Space && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if(groupFilterCBox.IsDropDownOpen)
                { //Check/uncheck current item of the dropdown
                    if(groupFilterCBox.SelectedItem is CheckBoxItem<WordGroup> sel)
                    {
                        sel.IsSelected = !sel.IsSelected;

                        groupFilterCBox.Items.Refresh();

                        //To return ability of keyboard selection after refresh
                        groupFilterCBox.SelectedItem = null;
                        groupFilterCBox.SelectedItem = sel;

                        //I wish but I can't restore combo box's text cuz then keyboard focus will be lost
                    }
                }
                else
                    groupFilterCBox.IsDropDownOpen = true; //Show the dropdown by Space key
            }

            if((e.Key == Key.Up || e.Key == Key.Down) && !groupFilterCBox.IsDropDownOpen)
                UpdateGroupFilterText(); //Restore shown text IN THE CLOSED combo box after keyboard "selection"
        }

        /// <summary>
        /// Keyboard handler for toGroup combo box.
        /// </summary>
        private void OnToGroupCBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Space && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && !toGroupCBox.IsDropDownOpen)
                toGroupCBox.IsDropDownOpen = true; //Show the dropdown by Space key

            if((e.Key == Key.Up || e.Key == Key.Down) && !toGroupCBox.IsDropDownOpen)
                toGroupCBox.SelectedItem = null; //Restore shown text IN THE CLOSED combo box after keyboard "selection"
        }

        /// <summary>
        /// Handle "Show relations" column button click.
        /// </summary>
        private void ShowRelationsButton_Click(object sender, RoutedEventArgs e)
        {
            var clauseDTO = (DataGridClause)((FrameworkElement)sender).DataContext;

            if(String.IsNullOrEmpty(clauseDTO.Relations))
            {
                Debug.WriteLine($"Broken link in {nameof(ShowRelationsButton_Click)}()!");
                return;
            }

            ClearFilter(false);
            currentFilter.RelatedFrom = dbFacade.GetClauseById(clauseDTO.Id);
            
            UpdateDataGrid();
            UpdateGroupFilterText();
            UpdateClearFilterButtonState();
        }

        /// <summary>
        /// Handle play sound column button click.
        /// </summary>
        /// <seealso cref="DDictionary.MainWindow.mediaPlayer"/>
        private async void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            var ctrl = (FrameworkElement)sender;
            ctrl.IsEnabled = false; //Temporary disabled to prevent multiple clicking

            try
            {
                var clauseDTO = (DataGridClause)ctrl.DataContext;

                if(String.IsNullOrEmpty(clauseDTO.Sound))
                { //There is no sound for this clause
                    Debug.WriteLine($"Broken link in {nameof(PlaySoundButton_Click)}()!");
                    return;
                }

                try
                {
                    var source = new Uri(clauseDTO.Sound);

                    if(!source.IsFile)
                    { //Let's try to download this file
                        if(!soundsCacheFolder.Exists)
                            soundsCacheFolder.Create();

                        var localFile = new FileInfo(
                            Path.Combine(soundsCacheFolder.FullName, makeLocalFileName(clauseDTO)) );

                        if(!localFile.Exists)
                        { //It's not in the cache yet
                            using(var client = new System.Net.WebClient())
                                await client.DownloadFileTaskAsync(source, localFile.FullName);
                        }

                        source = new Uri(localFile.FullName); //Now it's path to local cached file
                    }

                    if(!File.Exists(source.AbsolutePath))
                    {
                        MessageBox.Show(this, String.Format(PrgResources.FileNotFoundError, source.AbsolutePath),
                            PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                        return;
                    }

                    //Play sound file
                    if(mediaPlayer.Source != source)
                        mediaPlayer.Open(source);

                    mediaPlayer.Stop(); //To stop previous play
                    mediaPlayer.Play();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());

                    MessageBox.Show(this, String.Format(PrgResources.CannotPlaySound, clauseDTO.Sound, ex.Message),
                        PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally { ctrl.IsEnabled = true; }


            string makeLocalFileName(DataGridClause clause)
            { //Make an unique name for the sound file cuz original ones prone to repeat each other
                return String.Concat(
                    clause.Id.ToString("x8"),                  //The new name consists of the clause id
                    clause.Sound.GetHashCode().ToString("x8"), //and the hash of the original source path.
                    Path.GetExtension(clause.Sound));          //File extension remains the same.
            }
        }

        /// <summary>
        /// Highlight the cells if necessary.
        /// </summary>
        private void OnMainDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if(currentFilter.TextFilter?.Length > 0)
            { //Highlight the cells
                mainDataGrid.UpdateLayout();
                HighlightCells(currentFilter.TextFilter);
            }
        }

        /// <summary>
        /// Main data grid's hyperlinks handler.
        /// </summary>
        private void OnMainDataGrid_HyperlinkClick(object sender, RoutedEventArgs e)
        {
            if(!(e.OriginalSource is Hyperlink hyperlink))
                return;

            var dlg = new RelationsEditDlg(((DataGridClause)hyperlink.DataContext).Id) { Owner = this };

            if(dlg.ShowDialog() == true)
                UpdateDataGrid();
        }

        /// <summary>
        /// Delete selected clauses button handler.
        /// </summary>
        private void OnDeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show(this, String.Format(PrgResources.ClausesDeletionConfirmation, mainDataGrid.SelectedItems.Count), 
                PrgResources.QuestionCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            dbFacade.RemoveClauses(mainDataGrid.SelectedItems.Cast<DataGridClause>()
                                                             .Select(o => o.Id)
                                                             .ToArray());

            UpdateDataGrid();
        }
    }
}
