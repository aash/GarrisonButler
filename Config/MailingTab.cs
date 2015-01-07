#region

using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GarrisonButler.Objects;
using GarrisonButler.Libraries;
using Styx.Helpers;

#endregion

namespace GarrisonButler.Config
{
    internal class MailingTab
    {
        private readonly Grid _mainGrid;
        private readonly ListView _myListView = new ListView();
        private TextBox _addCommentTextBox = new TextBox();
        private TextBox _addItemIdTextBox = new TextBox();
        private TextBox _addRecipientTextBox = new TextBox();
        private SortAdorner _listViewSortAdorner;
        private GridViewColumnHeader _listViewSortCol;

        public MailingTab()
        {
            _mainGrid = new Grid();
            _mainGrid.Height = double.NaN;
            _mainGrid.Width = double.NaN;

            var gridmainCol1 = new ColumnDefinition();
            _mainGrid.ColumnDefinitions.Add(gridmainCol1);

            var gridmainRow1 = new RowDefinition();
            _mainGrid.RowDefinitions.Add(gridmainRow1);

            var gridmainRow2 = new RowDefinition();
            gridmainRow2.Height = new GridLength(60);
            _mainGrid.RowDefinitions.Add(gridmainRow2);

            var gridView = new GridView();

            var column0 = new GridViewColumn();
            var columnHeader0 = new GridViewColumnHeader();
            columnHeader0.Tag = "ItemID";
            columnHeader0.Content = "Item ID";
            columnHeader0.Width = double.NaN;
            columnHeader0.Click += MailColumnHeader_Click;
            column0.Header = columnHeader0;
            column0.Width = double.NaN;
           
            column0.DisplayMemberBinding = new Binding("ItemId");
            gridView.Columns.Add(column0);

            var column1 = new GridViewColumn();
            var columnHeader1 = new GridViewColumnHeader();
            columnHeader1.Tag = "Recipient";
            columnHeader1.Content = "Recipient";
            columnHeader1.Width = double.NaN;
            columnHeader1.Click += MailColumnHeader_Click;
            column1.Header = columnHeader1;
            column1.Width = double.NaN;
            column1.DisplayMemberBinding = new Binding("Recipient");
            gridView.Columns.Add(column1);

            var column2 = new GridViewColumn();
            var columnHeader2 = new GridViewColumnHeader();
            columnHeader2.Tag = "Comment";
            columnHeader2.Content = "Comment";
            columnHeader2.Width = double.NaN;
            columnHeader2.HorizontalAlignment = HorizontalAlignment.Stretch;
            columnHeader2.Click += MailColumnHeader_Click;
            column2.Header = columnHeader2;
            column2.Width = double.NaN;
            column2.DisplayMemberBinding = new Binding("Comment");
            gridView.Columns.Add(column2);

            _myListView = new ListView();
            _myListView.View = gridView;
            _myListView.ItemsSource = GaBSettings.Get().MailItems;
            _myListView.SelectionChanged += myListView_OnSelectionChanged;

            //mainFrame.Content = myListView;
            Grid.SetColumn(_myListView, 0);
            Grid.SetRow(_myListView, 0);
            _mainGrid.Children.Add(_myListView);

            // Buttons
            Grid barPanel = Buttons();
            _mainGrid.Children.Add(barPanel);

            // Input data
            StackPanel stackpanelTextBox = InputBoxes();
            barPanel.Children.Add(stackpanelTextBox);
        }

        public object ContentTabMailing()
        {
            return _mainGrid;
        }

