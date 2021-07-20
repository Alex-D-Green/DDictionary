using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for ReportDlg.xaml
    /// </summary>
    public partial class ReportDlg: Window
    {
        public ReportDlg(string title, IEnumerable<string> data)
        {
            if(data is null)
                throw new ArgumentNullException(nameof(data));


            InitializeComponent();
            ApplyGUIScale();

            Title = title;

            foreach(string str in data)
            {
                textEdit.AppendText(str);
                textEdit.AppendText(Environment.NewLine);
            }
        }

        private void ApplyGUIScale()
        {
            double guiScale = Properties.Settings.Default.DialogsScale;

            mainWindowGrid.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            MaxWidth *= guiScale;
            MaxHeight *= guiScale;

            MinWidth *= guiScale;
            MinHeight *= guiScale;
        }
    }
}
