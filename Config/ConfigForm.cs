using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using GarrisonBuddy.Objects;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using Orientation = System.Windows.Controls.Orientation;
using TabControl = System.Windows.Controls.TabControl;
using TextBox = System.Windows.Controls.TextBox;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace GarrisonBuddy.Config
{
    public partial class ConfigForm : Form
    {
        private static MyWindow _myWindow;
        private static List<CheckBox> collectCheckBoxes;
        private static List<CheckBox> startCheckBoxes; 
        public ConfigForm()
        {
            this.Close();
            if(_myWindow == null)
                _myWindow = new MyWindow();

            _myWindow.Activate();
            _myWindow.Show();

        }

        public class MyWindow : Window
        {
            public MyWindow()
            {
                Width = 600;
                Height = 400;
                MinHeight = 300;
                MinHeight = 300;
                this.Title = "GarrisonBuddy Beta v" + GarrisonBuddy.Version;

                TabControl tabControl = new TabControl();
                tabControl.Height = double.NaN;
                tabControl.Width = double.NaN;

                TabItem generalTabItem = new TabItem { Header = "General", Content = ContentTabGeneral() };
                tabControl.Items.Add(generalTabItem);

                TabItem WorkOrderTabItem = new TabItem { Header = "Work Orders", Content = ContentTabWorkOrder() };
                tabControl.Items.Add(WorkOrderTabItem);
                
                TabItem ProfessionTabItem = new TabItem {Header = "Professions", Content = ContentTabProfession()};
                tabControl.Items.Add(ProfessionTabItem);


                Content = tabControl;

            }
            private UIElement ProfessionBox(List<DailyProfession> dailies)
            {
                Border border = new Border();
                border.HorizontalAlignment = HorizontalAlignment.Left;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);

                Grid grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                Label name = new Label();
                name.Content = dailies.First().TradeskillId.ToString();
                name.FontSize = 14;
                name.FontWeight = FontWeights.Black;
                name.HorizontalAlignment = HorizontalAlignment.Left;
                name.VerticalAlignment = VerticalAlignment.Top;
                name.Width = 163;
                name.Height = 28;
                name.Margin = new Thickness(10, 5, 0, 0);

                CheckBox daily1 = CreateCheckBoxWithBindingBuilding(dailies.ElementAt(0).Name, "Activated", dailies.ElementAt(0));
                daily1.Margin = new Thickness(10, 33, 0, 0);
                AlldailiesCheckkbox.Add(daily1);

                CheckBox daily2 = CreateCheckBoxWithBindingBuilding(dailies.ElementAt(1).Name, "Activated", dailies.ElementAt(1));
                daily2.Margin = new Thickness(10, 60, 0, 0);
                AlldailiesCheckkbox.Add(daily2);


                grid.Children.Add(name);
                grid.Children.Add(daily1);
                grid.Children.Add(daily2);

                border.Child = grid;

                return border;
            }
            protected object ContentTabProfession()
            {

                AlldailiesCheckkbox = new List<CheckBox>();

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;

                ColumnDefinition gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                RowDefinition gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                RowDefinition gridmainRow2 = new RowDefinition();
                gridmainRow2.Height = new GridLength(30);
                grid.RowDefinitions.Add(gridmainRow2);

                

                ScrollViewer mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                WrapPanel mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Horizontal;
                mainWrapPanel.Width = double.NaN;
                var listOflists = GaBSettings.Get().DailySettings
                    .Select((x, i) => new { Index = x.TradeskillId, Value = x })
                    .GroupBy(x => x.Index)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var dailies in listOflists)
                {
                    var b = ProfessionBox(dailies);
                    mainWrapPanel.Children.Add(b);

                }


                mainFrame.Content = mainWrapPanel;
                Grid.SetColumn(mainFrame, 0);
                Grid.SetRow(mainFrame, 0);
                grid.Children.Add(mainFrame);


                //  bar
                Grid barPanel = new Grid();
                barPanel.Width = double.NaN;
                barPanel.Height = 30;
                barPanel.MinHeight = 30;
                barPanel.MinWidth = 250;
                barPanel.VerticalAlignment = VerticalAlignment.Bottom;
                barPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                ColumnDefinition gridCol1 = new ColumnDefinition();
                ColumnDefinition gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                RowDefinition gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(30);
                barPanel.RowDefinitions.Add(gridRow1);

                Button Select = new Button();
                Select.Content = "Select All";
                Select.Tag = 1;
                Select.Click += SelectAllProfession_Click;
                Grid.SetRow(Select, 0);
                Grid.SetColumn(Select, 0);
                barPanel.Children.Add(Select);

                Button StartAll = new Button();
                StartAll.Content = "Unselect All";
                StartAll.Tag = 3;
                StartAll.Click += UnSelectAllProfession_Click;
                Grid.SetRow(StartAll, 0);
                Grid.SetColumn(StartAll, 1);
                barPanel.Children.Add(StartAll);

                Grid.SetRow(barPanel, 1);
                Grid.SetColumn(barPanel, 0);
                grid.Children.Add(barPanel);

                
                return grid;

            }

            public List<CheckBox> AlldailiesCheckkbox { get; set; }

            protected object ContentTabWorkOrder()
            {
                startCheckBoxes = new List<CheckBox>();
                collectCheckBoxes = new List<CheckBox>();

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;

                ColumnDefinition gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                RowDefinition gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                RowDefinition gridmainRow2 = new RowDefinition();
                gridmainRow2.Height = new GridLength(30);
                grid.RowDefinitions.Add(gridmainRow2);

                
                ScrollViewer mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                WrapPanel mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Horizontal;
                mainWrapPanel.Width = double.NaN;
                
                foreach (var buildingsSetting in GaBSettings.Get().BuildingsSettings.OrderBy(b => b.Name))
                {
                    var b = BuildingBox(buildingsSetting);
                    mainWrapPanel.Children.Add(b);

                }
                mainFrame.Content = mainWrapPanel;

                Grid.SetRow(mainFrame, 0);
                Grid.SetColumn(mainFrame, 0);

                grid.Children.Add(mainFrame);

                //  bar
                Grid barPanel = new Grid();
                barPanel.Width = double.NaN;
                barPanel.Height = 30;
                barPanel.MinHeight = 30;
                barPanel.MinWidth = 250;
                barPanel.VerticalAlignment = VerticalAlignment.Bottom;
                barPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                ColumnDefinition gridCol1 = new ColumnDefinition();
                ColumnDefinition gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                RowDefinition gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(30);
                barPanel.RowDefinitions.Add(gridRow1);

                Button collectAll = new Button();
                collectAll.Content = "Select Collect All";
                collectAll.Tag = 1;
                collectAll.Click += newBtn_Click;
                Grid.SetRow(collectAll, 0);
                Grid.SetColumn(collectAll, 0);
                barPanel.Children.Add(collectAll);

                Button StartAll = new Button();
                StartAll.Content = "Select Start All";
                StartAll.Tag = 3;
                StartAll.Click += newBtn_Click;
                Grid.SetRow(StartAll, 0);
                Grid.SetColumn(StartAll, 1);
                barPanel.Children.Add(StartAll);

                Grid.SetRow(barPanel, 1);
                Grid.SetColumn(barPanel, 0);
                grid.Children.Add(barPanel);

                return grid;
            }
            private void SelectAllProfession_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    foreach (var checkBox in AlldailiesCheckkbox)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
            private void UnSelectAllProfession_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    foreach (var checkBox in AlldailiesCheckkbox)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
            private void newBtn_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    if (btn.Tag is int)
                    {
                        switch ((int)btn.Tag)
                        {
                            case 1:
                                foreach (var checkBox in collectCheckBoxes)
                                {
                                    checkBox.IsChecked = true;
                                    btn.Tag = 2;
                                    btn.Content = "Unselect Collect All";
                                }
                                break;

                            case 2:
                                foreach (var checkBox in collectCheckBoxes)
                                {
                                    checkBox.IsChecked = false;
                                    btn.Tag = 1;
                                    btn.Content = "Select Collect All";
                                }
                                break;

                            case 3:
                                foreach (var checkBox in startCheckBoxes)
                                {
                                    checkBox.IsChecked = true;
                                    btn.Tag = 4;
                                    btn.Content = "Unselect Start All";
                                }
                                break;

                            case 4:
                                foreach (var checkBox in startCheckBoxes)
                                {
                                    checkBox.IsChecked = false;
                                    btn.Tag = 3;
                                    btn.Content = "Select Start All";
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            protected object ContentTabGeneral()
            {
                var grid = new Grid();

                grid.Height = double.NaN;
                grid.Width = double.NaN;

                ScrollViewer mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                WrapPanel mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Vertical;
                mainWrapPanel.Width = double.NaN;

                var UseGarrisonHearthstone = CreateCheckBoxWithBinding("Use Garrison Hearthstone", "UseGarrisonHearthstone", GaBSettings.Get());
                mainWrapPanel.Children.Add(UseGarrisonHearthstone);

                var HBRelogMode = CreateCheckBoxWithBinding("Activate HBRelog Mode: auto skip to next task when done (Cautious, not tested!)", "HBRelogMode", GaBSettings.Get());
                mainWrapPanel.Children.Add(HBRelogMode);
                

                var GarrisonCache = CreateCheckBoxWithBinding("Collect garrison cache", "GarrisonCache", GaBSettings.Get());
                mainWrapPanel.Children.Add(GarrisonCache);


                var HarvestGarden = CreateCheckBoxWithBinding("Harvest herbs in garden", "HarvestGarden", GaBSettings.Get());
                mainWrapPanel.Children.Add(HarvestGarden);


                var HarvestMine = CreateCheckBoxWithBinding("Harvest ores in mine", "HarvestMine", GaBSettings.Get());
                mainWrapPanel.Children.Add(HarvestMine);

                var UseCoffee = CreateCheckBoxWithBinding("Use coffee in mine", "UseCoffee", GaBSettings.Get());
                mainWrapPanel.Children.Add(UseCoffee);

                var UseMiningPick = CreateCheckBoxWithBinding("Use mining pick in mine", "UseMiningPick", GaBSettings.Get());
                mainWrapPanel.Children.Add(UseMiningPick);


                var ActivateBuildings = CreateCheckBoxWithBinding("Activate finished buildings", "ActivateBuildings", GaBSettings.Get());
                mainWrapPanel.Children.Add(ActivateBuildings);


                var SalvageCrates = CreateCheckBoxWithBinding("Open Salvage crates", "SalvageCrates", GaBSettings.Get());
                mainWrapPanel.Children.Add(SalvageCrates);


                var StartMissions = CreateCheckBoxWithBinding("Start missions if possible", "StartMissions", GaBSettings.Get());
                mainWrapPanel.Children.Add(StartMissions);

                var CompletedMissions = CreateCheckBoxWithBinding("Turn in completed missions", "CompletedMissions", GaBSettings.Get());
                mainWrapPanel.Children.Add(CompletedMissions);


                mainFrame.Content = mainWrapPanel;
                return mainFrame;

            }

            protected CheckBox CreateCheckBoxWithBinding(string Label, string AttributeName, object source)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Content = Label;
                checkBox.Height = 25;
                // binding
                var binding = new System.Windows.Data.Binding(AttributeName);
                binding.Source = source;
                checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
                return checkBox;
            }
            protected CheckBox CreateCheckBoxWithBindingBuilding(string Label, string AttributeName, object source)
            {
                CheckBox checkBox = CreateCheckBoxWithBinding(Label, AttributeName, source);
                checkBox.HorizontalAlignment = HorizontalAlignment.Left;
                checkBox.VerticalAlignment = VerticalAlignment.Top;
                checkBox.Height = 23;
                checkBox.Width = 150;
                return checkBox;
            }
            protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
            {
                //do my stuff before closing
                GaBSettings.Save();
                base.OnClosing(e);
                _myWindow = null;
            }

            private UIElement BuildingBox(BuildingSettings building)
            {
                Border border = new Border();
                border.HorizontalAlignment = HorizontalAlignment.Left;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);

                Grid grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                Label name = new Label();
                name.Content = building.Name;
                name.FontSize = 14;
                name.FontWeight = FontWeights.Black;
                name.HorizontalAlignment = HorizontalAlignment.Left;
                name.VerticalAlignment = VerticalAlignment.Top;
                name.Width = 163;
                name.Height = 28;
                name.Margin = new Thickness(10, 5, 0, 0);

                CheckBox collect = CreateCheckBoxWithBindingBuilding("Collect work orders", "CanCollectOrder", building);
                collect.Margin = new Thickness(10, 33, 0, 0);
                collectCheckBoxes.Add(collect);

                CheckBox start = CreateCheckBoxWithBindingBuilding("Start work orders", "CanStartOrder", building);
                start.Margin = new Thickness(10, 60, 0, 0);
                startCheckBoxes.Add(start);

                Label max = new Label();
                max.HorizontalAlignment = HorizontalAlignment.Left;
                max.VerticalAlignment = VerticalAlignment.Top;
                max.Width = 150;
                max.Height = 23;
                max.Margin = new Thickness(9, 75, 0, 0);
                max.Content = "Max (0 = unlimited):";

                TextBox maxTextBox = new TextBox();
                maxTextBox.Height = 23;
                maxTextBox.TextWrapping = TextWrapping.Wrap;
                maxTextBox.Text = "0";
                maxTextBox.Margin = new Thickness(127, 76, 11, 9);
                maxTextBox.MaxLength = 3;
                // binding
                var maxStartBinding = new System.Windows.Data.Binding("MaxCanStartOrder");
                maxStartBinding.Source = building;
                maxStartBinding.ValidationRules.Add(new IsValidNumberOrderRule());
                maxTextBox.SetBinding(TextBox.TextProperty, maxStartBinding);

                grid.Children.Add(name);
                grid.Children.Add(collect);
                grid.Children.Add(start);
                grid.Children.Add(max);
                grid.Children.Add(maxTextBox);

                border.Child = grid;

                return border;
            }

            public class IsValidNumberOrderRule : ValidationRule
            {
                public override ValidationResult Validate(object value, CultureInfo cultureInfo)
                {
                    var str = value as string;
                    if (str == null)
                    {
                        return new ValidationResult(false, "Please enter a number between 0 and 100.");
                    }
                    uint test;
                    if (uint.TryParse(str, out test) && test<0 || test > 100)
                    {
                        return new ValidationResult(false, "Please enter a number between 0 and 100.");
                    }
                    return new ValidationResult(true, null);

                }
            }



        }




    }
}