        private StackPanel InputBoxes()
        {
            var stackpanelTextBox = new StackPanel();
            stackpanelTextBox.Orientation = Orientation.Horizontal;

            var AddItemID = new Label();
            AddItemID.VerticalAlignment = VerticalAlignment.Center;
            AddItemID.Content = "Item ID:";
            AddItemID.Margin = new Thickness(5, 0, 5, 0);
            stackpanelTextBox.Children.Add(AddItemID);

            _addItemIdTextBox = new TextBox();
            _addItemIdTextBox.Width = 75;
            _addItemIdTextBox.MinWidth = 30;
            _addItemIdTextBox.TextWrapping = TextWrapping.Wrap;
            _addItemIdTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            stackpanelTextBox.Children.Add(_addItemIdTextBox);

            var AddRecipient = new Label();
            AddRecipient.VerticalAlignment = VerticalAlignment.Center;
            AddRecipient.Content = "Recipient:";
            AddRecipient.Margin = new Thickness(5, 0, 5, 0);
            stackpanelTextBox.Children.Add(AddRecipient);

            _addRecipientTextBox = new TextBox();
            _addRecipientTextBox.Width = 110;
            _addRecipientTextBox.MinWidth = 30;
            _addRecipientTextBox.TextWrapping = TextWrapping.Wrap;
            _addRecipientTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            stackpanelTextBox.Children.Add(_addRecipientTextBox);

            var AddComment = new Label();
            AddComment.VerticalAlignment = VerticalAlignment.Center;
            AddComment.Content = "Comment:";
            AddComment.Margin = new Thickness(5, 0, 5, 0);
            stackpanelTextBox.Children.Add(AddComment);

            _addCommentTextBox = new TextBox();
            _addCommentTextBox.Width = 150;
            _addCommentTextBox.MinWidth = 30;
            _addCommentTextBox.TextWrapping = TextWrapping.Wrap;
            _addCommentTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            stackpanelTextBox.Children.Add(_addCommentTextBox);

            Grid.SetRow(stackpanelTextBox, 1);
            Grid.SetColumn(stackpanelTextBox, 0);
            return stackpanelTextBox;
        }

        private Grid Buttons()
        {
            var barPanel = new Grid();
            barPanel.Width = double.NaN;
            barPanel.Height = 60;
            barPanel.MinHeight = 60;
            barPanel.MinWidth = 250;
            barPanel.VerticalAlignment = VerticalAlignment.Bottom;
            barPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

            var gridCol1 = new ColumnDefinition();
            barPanel.ColumnDefinitions.Add(gridCol1);

            var gridRow1 = new RowDefinition();
            gridRow1.Height = new GridLength(30);
            var gridRow2 = new RowDefinition();
            gridRow2.Height = new GridLength(30);
            barPanel.RowDefinitions.Add(gridRow1);
            barPanel.RowDefinitions.Add(gridRow2);


            // BUTTONS to add and delete
            var GridButtons = new Grid();
            GridButtons.Width = double.NaN;
            GridButtons.Height = double.NaN;
            GridButtons.MinHeight = 30;
            GridButtons.VerticalAlignment = VerticalAlignment.Stretch;
            GridButtons.HorizontalAlignment = HorizontalAlignment.Stretch;

            var GridButtonsCol0 = new ColumnDefinition();
            GridButtons.ColumnDefinitions.Add(GridButtonsCol0);
            var GridButtonsCol1 = new ColumnDefinition();
            GridButtons.ColumnDefinitions.Add(GridButtonsCol1);

            var GridButtonsRow0 = new RowDefinition();
            GridButtonsRow0.Height = new GridLength(30);
            barPanel.RowDefinitions.Add(GridButtonsRow0);

            AddNewMailItemButton = new Button();
            AddNewMailItemButton.Content = "Add new item";
            AddNewMailItemButton.Tag = 1;
            AddNewMailItemButton.Click += AddNewMailItem_Click;
            Grid.SetRow(AddNewMailItemButton, 0);
            Grid.SetColumn(AddNewMailItemButton, 0);
            GridButtons.Children.Add(AddNewMailItemButton);

            var DeleteSelected = new Button();
            DeleteSelected.Content = "Delete Selected";
            DeleteSelected.Tag = 3;
            DeleteSelected.Click += DeleteSelected_Click;
            Grid.SetRow(DeleteSelected, 0);
            Grid.SetColumn(DeleteSelected, 1);
            GridButtons.Children.Add(DeleteSelected);

            Grid.SetRow(GridButtons, 0);
            Grid.SetColumn(GridButtons, 0);
            barPanel.Children.Add(GridButtons);

            Grid.SetRow(barPanel, 1);
            Grid.SetColumn(barPanel, 0);
            return barPanel;
        }

        private Button AddNewMailItemButton { get; set; }

