using System;
using System.Collections.Generic;
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

            #region Group filter initialization

            foreach(WordGroup gr in Enum.GetValues(typeof(WordGroup)).Cast<WordGroup>().OrderByDescending(o => o))
                groupFilter.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });

            UpdateGroupFilterText();

            #endregion

            UpdateDataGrid();

            textFilter.Focus();
        }

        /// <summary>
        /// Get the list of checked groups for clauses' filtration.
        /// </summary>
        private IEnumerable<WordGroup> GetFilterGroups()
        {
            return groupFilter.Items.Cast<CheckBoxItem<WordGroup>>().Where(o => o.IsSelected).Select(o => o.ItemValue);
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
                Group = cl.Group.ToGradeStr()
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
        private void UpdateDataGrid()
        {
            mainDataGrid.Items.Clear();

            foreach(DataGridClause item in LoadData())
                mainDataGrid.Items.Add(item);

            ClearSorting();

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
        }

        /// <summary>
        /// Show all checked groups (in short form) separated by comma in group filter combo box.
        /// </summary>
        private void UpdateGroupFilterText()
        {
            if(currentFilter.RelatedFrom != null)
            {
                groupFilter.Text = String.Format(PrgResources.RelationsFilterTextTmpl, currentFilter.RelatedFrom.Word);
                return;
            }

            WordGroup[] selected = GetFilterGroups().ToArray();

            if(selected.Length > 0)
                groupFilter.Text = selected.Aggregate("", (s, o) => s += $"{o.ToGradeStr()}, ").TrimEnd(' ', ',');
            else
                groupFilter.Text = "";
        }

        /// <summary>
        /// Select all rows or clear selection.
        /// </summary>
        private void OnSelectAllButton_Clicked(object sender, RoutedEventArgs e)
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
            textFilter.Background = textFilter.Text?.Length > 0 ? highlightBrush 
                                                                : Brushes.Transparent;

            if(textFilter.Text == currentFilter.TextFilter)
                return;

            currentFilter.RelatedFrom = null;
            currentFilter.TextFilter = textFilter.Text;

            UpdateDataGrid();
            UpdateGroupFilterText();
        }

        /// <summary>
        /// Clear the filter.
        /// </summary>
        private void OnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            ClearFilter();
        }

        /// <summary>
        /// Clear the current filter and related to filtration controls.
        /// </summary>
        /// <param name="updateGrid">Refill the main data grid.</param>
        private void ClearFilter(bool updateGrid = true)
        {
            foreach(CheckBoxItem<WordGroup> item in groupFilter.Items.Cast<CheckBoxItem<WordGroup>>())
                item.IsSelected = false; //Uncheck all groups in dropdown

            groupFilter.Items.Refresh();

            currentFilter.Clear();

            textFilter.Text = "";

            UpdateGroupFilterText();

            if(updateGrid)
                UpdateDataGrid();
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
        private void GroupFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Space && 
                (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
            {
                if(groupFilter.IsDropDownOpen)
                { //Check/uncheck current item of the dropdown
                    if(groupFilter.SelectedItem is CheckBoxItem<WordGroup> sel)
                    {
                        sel.IsSelected = !sel.IsSelected;

                        groupFilter.Items.Refresh();

                        //To return ability of keyboard selection after refresh
                        groupFilter.SelectedItem = null;
                        groupFilter.SelectedItem = sel;

                        //I wish but I can't restore combo box's text cuz then keyboard focus will be lost
                    }
                }
                else
                    groupFilter.IsDropDownOpen = true; //Show the dropdown by Space key
            }

            if((e.Key == Key.Up || e.Key == Key.Down) && !groupFilter.IsDropDownOpen)
                UpdateGroupFilterText(); //Restore shown text IN THE CLOSED combo box after keyboard "selection"
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
        /// Main data grid's buttons/hyperlinks handler.
        /// </summary>
        private void OnMainDataGrid_Click(object sender, RoutedEventArgs e)
        {
            if(!(e.OriginalSource is Hyperlink hyperlink))
                return;

            var dlg = new RelationsEditDlg(((DataGridClause)hyperlink.DataContext).Id) { Owner = this };

            if(dlg.ShowDialog() == true)
                UpdateDataGrid();
        }

        ////The example of highlightening a part of the text
        //private void HighlightText(TextBlock tb)
        //{
        //    var regex = new Regex("(" + textFilter.Text + ")", RegexOptions.IgnoreCase);

        //    if(textFilter.Text.Length == 0)
        //    {
        //        string str = tb.Text;
        //        tb.Inlines.Clear();
        //        tb.Inlines.Add(str);
        //        return;
        //    }

        //    string[] substrings = regex.Split(tb.Text);
        //    tb.Inlines.Clear();

        //    foreach(string str in substrings)
        //    {
        //        if(regex.Match(str).Success)
        //        {
        //            var runx = new Run(str);
        //            runx.Background = Brushes.Yellow;
        //            tb.Inlines.Add(runx);
        //        }
        //        else
        //            tb.Inlines.Add(str);
        //    }
        //}
    }
}
