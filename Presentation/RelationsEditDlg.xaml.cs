using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using DDictionary.DAL;
using DDictionary.Domain;
using DDictionary.Domain.Entities;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Interaction logic for RelationsEditDlg.xaml
    /// </summary>
    public partial class RelationsEditDlg: Window
    {
        private sealed class RelationDTO
        {
            public int Id { get; set; }
            public int ToWordId { get; set; }
            public string Description { get; set; }
            public bool DescriptionWasChanged { get; set; }
            public bool MakeInterconnected { get; set; }
        }


        /// <summary>The maximal count of handling relations.</summary>
        public const int MaxCountOfRelations = 10;


        /// <summary>The clause which relations are editing.</summary>
        private readonly Clause clause;

        /// <summary>The list of removed relations' ids.</summary>
        private readonly List<int> removedRelations = new List<int>();


        /// <summary>The count of relations in the dialog.</summary>
        private int countOfRelations;


        /// <summary>The object to work with data storage.</summary>
        private IDBFacade dbFacade { get; set; } = new InMemoryMockStorage(); //Dependency Injection


        public RelationsEditDlg(int clauseId)
        {
            if(clauseId <= 0)
                throw new ArgumentOutOfRangeException(nameof(clause));


            clause = dbFacade.GetClauseById(clauseId);

            InitializeComponent();

            wordLbl.Content = String.Format(PrgResources.WordRelationTo, clause.Word);

            //Initialize the list of all words in the dictionary except this clause's word itself
            listOfWordsCBox.ItemsSource = dbFacade.GetJustWords()
                                                  .Where(o => o.Id != clause.Id)
                                                  .OrderBy(o => o.Word);

            //Show rows of relations
            foreach(Relation rel in clause.Relations)
            {
                AddRelationRow(rel.Id, rel.To.Word, rel.To.Id, rel.Description);

                if(countOfRelations == MaxCountOfRelations)
                    break; //The maximal amount has been shown
            }

            if(clause.Relations.Count > MaxCountOfRelations)
                MessageBox.Show(this, 
                    String.Format(PrgResources.ExceedMaxCountOfRelations, MaxCountOfRelations, clause.Relations.Count), 
                    PrgResources.InformationCaption, MessageBoxButton.OK, MessageBoxImage.Information);

            mainStackPanel.Children[1].Visibility = Visibility.Collapsed; //To hide the "template" row
            UpdateAddButtonState();

            Activated += OnRelationsEditDlg_Activated; //To do initial actions when form will be shown
        }

        /// <summary>
        /// Initial actions.
        /// </summary>
        private void OnRelationsEditDlg_Activated(object sender, EventArgs e)
        {
            Activated -= OnRelationsEditDlg_Activated; //Not need to replay

            FixHeight();
        }

        /// <summary>
        /// Add new relation row (a panel with respective elements) to the dialog.
        /// </summary>
        private void AddRelationRow(int relId, string word, int wordId, string descr, bool makeInterconnected = false)
        {
            var copy = (FrameworkElement)XamlReader.Parse(XamlWriter.Save(relationRow));
            int addPanelIdx = mainStackPanel.Children.IndexOf(addNewRelationPanel);

            var relationDTO = new RelationDTO {
                Id = relId,
                ToWordId = wordId,
                Description = descr,
                MakeInterconnected = makeInterconnected
            };

            copy.Visibility = Visibility.Visible;
            copy.Tag = relationDTO;

            var newToWordLbl = (Label)copy.FindName(nameof(toWordLbl));
            newToWordLbl.Content = word;

            var newDescrTBox = (TextBox)copy.FindName(nameof(descrTBox));
            newDescrTBox.Text = descr;
            newDescrTBox.TabIndex = addPanelIdx;
            newDescrTBox.TextChanged += (s, e) => 
            {
                relationDTO.Description = newDescrTBox.Text;
                relationDTO.DescriptionWasChanged = true;
                ChangesHaveBeenMade();
            };

            var newRemoveBtn = (Button)copy.FindName(nameof(removeBtn));
            newRemoveBtn.Click += (s, e) => 
            {
                LooseHeight();

                mainStackPanel.Children.Remove(copy);
                if(relId != 0)
                    removedRelations.Add(relId);

                FixHeight();

                countOfRelations--;
                UpdateAddButtonState();
                ChangesHaveBeenMade();
            };
            newRemoveBtn.TabIndex = addPanelIdx + 1;

            LooseHeight();

            mainStackPanel.Children.Insert(addPanelIdx, copy);

            if(Visibility == Visibility.Visible) //Only if form is already shown and knows its size
                FixHeight();

            countOfRelations++;
            UpdateAddButtonState();
        }

        /// <summary>
        /// Allow to change the dialog height.
        /// </summary>
        private void LooseHeight()
        {
            MinHeight = 0;
            MaxHeight = Double.PositiveInfinity;
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

        #region Handling of "add new relative" button's state

        private void OnNewRelationDescrTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddButtonState();
        }

        private void OnListOfWordsCBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAddButtonState();
        }

        private void UpdateAddButtonState()
        {
            addRelationBtn.IsEnabled = countOfRelations < MaxCountOfRelations &&
                                       listOfWordsCBox.SelectedItem != null &&
                                       !String.IsNullOrEmpty(newRelationDescrTBox.Text);
        }

        #endregion

        /// <summary>
        /// Create new relation.
        /// </summary>
        private void OnAddRelationBtn_Click(object sender, RoutedEventArgs e)
        {
            var word = (JustWordDTO)listOfWordsCBox.SelectedItem;

            AddRelationRow(0, word.Word, word.Id, newRelationDescrTBox.Text, interconnectedCheck.IsChecked == true);

            newRelationDescrTBox.Text = ""; //To prevent duplication
            interconnectedCheck.IsChecked = false;
            ChangesHaveBeenMade();
        }

        private void OnCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Some changes have been made in the records.
        /// </summary>
        private void ChangesHaveBeenMade()
        {
            acceptBtn.IsEnabled = true;
        }

        /// <summary>
        /// Save all changes in relations into the storage.
        /// </summary>
        private void SaveChanges()
        {
            int relationRowIdx = mainStackPanel.Children.IndexOf(relationRow);
            int addPanelIdx = mainStackPanel.Children.IndexOf(addNewRelationPanel);

            //Removing all deleted relations
            foreach(int relId in removedRelations)
                dbFacade.RemoveRelation(relId);

            //Create/update changed relations
            for(int i=relationRowIdx+1; i<addPanelIdx; i++)
            {
                var rowPanel = (FrameworkElement)mainStackPanel.Children[i];
                var relationDTO = (RelationDTO)rowPanel.Tag;

                if(relationDTO.Id != 0 && !relationDTO.DescriptionWasChanged)
                    continue; //This, existed record hasn't been changed
                
                dbFacade.AddOrUpdateRelation(relationDTO.Id, clause.Id, relationDTO.ToWordId, relationDTO.Description);

                if(relationDTO.MakeInterconnected)
                { //Add relation to the other side
                    Debug.Assert(relationDTO.Id == 0);

                    dbFacade.AddOrUpdateRelation(0, relationDTO.ToWordId, clause.Id, relationDTO.Description);
                }
            }
        }
    }
}
