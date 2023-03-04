using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DDictionary.Domain;
using DDictionary.Domain.DTO;
using DDictionary.Domain.Entities;

using PrgSettings = DDictionary.Properties.Settings;

namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for HistoryDlg.xaml
    /// </summary>
    public partial class HistoryDlg : Window
    {
        #region Internal types

        private sealed class HistoryTableClause
        {
            public int ClauseId { get; set; }
            public DateTime DateTime { get; set; }
            public string Word { get; set; }
            public WordGroup WordGroup { get; set; }
            public AsteriskType? WordAsterisk { get; set; }
            public string Success { get; set; }
            public TestType TestType { get; set; }
        }

        #endregion


        /// <summary>The object to work with data storage.</summary>
        protected IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;


        public HistoryDlg()
        {
            InitializeComponent();
            ApplyGUIScale();

            UpdateDataGridAsync().GetAwaiter().GetResult();
        }


        private void ApplyGUIScale()
        {
            double guiScale = PrgSettings.Default.DialogsScale;

            historyDataGrid.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;
        }

        /// <summary>
        /// Edit clause button handler.
        /// </summary>
        private async void OnDataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(sender is DataGridRow row && row.DataContext is HistoryTableClause);

            await EditClause(((sender as DataGridRow).DataContext as HistoryTableClause).ClauseId);
        }

        private async Task EditClause(int clauseId)
        {
            var lst = historyDataGrid.Items.Cast<HistoryTableClause>()
                                           .Select(o => o.ClauseId)
                                           .Distinct()
                                           .ToList();

            var dlg = new ClauseEditDlg(clauseId, lst) { Owner = this };
            dlg.ClausesWereUpdated += async () => await UpdateDataGridAsync();

            await Task.Delay(150); //To prevent mouse event from catching in the just opened dialog

            dlg.ShowDialog();
        }

        /// <summary>
        /// Refill data grid.
        /// </summary>
        private async Task UpdateDataGridAsync()
        {
            historyDataGrid.Items.Clear();

            IEnumerable<TrainingHistoryEntryDTO> items = await dbFacade.GetTrainingHistoryAsync(150);

            foreach (TrainingHistoryEntryDTO item in items)
            {
                historyDataGrid.Items.Add(new HistoryTableClause {
                    ClauseId = item.ClauseId,
                    DateTime = item.TrainingDate,
                    Word = item.Word,
                    WordGroup = item.Group,
                    WordAsterisk = item.Type,
                    Success = (item.Success ? "✓" : "✖"), //✓✔✕✖
                    TestType = item.TrainingType
                });
            }
        }
    }
}
