using System.Reflection;
using System.Windows;
using System.Windows.Media;


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
            ApplyGUIScale();

            Assembly assembly = Assembly.GetExecutingAssembly();

            versionLbl.Content += assembly.GetName().Version.ToString();
            copyrightLbl.Content = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        }

        private void ApplyGUIScale()
        {
            double guiScale = Properties.Settings.Default.DialogsScale;

            mainWindowGrid.LayoutTransform = new ScaleTransform(guiScale, guiScale);

            Width *= guiScale;
            Height *= guiScale;
        }
    }
}
