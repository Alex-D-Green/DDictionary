using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DDictionary.Domain;
using DDictionary.Domain.Entities;
using DDictionary.Presentation.Converters;

using PrgSettings = DDictionary.Properties.Settings;

namespace DDictionary.Presentation.Testing
{
    /// <summary>
    /// Interaction logic for StatisticsDlg.xaml
    /// </summary>
    public partial class StatisticsDlg: Window
    {
        #region Internal types

        private sealed class TrainingTableClause
        {
            public int TestRuns { get; set; }
            public string TestTypeName { get; set; }
            public object Success { get; set; }
            public object Fail { get; set; }
            public object Total { get; set; }
            public string Percent { get; set; }
            public int? PercentSort { get; set; }
            public object LastTraining { get; set; }
            public TestType TestType { get; set; }
        }

        #endregion


        /// <summary>The object to work with data storage.</summary>
        protected IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>Command on start a certain training after this dialog closing.</summary>
        public TestType? StartTraining { get; private set; }


        public StatisticsDlg()
        {
            InitializeComponent();
            ApplyGUIScale();

            List<TrainingStatistic> total = dbFacade.GetGeneralTrainingStatisticsAsync().Result.ToList();

            DateTime lastStatDate = DateTime.Now.AddMonths(-1);

            List<ShortTrainingStatistic> lastStat =
                dbFacade.GetGeneralTrainingStatisticsAsync(lastStatDate).Result.ToList();

            if(total.Count == 0)
            {
                MessageBox.Show(this, Properties.Resources.NoTrainingStatYet, Properties.Resources.InformationCaption,
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            foreach(TestType testType in Enum.GetValues(typeof(TestType)).Cast<TestType>())
            {
                int? per = null;

                TrainingStatistic rowT = total.FirstOrDefault(o => o.TestType == testType);
                if(rowT != null)
                {
                    per = calcPercent(rowT.Success, rowT.Fail);

                    statDataGrid.Items.Add(new TrainingTableClause {
                        TestRuns = TestDlgBase.GetTestRunsCounter(rowT.TestType),
                        TestTypeName = rowT.TestType.ToFullString(),
                        Success = rowT.Success,
                        Fail = rowT.Fail,
                        Total = (rowT.Success + rowT.Fail),
                        Percent = per.HasValue ? $"{per} %" : "-",
                        PercentSort = per, //For sorting only
                        LastTraining = rowT.LastTraining,
                        TestType = rowT.TestType
                    });
                }
                else
                    statDataGrid.Items.Add(new TrainingTableClause {
                        TestTypeName = testType.ToFullString(),
                        Success = "-",
                        Fail = "-",
                        Total = "-",
                        Percent = "-",
                        LastTraining = "-",
                        TestType = testType
                    });


                ShortTrainingStatistic rowL = lastStat.FirstOrDefault(o => o.TestType == testType);
                if(rowL != null)
                {
                    lastStatDataGrid.Items.Add(new {
                        TestType = rowL.TestType.ToFullString(),
                        Count = rowL.Count,
                        LastTraining = rowL.LastTraining
                    });
                }
                else
                    lastStatDataGrid.Items.Add(new { TestType = testType.ToFullString(), Count = "-", LastTraining = "-" });
            }

            newWordsLbl.Content =
                dbFacade.GetClausesAsync(new FiltrationCriteria { AddedAfter = lastStatDate }).Result.Count();


            int? calcPercent(int success, int fail)
            {
                if(success + fail == 0)
                    return null;

                return (int)((double)success / (success + fail) * 100);
            }
        }

        private void ApplyGUIScale()
        {
            double guiScale = PrgSettings.Default.DialogsScale;

            mainWindowGrid.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;
        }

        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            var data = (TrainingTableClause)((FrameworkElement)sender).DataContext;

            StartTraining = data.TestType;
            DialogResult = true;
            Close();
        }
    }
}
