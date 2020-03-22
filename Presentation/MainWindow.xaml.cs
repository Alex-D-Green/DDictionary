using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using CsvHelper;

using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;

using Microsoft.Win32;

using PrgResources = DDictionary.Properties.Resources;
using PrgSettings = DDictionary.Properties.Settings;


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


        /// <summary>
        /// Cancellation token source to cancel an update fallowed by changes of the text filter.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.MainWindow.OnTextFilter_TextChanged"/>
        private CancellationTokenSource textFilterUpdateCancellation;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>The brush to highlight cells.</summary>
        private Brush highlightBrush { get; }


        public MainWindow()
        {
            #region Upgrade application settings if needed

            if(Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            #endregion

            dbFacade.OnErrorOccurs += (Exception e, ref bool handled) =>
            { //The DAL errors handler for the whole GUI
                if(handled)
                    return;

                Debug.WriteLine(e.ToString());

                Dispatcher.Invoke(() => //In case if it's not the GUI thread
                    MessageBox.Show(this, e.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error));
            };

            InitializeComponent();

            highlightBrush = mainDataGrid.Resources["HighlightBrush"] as Brush ??
                new SolidColorBrush(Color.FromRgb(0xFD, 0xC4, 0x4D));

            #region ComboBoxes with groups initialization

            foreach(WordGroup gr in Enum.GetValues(typeof(WordGroup)).Cast<WordGroup>().OrderByDescending(o => o))
            {
                groupFilterCBox.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });
                toGroupCBox.Items.Add(new CheckBoxItem<WordGroup> { Text = gr.ToFullStr(), ItemValue = gr });
            }

            UpdateGroupFilterText();

            #endregion
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Restore window's state
            if(PrgSettings.Default.Maximized)
            {
                ScreenInfo screen =
                    ScreenInfo.AllScreens().SingleOrDefault(o => o.DeviceName == PrgSettings.Default.ScreenName);

                if(screen != null)
                { //Move the window on the right screen before maximize it
                    Rect workArea = ScreenInfo.GetRectInDpi(this, screen.WorkingAreaPix);

                    Left = workArea.Left;
                    Top = workArea.Top;
                }

                WindowState = WindowState.Maximized;
            }
            else
            {
                Left = PrgSettings.Default.Left;
                Top = PrgSettings.Default.Top;
                Width = PrgSettings.Default.Width;
                Height = PrgSettings.Default.Height;
            }

            if(!String.IsNullOrEmpty(PrgSettings.Default.Columns))
            {
                try
                {
                    //Restore the columns, there is an issue with that see 
                    //https://www.telerik.com/forums/reordering-columns-issue-once-again#A-r5p46x-k-Je0JMRTCYeA

                    var columnsData = PrgSettings.Default.Columns.Split(';').Select((s, i) =>
                    {
                        string[] values = s.Split(',');

                        return new {
                            Index = i,
                            DisplayIndex = Int32.Parse(values[0]),
                            Width = Double.Parse(values[1])
                        };
                    });

                    foreach(var data in columnsData.OrderBy(o => o.DisplayIndex))
                    {
                        mainDataGrid.Columns[data.Index].DisplayIndex = data.DisplayIndex;
                        mainDataGrid.Columns[data.Index].Width =
                            new DataGridLength(data.Width, DataGridLengthUnitType.Pixel);
                    }
                }
                catch(Exception ex)
                {
#if DEBUG
                    throw;
#endif
                    Debug.WriteLine(ex.ToString());
                    PrgSettings.Default.Columns = ""; //To clear corrupted value
                }
            }


            //Restore browser context's state
            if(PrgSettings.Default.SaveContext)
            { 
                if(PrgSettings.Default.FilterRelatedFrom > 0)
                { //Relation filtration
                    try 
                    { 
                        currentFilter.RelatedFrom = dbFacade.GetClauseByIdAsync(PrgSettings.Default.FilterRelatedFrom).Result; 
                    }
                    catch(Exception ex)
                    {
#if DEBUG
                        throw;
#endif
                        Debug.WriteLine(ex.ToString());
                        PrgSettings.Default.FilterRelatedFrom = 0; //To clear corrupted value
                    }
                }

                //Text filtration
                textFilterEdit.Text = currentFilter.TextFilter = PrgSettings.Default.FilterText;

                textFilterEdit.Background = textFilterEdit.Text?.Length > 0 ? highlightBrush
                                                                            : Brushes.Transparent;

                //Groups filtration
                if(!String.IsNullOrEmpty(PrgSettings.Default.FilterGroups))
                {
                    try
                    {
                        currentFilter.ShownGroups = PrgSettings.Default.FilterGroups.Split(',')
                            .Select(o => WordGroupTranslator.FromGradeStr(o)).ToArray();

                        foreach(var item in groupFilterCBox.Items.Cast<CheckBoxItem<WordGroup>>())
                            item.IsSelected = currentFilter.ShownGroups.Contains(item.ItemValue);
                    }
                    catch(Exception ex)
                    {
#if DEBUG
                        throw;
#endif
                        Debug.WriteLine(ex.ToString());
                        PrgSettings.Default.FilterGroups = ""; //To clear corrupted value
                    }
                }

                UpdateGroupFilterText();

                if(!String.IsNullOrEmpty(PrgSettings.Default.Sorting))
                { //Restore sorting
                    try
                    {
                        string[] data = PrgSettings.Default.Sorting.Split(',');

                        var sorting = new SortDescription(data[0], (ListSortDirection)Int32.Parse(data[1]));

                        mainDataGrid.Items.SortDescriptions.Add(sorting);

                        mainDataGrid.Columns.First(o => o.SortMemberPath == sorting.PropertyName)
                                            .SortDirection = sorting.Direction; //Column header arrow
                    }
                    catch(Exception ex)
                    {
#if DEBUG
                        throw;
#endif
                        Debug.WriteLine(ex.ToString());
                        PrgSettings.Default.Sorting = ""; //To clear corrupted value
                    }
                }
            }

            //The first datagrid update
            UpdateDataGridAsync().Wait();
        }

        /// <summary>
        /// Get the list of checked groups for clauses' filtration.
        /// </summary>
        private IEnumerable<WordGroup> GetFilterGroups()
        {
            return groupFilterCBox.Items.Cast<CheckBoxItem<WordGroup>>().Where(o => o.IsSelected).Select(o => o.ItemValue);
        }

        /// <summary>
        /// Get all clauses that satisfy current filter (see <see cref="DDictionary.MainWindow.currentFilter"/>).
        /// </summary>
        private async Task<IEnumerable<DataGridClause>> LoadDataAsync(CancellationToken cancellationToken = default)
        {
            return (await dbFacade.GetClausesAsync(currentFilter, cancellationToken)).Select(o => o.MapToDataGridClause());
        }

        /// <summary>
        /// Refill main data grid with accordance to current filter.
        /// </summary>
        /// <remarks>The method use GUI Dispatcher inside so it could be called from outside of the GUI thread.
        /// </remarks>
        /// <param name="clearSorting">Set the clauses in the default order.</param>
        private async Task UpdateDataGridAsync(bool clearSorting = false, CancellationToken cancellationToken = default)
        {
            SortDescription? sorting = null;

            Dispatcher.Invoke(() =>
            {
                //Only one column could have sorting
                Debug.Assert(mainDataGrid.Items.SortDescriptions.Count <= 1);

                //Remember sorting if any
                sorting = mainDataGrid.Items.SortDescriptions.Count > 0
                    ? mainDataGrid.Items.SortDescriptions[0]
                    : (SortDescription?)null;

                //Clear sorting
                mainDataGrid.Items.SortDescriptions.Clear();

                foreach(DataGridColumn col in mainDataGrid.Columns)
                    col.SortDirection = null;
            });

            //Data loading is accomplished outside of the GUI thread (if the whole method was called from outside)
            IEnumerable<DataGridClause> data = await LoadDataAsync(cancellationToken);

            Dispatcher.Invoke(() =>
            {
                //Update items
                mainDataGrid.ItemsSource = data;

                if(!clearSorting && sorting.HasValue)
                { //Restore sorting
                    mainDataGrid.Items.SortDescriptions.Add(sorting.Value); //Sorting itself

                    mainDataGrid.Columns.First(o => o.SortMemberPath == sorting.Value.PropertyName)
                                        .SortDirection = sorting.Value.Direction; //Column header arrow

                    mainDataGrid.Items.Refresh();
                }

                exportBtn.IsEnabled = mainDataGrid.Items.Count > 0;

                UpdateStatusBar();
            });
        }

        /// <summary>
        /// Highlight main data grid's cells (in certain columns) that contain given text.
        /// </summary>
        private void HighlightCells(string substring)
        {
            Regex regEx = null;

            foreach(DataGridClause item in mainDataGrid.Items.Cast<DataGridClause>())
            {
                updateHighlight(item.Word, (TextBlock)mainDataGridWordColumn.GetCellContent(item));
                updateHighlight(item.Translations, (TextBlock)mainDataGridTranslationsColumn.GetCellContent(item));
                updateHighlight(item.Relations, (TextBlock)mainDataGridRelationsColumn.GetCellContent(item));
                updateHighlight(item.Context, (TextBlock)mainDataGridContextColumn.GetCellContent(item));
            }


            void updateHighlight(string text, TextBlock tb)
            {
                if(tb is null) 
                    return;

                if(String.IsNullOrEmpty(text) || String.IsNullOrEmpty(substring))
                {
                    tb.Background = Brushes.Transparent;
                    return;
                }

                if(regEx is null)
                {
                    regEx = new Regex(substring
                                          .Replace(".", "\\.") //To handle dots as a regular symbol
                                          .Replace("_", "."),  //To handle underscores as the placeholder
                                      RegexOptions.IgnoreCase);
                }

                if(regEx.IsMatch(text))
                    tb.Background = highlightBrush;
                else
                    tb.Background = Brushes.Transparent;
            }
        }

        private void UpdateStatusBar()
        {
            totalWordsLbl.Content = dbFacade.GetTotalClausesAsync().Result;
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
        private async void OnToGroupCBox_DropDownClosed(object sender, EventArgs e)
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

                await dbFacade.MoveClausesToGroupAsync(toGroup.ItemValue, mainDataGrid.SelectedItems.Cast<DataGridClause>()
                                                                                                    .Select(o => o.Id)
                                                                                                    .ToArray());

                await UpdateDataGridAsync();
            }
            finally { toGroupCBox.SelectedItem = null; }
        }

        /// <summary>
        /// Show all checked groups (in short form) separated by comma in group filter combo box.
        /// And update main data grid.
        /// </summary>
        private async void OnGroupFilter_DropDownClosed(object sender, EventArgs e)
        {
            currentFilter.RelatedFrom = null;
            currentFilter.ShownGroups = GetFilterGroups();

            await UpdateDataGridAsync();
            UpdateGroupFilterText();
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
        /// <seealso cref="DDictionary.Presentation.MainWindow.textFilterUpdateCancellation"/>
        private void OnTextFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFilterEdit.Background = textFilterEdit.Text?.Length > 0 ? highlightBrush 
                                                                        : Brushes.Transparent;

            if(textFilterEdit.Text == currentFilter.TextFilter)
                return;

            currentFilter.RelatedFrom = null;
            currentFilter.TextFilter = textFilterEdit.Text;

            textFilterUpdateCancellation?.Cancel(); //Cancel the previous task if any
            textFilterUpdateCancellation = new CancellationTokenSource(); //To get a token for a new task

            //Cuz clauses in the special order after text filtration

            //Start a separate task to free the GUI thread as soon as possible
            new Task(async () =>
            {
                try
                {
                    CancellationToken token = textFilterUpdateCancellation.Token;

                    //Start a delay task which could be canceled to reduce impact on the DB and GUI.
                    //If a new input will be done in 500 ms then the whole task will be canceled without
                    //bothering DB & GUI at all.
                    await Task.Delay(500, token);

                    await UpdateDataGridAsync(clearSorting: true, token); //Can be canceled on this stage as well
                    
                    Dispatcher.Invoke(() => UpdateGroupFilterText());
                }
                catch(TaskCanceledException) //The task was canceled, there is nothing to do about it
                { }
            }, TaskCreationOptions.LongRunning) //To encourage the using of a separate thread
            .Start(); 
        }

        /// <summary>
        /// Clear the filter/sorting.
        /// </summary>
        private void OnClearFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Alt)
                ClearSorting();
            else
                ClearFilter();
        }

        /// <summary>
        /// Clear the sorting.
        /// </summary>
        private void ClearSorting()
        {
            //Only one column could have sorting
            Debug.Assert(mainDataGrid.Items.SortDescriptions.Count <= 1);

            //Clear sorting
            mainDataGrid.Items.SortDescriptions.Clear();

            foreach(DataGridColumn col in mainDataGrid.Columns)
                col.SortDirection = null;
        }

        /// <summary>
        /// Clear the current filter and related to filtration controls.
        /// </summary>
        /// <param name="updateGrid">Refill the main data grid.</param>
        private async void ClearFilter(bool updateGrid = true)
        {
            foreach(CheckBoxItem<WordGroup> item in groupFilterCBox.Items.Cast<CheckBoxItem<WordGroup>>())
                item.IsSelected = false; //Uncheck all groups in dropdown

            groupFilterCBox.Items.Refresh();

            currentFilter.Clear();

            textFilterEdit.Text = "";

            UpdateGroupFilterText();

            if(updateGrid)
                await UpdateDataGridAsync();
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
        private async void ShowRelationsButton_Click(object sender, RoutedEventArgs e)
        {
            var clauseDTO = (DataGridClause)((FrameworkElement)sender).DataContext;

            if(String.IsNullOrEmpty(clauseDTO.Relations))
            {
                Debug.WriteLine($"Broken link in {nameof(ShowRelationsButton_Click)}()!");
                return;
            }

            ClearFilter(false);
            currentFilter.RelatedFrom = await dbFacade.GetClauseByIdAsync(clauseDTO.Id);
            
            await UpdateDataGridAsync();
            UpdateGroupFilterText();
        }

        /// <summary>
        /// Handle play sound column button click.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.SoundManager"/>
        private async void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            var ctrl = (FrameworkElement)sender;
            ctrl.IsEnabled = false; //Temporary disabled to prevent multiple clicking

            try 
            {
                var clauseDTO = (DataGridClause)ctrl.DataContext;

                await SoundManager.PlaySoundAsync(clauseDTO.Id, clauseDTO.Sound); 
            }
            catch(IOException ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { ctrl.IsEnabled = true; }
        }

        /// <summary>
        /// Highlight the cells if necessary.
        /// </summary>
        private void OnMainDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if(currentFilter.TextFilter?.Length > 0)
                HighlightCells(currentFilter.TextFilter);
        }

        /// <summary>
        /// Main data grid's hyperlinks handler.
        /// </summary>
        private async void OnMainDataGrid_HyperlinkClick(object sender, RoutedEventArgs e)
        {
            if(!(e.OriginalSource is Hyperlink hyperlink))
                return;

            Clause cl = await dbFacade.GetClauseByIdAsync(((DataGridClause)hyperlink.DataContext).Id);
            RelationDTO[] relLst = cl.Relations.Select(o => o.MapToRelationDTO()).ToArray();

            var dlg = new RelationsEditDlg(cl.Id, cl.Word, relLst) { Owner = this };

            if(dlg.ShowDialog() == true)
            {
                await dbFacade.RemoveRelationsAsync(cl.Relations.Select(o => o.Id)
                                                                .Except(dlg.Relations.Select(o => o.Id))
                                                                .ToArray());

                foreach(RelationDTO rel in dlg.Relations.Where(o => o.Id == 0 || o.DescriptionWasChanged))
                {
                    await dbFacade.AddOrUpdateRelationAsync(rel.Id, cl.Id, rel.ToWordId, rel.Description);

                    if(rel.MakeInterconnected)
                    { //Add relation to the other side
                        Debug.Assert(rel.Id == 0);

                        await dbFacade.AddOrUpdateRelationAsync(0, rel.ToWordId, cl.Id, rel.Description);
                    }
                }

                await UpdateDataGridAsync();
            }
        }

        /// <summary>
        /// Delete selected clauses button handler.
        /// </summary>
        private async void OnDeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show(this, String.Format(PrgResources.ClausesDeletionConfirmation, mainDataGrid.SelectedItems.Count), 
                PrgResources.QuestionCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            await dbFacade.RemoveClausesAsync(mainDataGrid.SelectedItems.Cast<DataGridClause>()
                                                                        .Select(o => o.Id)
                                                                        .ToArray());

            await UpdateDataGridAsync();
        }

        /// <summary>
        /// Add new clause button handler.
        /// </summary>
        private async void OnAddWordButton_Click(object sender, RoutedEventArgs e)
        {
            var lst = mainDataGrid.Items.Cast<DataGridClause>().Select(o => o.Id).ToList();

            var dlg = new ClauseEditDlg(null, lst) { Owner = this };
            dlg.ClausesWereUpdated += async () => await UpdateDataGridAsync();

            dlg.ShowDialog();
            
            await UpdateDataGridAsync(); //Clause's last watch data can be changed...
        }

        /// <summary>
        /// Edit clause button handler.
        /// </summary>
        private async void OnDataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(sender is DataGridRow row && row.DataContext is DataGridClause);

            var lst = mainDataGrid.Items.Cast<DataGridClause>().Select(o => o.Id).ToList();

            var dlg = new ClauseEditDlg(((sender as DataGridRow).DataContext as DataGridClause).Id, lst) { Owner = this };
            dlg.ClausesWereUpdated += async () => await UpdateDataGridAsync();

            await Task.Delay(150); //To prevent mouse event from catching in the just opened dialog

            dlg.ShowDialog();
            
            await UpdateDataGridAsync(); //Clause's last watch data can be changed...
        }


        /// <summary>
        /// Export button handler.
        /// </summary>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private async void OnExportBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!currentFilter.Empty &&
               MessageBox.Show(this, PrgResources.FilterIsActivated, PrgResources.QuestionCaption,
                   MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                return;

            var dlg = new SaveFileDialog
            {
                OverwritePrompt = true,
                DefaultExt = "csv",
                Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"{DateTime.Now.ToString("yyyy-MM-dd")}.csv"
            };

            if(dlg.ShowDialog() != true)
                return;

            try
            {
                exportBtn.IsEnabled = false; //To prevent simultaneous saving

                using(var writer = new StreamWriter(dlg.FileName))
                using(var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    await csv.WriteRecordsAsync(
                        (await dbFacade.GetClausesAsync(currentFilter)).Select(o => o.MapToCsvClause()) );

                MessageBox.Show(this, dlg.FileName, PrgResources.FileWasSuccessivelySaved,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(Exception ex)
            { MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            { exportBtn.IsEnabled = mainDataGrid.Items.Count > 0; }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            //Save window's state
            bool max = PrgSettings.Default.Maximized = (WindowState == WindowState.Maximized);
            PrgSettings.Default.Left = max ? 0 : Left;
            PrgSettings.Default.Top = max ? 0 : Top;
            PrgSettings.Default.Width = max ? 0 : ActualWidth;
            PrgSettings.Default.Height = max ? 0 : ActualHeight;
            PrgSettings.Default.ScreenName = ScreenInfo.GetScreenFrom(this).DeviceName;

            PrgSettings.Default.Columns = 
                mainDataGrid.Columns.Aggregate("", (s, o) => s += $"{o.DisplayIndex},{o.ActualWidth};").TrimEnd(';');

            //Save browser context's state
            if(PrgSettings.Default.SaveContext)
            {
                PrgSettings.Default.FilterRelatedFrom = currentFilter.RelatedFrom?.Id ?? 0;
                PrgSettings.Default.FilterText = currentFilter.TextFilter ?? "";
                PrgSettings.Default.FilterGroups =
                    currentFilter.ShownGroups?.Aggregate("", (s, o) => s += $"{o.ToGradeStr()},").TrimEnd(',') ?? "";

                if(mainDataGrid.Items.SortDescriptions.Count > 0)
                {
                    SortDescription sorting = mainDataGrid.Items.SortDescriptions[0];

                    PrgSettings.Default.Sorting = $"{sorting.PropertyName},{(int)sorting.Direction}";
                }
                else
                    PrgSettings.Default.Sorting = "";
            }
            else
            {
                PrgSettings.Default.FilterRelatedFrom = 0;
                PrgSettings.Default.FilterText = "";
                PrgSettings.Default.FilterGroups = "";
                PrgSettings.Default.Sorting = "";
            }

            PrgSettings.Default.Save();
        }

        private void OnWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if( !textFilterEdit.IsFocused && Keyboard.Modifiers == ModifierKeys.None && 
                ( (e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.D0 && e.Key <= Key.D9) || 
                  (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ) )
            {
                textFilterEdit.Focus();
                textFilterEdit.SelectAll();
            }
        }

        #region Commands' handlers

        private void OnExitCommand(object sender, ExecutedRoutedEventArgs e) 
        {
            Application.Current.Shutdown();
        }

        private void OnSettingsCommand(object sender, ExecutedRoutedEventArgs e)
        {
            new SettingsDlg() { Owner = this }.ShowDialog();
        }

        private void OnGoToSiteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start("https://github.com/Alex-D-Green/DDictionary");
        }

        private void OnOnlineHelpCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start("https://github.com/Alex-D-Green/DDictionary/wiki");
        }

        private void OnAboutCommand(object sender, ExecutedRoutedEventArgs e)
        {
            new AboutProgramDlg() { Owner = this }.ShowDialog();
        }

        private async void OnCreateMultirelationCommand(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridClause[] selectedClauses = mainDataGrid.SelectedItems.Cast<DataGridClause>().ToArray();

            if(new MultirelationCreateDlg(selectedClauses) { Owner = this }.ShowDialog() == true)
                await UpdateDataGridAsync();
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if(e.Command == UICommands.CreateMultirelationCommand)
                e.CanExecute = (mainDataGrid.SelectedItems.Count >= 2 && mainDataGrid.SelectedItems.Count <= 7);
        }

        #endregion
    }
}
