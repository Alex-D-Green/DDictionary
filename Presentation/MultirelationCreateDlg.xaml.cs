using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DDictionary.Domain;
using DDictionary.Presentation.ViewModels;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MultirelationCreateDlg: Window
    {
        /// <summary>The list of words that should be related.</summary>
        private readonly DataGridClause[] words;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = CompositionRoot.DBFacade;


        public MultirelationCreateDlg(DataGridClause[] words)
        {
            if(words == null || words.Length < 2)
                throw new ArgumentException("Must be at least 2 items.", nameof(words));


            this.words = words;

            InitializeComponent();

            wordsLbl.Content = words.Aggregate("", (s, o) => s + $"{o.Word}, ").TrimEnd(',', ' ');

            Activated += OnRelationsEditDlg_Activated; //To do initial actions when form will be shown
        }

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnRelationsEditDlg_Activated(object sender, EventArgs e)
        {
            Activated -= OnRelationsEditDlg_Activated; //Not need to replay

            FixHeight();

            newRelationDescrTBox.Focus();
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

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void OnCreateButton_Click(object sender, RoutedEventArgs e)
        {
            for(int i=words.Length-1; i>0; i--)
            {
                DataGridClause first = words[i];

                for(int j=i-1; j>=0; j--)
                {
                    DataGridClause second = words[j];

                    await dbFacade.AddOrUpdateRelationAsync(0, first.Id, second.Id, newRelationDescrTBox.Text);
                    await dbFacade.AddOrUpdateRelationAsync(0, second.Id, first.Id, newRelationDescrTBox.Text);
                }
            }

            DialogResult = true;
            Close();
        }

        private void OnNewRelationDescrTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            createBtn.IsEnabled = !String.IsNullOrEmpty(newRelationDescrTBox.Text);
        }
    }
}
