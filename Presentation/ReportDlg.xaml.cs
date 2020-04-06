﻿using System;
using System.Collections.Generic;
using System.Windows;


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

            Title = title;

            foreach(string str in data)
            {
                textEdit.AppendText(str);
                textEdit.AppendText(Environment.NewLine);
            }
        }
    }
}