        private void myListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listview = sender as ListView;
            if (listview != null)
            {
                var item = listview.SelectedItem as MailItem;
                if (item != null)
                {
                    var bindingId = new Binding("ItemId");
                    bindingId.Source = item;
                    _addItemIdTextBox.SetBinding(TextBox.TextProperty, bindingId);

                    var bindingRecipient = new Binding("Recipient");
                    bindingRecipient.Source = item;
                    _addRecipientTextBox.SetBinding(TextBox.TextProperty, bindingRecipient);

                    var bindingComment = new Binding("Comment");
                    bindingComment.Source = item;
                    _addCommentTextBox.SetBinding(TextBox.TextProperty, bindingComment);
                    AddNewMailItemButton.Content = "Edit";
                    AddNewMailItemButton.Tag = 2;
                }
                else
                {
                    BindingOperations.ClearBinding(_addItemIdTextBox, TextBox.TextProperty);
                    _addItemIdTextBox.Text = "";
                    BindingOperations.ClearBinding(_addRecipientTextBox, TextBox.TextProperty);
                    _addRecipientTextBox.Text = "";
                    BindingOperations.ClearBinding(_addCommentTextBox, TextBox.TextProperty);
                    _addCommentTextBox.Text = "";
                    AddNewMailItemButton.Content = "Add new item";
                    AddNewMailItemButton.Tag = 1;
                }
            }
        }


        private void MailColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (_listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(_listViewSortCol).Remove(_listViewSortAdorner);
                _myListView.Items.SortDescriptions.Clear();
            }

            var newDir = ListSortDirection.Ascending;
            if (_listViewSortCol == column && _listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            _listViewSortCol = column;
            _listViewSortAdorner = new SortAdorner(_listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_listViewSortCol).Add(_listViewSortAdorner);
            _myListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                foreach (object selected in _myListView.SelectedItems)
                {
                    GaBSettings.Get().MailItems.Remove((MailItem) selected);
                }
                GarrisonButler.Diagnostic("Deleted selected Items.");
                ObjectDumper.WriteToHB(GaBSettings.Get().MailItems, 3);
                //ICollectionView view = CollectionViewSource.GetDefaultView(myListView.ItemsSource);
                //view.Refresh();
                //BindingExpression binding = _myWindow.GetBindingExpression(ListView.DataContextProperty);
                //binding.UpdateSource();
            }
        }

        private void AddNewMailItem_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                if ((int)btn.Tag == 1)
                {
                    if (_addRecipientTextBox.Text == "")
                    {
                        GarrisonButler.Warning("No value for Recipient, item not added.");
                        return;
                    }
                    if (_addItemIdTextBox.Text == "")
                    {
                        GarrisonButler.Warning("No value for ItemID, item not added.");
                        return;
                    }
                    if (GaBSettings.Get().MailItems.GetEmptyIfNull().Any(i => i.ItemId == _addItemIdTextBox.Text.ToInt32()))
                    {
                        GarrisonButler.Warning("Item already added to list.");
                        return;
                    }

                    ObservableCollection<MailItem> mailItems = GaBSettings.Get().MailItems;

                    if (!mailItems.IsNullOrEmpty())
                        mailItems.Add(new MailItem(_addItemIdTextBox.Text.ToInt32(), _addRecipientTextBox.Text,
                            _addCommentTextBox.Text));

                    _myListView.View.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    GarrisonButler.Diagnostic("Added mail Item");
                    ObjectDumper.WriteToHB(GaBSettings.Get().MailItems, 3);
                }
                else if ((int) btn.Tag == 2)
                {
                    _myListView.SelectedItem = null;
                }
            }
        }
        private static System.Action EmptyDelegate = delegate() { };

        public class SortAdorner : Adorner
        {
            private static readonly Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static readonly Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
            {
                Direction = dir;
            }

            public ListSortDirection Direction { get; private set; }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                var transform = new TranslateTransform
                    (
                    AdornedElement.RenderSize.Width - 15,
                    (AdornedElement.RenderSize.Height - 5)/2
                    );
                drawingContext.PushTransform(transform);

                Geometry geometry = ascGeometry;
                if (Direction == ListSortDirection.Descending)
                    geometry = descGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }
    }
}