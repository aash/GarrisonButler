
        #region

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx.Helpers;

#endregion

namespace GarrisonButler.Config
{
    public class TradingPostTab
    {
        public Grid MainGrid { get; private set; }
        private readonly ListView _myListView;
        public SortAdorner ListViewSortAdorner { get; private set; }
        private GridViewColumnHeader _listViewSortCol;

        public TradingPostTab()
        {
            MainGrid = new Grid {Height = double.NaN, Width = double.NaN};
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            //MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});
            MainGrid.RowDefinitions.Add(new RowDefinition());
            //MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(30)});
            //MainGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(60)});

            var gridView = new GridView();

            var columnHeader0 = new GridViewColumnHeader { Tag = "Name", Content = "Item Name", Width = double.NaN };
            columnHeader0.Click += ColumnHeader_Click;
            var column0binding = new Binding("Name");
            //column0binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            var column0 = new GridViewColumn
            {
                Header = columnHeader0,
                Width = double.NaN,
                DisplayMemberBinding = column0binding
            };
            gridView.Columns.Add(column0);


            var columnHeader1 = new GridViewColumnHeader { Tag = "ItemId", Content = "Item ID", Width = double.NaN };
            columnHeader1.Click += ColumnHeader_Click;
            var column1 = new GridViewColumn
            {
                Header = columnHeader1,
                Width = double.NaN,
                DisplayMemberBinding = new Binding("ItemId")
            };
            gridView.Columns.Add(column1);


            var checkBoxTemplate = new DataTemplate();
            checkBoxTemplate.DataType = typeof (bool);
            FrameworkElementFactory activated = new FrameworkElementFactory(typeof(CheckBox));
            activated.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Activated"));

            checkBoxTemplate.VisualTree = activated;

            var columnHeader2 = new GridViewColumnHeader { Tag = "Activated", Content = "Activated", Width = double.NaN};
            columnHeader2.Click += ColumnHeader_Click;
            var column2 = new GridViewColumn
            {
                Header = columnHeader2,
                Width = double.NaN,
                //DisplayMemberBinding = column1binding,
                CellTemplate = checkBoxTemplate
            };
            gridView.Columns.Add(column2);



            _myListView = new ListView { View = gridView, ItemsSource = GaBSettings.Get().TradingPostReagentsSettings};

            //mainFrame.Content = myListView;
            Grid.SetColumn(_myListView, 0);
            Grid.SetRow(_myListView, 1);
            MainGrid.Children.Add(_myListView);
        }

        public object ContentTradingPostTab()
        {
            return MainGrid;
        }

        protected CheckBox CreateCheckBoxWithBinding(string label, string attributeName, object source)
        {
            var checkBox = new CheckBox {Content = label};
            // binding
            var binding = new Binding(attributeName) {Source = source};
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
            return checkBox;
        }
        
        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var column = (sender as GridViewColumnHeader);
            if (column == null) return;

            var sortBy = column.Tag.ToString();
            if (_listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(_listViewSortCol).Remove(ListViewSortAdorner);
                _myListView.Items.SortDescriptions.Clear();
            }

            var newDir = ListSortDirection.Ascending;
            if (Equals(_listViewSortCol, column) && ListViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            _listViewSortCol = column;
            ListViewSortAdorner = new SortAdorner(_listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_listViewSortCol).Add(ListViewSortAdorner);
            _myListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
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

                var geometry = AscGeometry;
                if (Direction == ListSortDirection.Descending)
                    geometry = DescGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }
    }
} 
    