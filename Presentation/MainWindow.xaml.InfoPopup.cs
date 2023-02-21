using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DDictionary.Presentation.Converters;
using DDictionary.Presentation.ViewModels;

using PrgSettings = DDictionary.Properties.Settings;


namespace DDictionary.Presentation
{
    //The logic of handling InfoPopup window.
    partial class MainWindow
    {
        /// <summary>The cancellation token source to cancel pop-up showing task.</summary>
        private CancellationTokenSource showInfoPopupTaskToken;

        /// <summary>The cell over which the showing of info pop-up was initiated.</summary>
        private DataGridCell infoPopupPivotCell;

        /// <summary>The reference to the last shown info pop-up.</summary>
        private InfoPopup infoPopupWindow;


        /// <summary>
        /// Main data grid cells' hover handler (show info pop-up with the delay).
        /// </summary>
        private void OnMainDataGridCell_MouseEnter(object sender, MouseEventArgs e)
        {
            Debug.Assert(sender is DataGridCell);

            var newPivotCell = (DataGridCell)sender;

            bool shouldNotAppear = newPivotCell.Column == mainDataGridRelationsColumn ||
                                   newPivotCell.Column == mainDataGridShowRelationsColumn ||
                                   newPivotCell.Column == mainDataGridPlayColumn ||
                                   Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ||
                                   PrgSettings.Default.ShowInfoPopup == false ||
                                   textFilterEdit.IsFocused;

            if(shouldNotAppear || 
               infoPopupWindow != null) //The mark that pop-up PROBABLY was closed by itself
            {
                HideInfoPopup(); //To dispose resources

                if(shouldNotAppear || infoPopupPivotCell == newPivotCell)
                { //If it's the same cell then the window most likely was indeed closed by itself (and shouldn't reappear) 
                  //otherwise we close old window (see HideInfoPopup() above) and right away show the new one (see below).
                    infoPopupPivotCell = null;
                    return;
                }
            }

            infoPopupPivotCell = newPivotCell;
            StartCountdownForInfoPopup((DataGridClause)newPivotCell.DataContext);
        }

        /// <summary>
        /// Trace mouse moving over main data grid to hide info pop-up when mouse is outside of the pivot cell.
        /// </summary>
        private void OnMainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if(infoPopupPivotCell?.IsMouseOver != true)
                HideInfoPopup();
        }

        /// <summary>
        /// Prevent pop-up if any mouse button was pressed.
        /// </summary>
        private void OnDataGridRow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            HideInfoPopup();
        }

        /// <summary>
        /// Show info pop-up with the certain delay.
        /// </summary>
        /// <param name="clause">The clause which info should be shown.</param>
        /// <returns>The task which will show the pop-up.</returns>
        private Task StartCountdownForInfoPopup(DataGridClause clause)
        {
            HideInfoPopup();

            var cts = new CancellationTokenSource();

            var ret = Task.Delay(1000, cts.Token)
                          .ContinueWith(delayTask =>
                          {
                              if(!delayTask.IsCanceled && infoPopupPivotCell?.IsMouseOver == true)
                                  Dispatcher.Invoke(() => ShowInfoPopup(clause));
                          });

            showInfoPopupTaskToken = cts; //To make it thread safe
            return ret;
        }

        /// <summary>
        /// Show info pop-up and position it.
        /// </summary>
        private async void ShowInfoPopup(DataGridClause clause)
        {
            Rect workArea = ScreenInfo.GetRectInDpi(this, ScreenInfo.GetScreenFrom(this).WorkingAreaPix);

            var popup = new InfoPopup(clause) { Owner = this };

            //Calculate shifts to position the pop-up relative to the main window and its screen

            double addX = (WindowState == WindowState.Normal)
                ? Left +
                    SystemParameters.WindowResizeBorderThickness.Left +
                    SystemParameters.WindowNonClientFrameThickness.Left
                : workArea.Left;

            double addY = (WindowState == WindowState.Normal)
                ? Top +
                    SystemParameters.WindowResizeBorderThickness.Top +
                    SystemParameters.WindowNonClientFrameThickness.Top
                : workArea.Top + SystemParameters.WindowCaptionHeight;


            popup.Show(); //Here, to get actual window size


            //Position the window

            popup.Left = Mouse.GetPosition(this).X - popup.Width/2 + addX;
            popup.Top = Mouse.GetPosition(this).Y - popup.Height + 5 + addY;


            //Handling of falling the window out of bounds of the screen

            if(popup.Left < workArea.Left)
                popup.Left = workArea.Left;
            else if((popup.Left + popup.Width) > workArea.Right)
                popup.Left = workArea.Right - popup.Width;

            if(popup.Top < workArea.Top)
                popup.Top = workArea.Top;
            else if((popup.Top + popup.Height) > workArea.Bottom)
                popup.Top = workArea.Bottom - popup.Height;

            infoPopupWindow = popup; //To make it thread safe


            if(PrgSettings.Default.AutoplaySound && !String.IsNullOrEmpty(clause.Sound))
            {
                try { await SoundManager.PlaySoundAsync(clause.Id, clause.Sound, dbFacade.DataSource); }
                catch(FileNotFoundException) { }
            }

            //Update clause's watch data
            if(clause.Watched.Date == DateTime.Now.Date)
                return; //Increment only once a day
            
            await dbFacade.UpdateClauseWatchAsync(clause.Id);

            DataGridClause updated = (await dbFacade.GetClauseByIdAsync(clause.Id)).MapToDataGridClause(currentFilter);

            clause.Watched = updated.Watched;
            clause.WatchedCount = updated.WatchedCount;
            
            mainDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Hide info pop-up or cancel the task if it wasn't finished yet (prevent pop-up showing).
        /// </summary>
        private void HideInfoPopup()
        {
            showInfoPopupTaskToken?.Cancel();
            showInfoPopupTaskToken?.Dispose();
            showInfoPopupTaskToken = null;

            infoPopupWindow?.Close();
            infoPopupWindow = null;
        }
    }
}
