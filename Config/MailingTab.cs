#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Threading;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx.Helpers;
using VerticalAlignment = System.Windows.VerticalAlignment;

#endregion

namespace GarrisonButler.Config
{
    internal class MailingTab
    {
        private static readonly Action EmptyDelegate = delegate { };
        private readonly Grid _mainGrid;
        private readonly ListView _myListView = new ListView();
        private TextBox _addCommentTextBox = new TextBox();
        private TextBox _addItemIdTextBox = new TextBox();
        private TextBox _addRecipientTextBox = new TextBox();
        private ComboBox _addRuleListBox = new ComboBox();
        private TextBox _addRuleValueTextBox = new TextBox();
        private SortAdorner _listViewSortAdorner;
        private GridViewColumnHeader _listViewSortCol;

        public MailingTab()
        {
            _mainGrid = new Grid();
            _mainGrid.Height = double.NaN;
            _mainGrid.Width = double.NaN;
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _mainGrid.RowDefinitions.Add(new RowDefinition());
            _mainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});
            _mainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(60)});


            var gridView = new GridView();

            var columnHeader0 = new GridViewColumnHeader {Tag = "ItemID", Content = "Item ID", Width = double.NaN};
            columnHeader0.Click += MailColumnHeader_Click;
            var column0 = new GridViewColumn
            {
                Header = columnHeader0,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("ItemId")
            };
            gridView.Columns.Add(column0);

            var columnHeader1 = new GridViewColumnHeader {Tag = "Recipient", Content = "Recipient", Width = double.NaN};
            columnHeader1.Click += MailColumnHeader_Click;
            var column1 = new GridViewColumn
            {
                Header = columnHeader1,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("Recipient")
            };
            gridView.Columns.Add(column1);

            var columnHeader2 = new GridViewColumnHeader
            {
                Tag = "Comment",
                Content = "Comment",
                Width = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            columnHeader2.Click += MailColumnHeader_Click;
            var column2 = new GridViewColumn
            {
                Header = columnHeader2,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("Comment")
            };
            gridView.Columns.Add(column2);

            var columnHeader3 = new GridViewColumnHeader
            {
                Tag = "Condition",
                Content = "Condition",
                Width = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            columnHeader3.Click += MailColumnHeader_Click;
            var column3 = new GridViewColumn
            {
                Header = columnHeader3,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("Condition")
            };
            gridView.Columns.Add(column3);

            var columnHeader4 = new GridViewColumnHeader
            {
                Tag = "Value",
                Content = "Value",
                Width = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            columnHeader4.Click += MailColumnHeader_Click;
            var column4 = new GridViewColumn
            {
                Header = columnHeader4,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("CheckValue")
            };
            gridView.Columns.Add(column4);

            _myListView = new ListView {View = gridView, ItemsSource = GaBSettings.Get().MailItems};
            _myListView.SelectionChanged += myListView_OnSelectionChanged;

            //mainFrame.Content = myListView;
            Grid.SetColumn(_myListView, 0);
            Grid.SetRow(_myListView, 0);
            _mainGrid.Children.Add(_myListView);

            // Buttons
            Grid barPanel = Buttons();
            Grid.SetRow(barPanel, 1);
            Grid.SetColumn(barPanel, 0);
            _mainGrid.Children.Add(barPanel);

            // Input data
            Grid Inputs = InputBoxes();
            Grid.SetRow(Inputs, 2);
            Grid.SetColumn(Inputs, 0);
            _mainGrid.Children.Add(Inputs);
        }

        private Button AddNewMailItemButton { get; set; }

        public object ContentTabMailing()
        {
            return _mainGrid;
        }

        private Grid InputBoxes()
        {
            var grid = new Grid();

            grid = new Grid();
            grid.Height = double.NaN;
            grid.Width = double.NaN;
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});
            grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});

            Grid.SetRow(grid, 1);
            Grid.SetColumn(grid, 0);

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

            Grid.SetRow(stackpanelTextBox, 0);
            Grid.SetColumn(stackpanelTextBox, 0);
            grid.Children.Add(stackpanelTextBox);


            var stackpanelTextBox2 = new StackPanel();
            stackpanelTextBox2.Orientation = Orientation.Horizontal;

            var AddRule = new Label();
            AddRule.VerticalAlignment = VerticalAlignment.Center;
            AddRule.Content = "AddRule:";
            AddRule.Margin = new Thickness(5, 0, 5, 0);
            stackpanelTextBox2.Children.Add(AddRule);

            _addRuleListBox = new ComboBox();
            _addRuleListBox.Width = double.NaN;
            _addRuleListBox.MinWidth = 40;
            _addRuleListBox.VerticalContentAlignment = VerticalAlignment.Center;
            _addRuleListBox.ItemsSource = MailCondition.GetAllPossibleConditions();
            stackpanelTextBox2.Children.Add(_addRuleListBox);


            var AddRuleValue = new Label();
            AddRuleValue.VerticalAlignment = VerticalAlignment.Center;
            AddRuleValue.Content = "AddRuleValue:";
            AddRuleValue.Margin = new Thickness(5, 0, 5, 0);
            stackpanelTextBox2.Children.Add(AddRuleValue);

            _addRuleValueTextBox = new TextBox();
            _addRuleValueTextBox.Width = 40;
            _addRuleValueTextBox.MinWidth = 30;
            _addRuleValueTextBox.TextWrapping = TextWrapping.Wrap;
            _addRuleValueTextBox.VerticalContentAlignment = VerticalAlignment.Center;
            stackpanelTextBox2.Children.Add(_addRuleValueTextBox);

            Grid.SetRow(stackpanelTextBox2, 1);
            Grid.SetColumn(stackpanelTextBox2, 0);
            grid.Children.Add(stackpanelTextBox2);


            return grid;
        }

        private Grid Buttons()
        {
            var barPanel = new Grid
            {
                Width = double.NaN,
                Height = 30,
                MinHeight = 30,
                MinWidth = 250,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            barPanel.ColumnDefinitions.Add(new ColumnDefinition());
            barPanel.ColumnDefinitions.Add(new ColumnDefinition());
            barPanel.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});

            AddNewMailItemButton = new Button {Content = "Add new item", Tag = 1};
            AddNewMailItemButton.Click += AddNewMailItem_Click;
            Grid.SetRow(AddNewMailItemButton, 0);
            Grid.SetColumn(AddNewMailItemButton, 0);
            barPanel.Children.Add(AddNewMailItemButton);

            var DeleteSelected = new Button {Content = "Delete Selected", Tag = 3};
            DeleteSelected.Click += DeleteSelected_Click;
            Grid.SetRow(DeleteSelected, 0);
            Grid.SetColumn(DeleteSelected, 1);
            barPanel.Children.Add(DeleteSelected);

            return barPanel;
        }

        private void myListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listview = sender as ListView;
            if (listview != null)
            {
                var item = listview.SelectedItem as MailItem;
                if (item != null)
                {
                    var bindingId = new Binding("ItemId") {Source = item};
                    _addItemIdTextBox.SetBinding(TextBox.TextProperty, bindingId);

                    var bindingRecipient = new Binding("Recipient") {Source = item};
                    _addRecipientTextBox.SetBinding(TextBox.TextProperty, bindingRecipient);

                    var bindingComment = new Binding("Comment") { Source = item };
                    _addCommentTextBox.SetBinding(TextBox.TextProperty, bindingComment);

                    var bindingCheckValue = new Binding("CheckValue") { Source = item };
                    _addRuleValueTextBox.SetBinding(TextBox.TextProperty, bindingCheckValue);

                    var toSelect =
                        MailCondition.GetAllPossibleConditions().FirstOrDefault(c => c.Name == item.Condition.Name);
                    //var bindingCondition = new Binding("Condition") { Source = item };
                    //_addRuleListBox.SetBinding(TextBox.TextProperty, bindingCondition);
                    //if (toSelect != null)
                    _addRuleListBox.SelectedItem = toSelect;


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
            var sortBy = column.Tag.ToString();
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
                foreach (var selected in _myListView.SelectedItems)
                {
                    GaBSettings.Get().MailItems.Remove((MailItem) selected);
                }
                GarrisonButler.Diagnostic("Deleted selected Items.");
                ObjectDumper.WriteToHB(GaBSettings.Get().MailItems, 3);
                var view = CollectionViewSource.GetDefaultView(_myListView.ItemsSource);
                view.Refresh();
            }
            _myListView.View.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }


        private void AddNewMailItem_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            ICollectionView view = CollectionViewSource.GetDefaultView(_myListView.ItemsSource);
            switch ((int) btn.Tag)
            {
                case 1:
                {
                    uint itemId;
                    int checkValue;
                    MailCondition mailCondition;
                    if (!checkValuesInputs(out itemId, out checkValue, out mailCondition))
                        return;

                    var mailItems = GaBSettings.Get().MailItems;
                    var recipient = _addRecipientTextBox.Text;
                    var comment = _addCommentTextBox.Text;

                    if (mailItems != null)
                        mailItems.Add(new MailItem(itemId, recipient, mailCondition, checkValue, comment));

                    GarrisonButler.Diagnostic("Added mail Item");
                    ObjectDumper.WriteToHB(GaBSettings.Get().MailItems, 3);
                    view.Refresh();
                }
                    break;
                case 2:
                    var item = (MailItem)_myListView.SelectedItem;
                    item.SetCondition(_addRuleListBox.SelectedValue.ToString());
                    view.Refresh();
                    _myListView.SelectedItem = null;
                    break;
            }
        }

        private bool checkValuesInputs(out uint itemId, out int checkValue, out MailCondition mailCondition)
        {
            itemId = 0;
            checkValue = 0;
            mailCondition = null;
            // Check recipient
            if (_addRecipientTextBox.Text == "")
            {
                GarrisonButler.Warning("No value for Recipient, item not added.");
                return false;
            }
            // Check itemId
            if (!uint.TryParse(_addItemIdTextBox.Text, out itemId))
            {
                GarrisonButler.Warning("Value for itemId is not valid.");
                return false;
            }
            // Check itemId
            if (!int.TryParse(_addRuleValueTextBox.Text, out checkValue))
            {
                GarrisonButler.Warning("Value for checkValue is not valid.");
                return false;
            }
            // Check that a correct rule is selected
            mailCondition = (_addRuleListBox.SelectedItem as MailCondition);
            if (mailCondition == null || mailCondition.Name == "")
            {
                GarrisonButler.Warning("Value for checkValue is not valid.");
                return false;
            }
            // Check if already in list
            if (
                GaBSettings.Get()
                    .MailItems.GetEmptyIfNull()
                    .Any(i => i.ItemId == _addItemIdTextBox.Text.ToInt32()))
            {
                GarrisonButler.Warning("Item already added to list.");
                return false;
            }
            return true;
        }

        public class SortAdorner : Adorner
        {
            private static readonly Geometry AscGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static readonly Geometry DescGeometry =
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

                Geometry geometry = AscGeometry;
                if (Direction == ListSortDirection.Descending)
                    geometry = DescGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }
    }
}