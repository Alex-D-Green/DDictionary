using System;
using System.Diagnostics;
using System.Windows;

using DDictionary.Domain;
using DDictionary.Domain.Entities;

using Microsoft.Win32;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for SoundRefEditDlg.xaml
    /// </summary>
    public partial class SoundRefEditDlg: Window
    {
        private readonly Clause clause;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;

        /// <summary>Selected sound uri.</summary>
        public string SoundRef { get; private set; } = null;


        public SoundRefEditDlg(Clause clause)
        {
            if(clause is null)
                throw new ArgumentNullException(nameof(clause));


            InitializeComponent();

            this.clause = clause;

            //Add controls update depending on ref value (INCLUDING TOOLTIP WITH CACHED FILE NAME);

            refEdit.Text = clause.Sound;
            refEdit.Focus();
            refEdit.TextChanged += (s, e) => 
            { 
                UpdateGUIState(); 

                acceptBtn.IsEnabled = true; 
            };

            Activated += OnDialog_Activated; //To do initial actions when form will be shown
        }

        /// <summary>
        /// Update availability of the controls.
        /// </summary>
        private void UpdateGUIState()
        {
            bool haveSound = !String.IsNullOrWhiteSpace(refEdit.Text);

            playBtn.IsEnabled = clearBtn.IsEnabled = haveSound;

            refreshBtn.IsEnabled = haveSound && 
                Uri.TryCreate(refEdit.Text, UriKind.RelativeOrAbsolute, out Uri uri) && uri.IsAbsoluteUri && !uri.IsFile;

            if(SoundManager.IsFileCached(clause.Id, refEdit.Text, dbFacade.DataSource, out string fullName))
                refEdit.ToolTip = fullName;
            else
                refEdit.ToolTip = null;
        }

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnDialog_Activated(object sender, EventArgs e)
        {
            Activated -= OnDialog_Activated; //Not need to replay

            FixHeight();

            UpdateGUIState();
        }

        /// <summary>
        /// Fix current dialog height and prohibit it from changing.
        /// </summary>
        private void FixHeight()
        {
            //https://stackoverflow.com/questions/1256916/how-to-force-actualwidth-and-actualheight-to-update-silverlight

            //To fit dialog's size, especially its height
            SizeToContent = SizeToContent.Manual;
            SizeToContent = SizeToContent.WidthAndHeight;

            //To prevent height changing
            MinHeight = MaxHeight = ActualHeight; //ActualHeight should be updated by this time
        }

        /// <summary>
        /// Handle play sound button click.
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.SoundManager"/>
        private async void OnPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            var ctrl = (FrameworkElement)sender;
            ctrl.IsEnabled = false; //Temporary disabled to prevent multiple clicking

            try
            { 
                await SoundManager.PlaySoundAsync(clause.Id, refEdit.Text, dbFacade.DataSource);
                UpdateGUIState();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { ctrl.IsEnabled = true; }
        }

        /// <summary>
        /// Handle refresh sound button click (update the sound cache).
        /// </summary>
        /// <seealso cref="DDictionary.Presentation.SoundManager"/>
        private async void OnRefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SoundManager.StopPlaying(); //To free local file 

                await SoundManager.TryRefreshAsync(clause.Id, refEdit.Text, dbFacade.DataSource);
                UpdateGUIState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle clear sound button click.
        /// </summary>
        private void OnClearBtn_Click(object sender, RoutedEventArgs e)
        {
            refEdit.Text = "";
        }

        /// <summary>
        /// Handle browse local button click.
        /// </summary>
        private void OnLocalBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog() {
                    Title = PrgResources.SelectSoundFileTitle,
                    Filter = "All sound files|*.wav;*.mp3|All files (*.*)|*.*"
                };

                if(openFileDialog.ShowDialog() == true)
                    refEdit.Text = openFileDialog.FileName;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(this, ex.Message, PrgResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle accept button click.
        /// </summary>
        private void OnAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            SoundRef = refEdit.Text;

            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            SoundManager.StopPlaying();

            base.OnClosed(e);
        }
    }
}
