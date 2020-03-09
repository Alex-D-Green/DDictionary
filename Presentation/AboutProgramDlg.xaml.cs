using System.Reflection;
using System.Windows;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for AboutProgram.xaml
    /// </summary>
    public partial class AboutProgramDlg: Window
    {
        public AboutProgramDlg()
        {
            InitializeComponent();

            versionLbl.Content += 
                Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
