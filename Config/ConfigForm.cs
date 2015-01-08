#region

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx.Helpers;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using ListView = System.Windows.Controls.ListView;
using Orientation = System.Windows.Controls.Orientation;
using TabControl = System.Windows.Controls.TabControl;
using TextBox = System.Windows.Controls.TextBox;
using System;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

#endregion

namespace GarrisonButler.Config
{
    public partial class ConfigForm : Form
    {
        private static MyWindow _myWindow;
        private static List<CheckBox> collectCheckBoxes;
        private static List<CheckBox> startCheckBoxes;
        private static MailingTab mailingTab;

        public ConfigForm()
        {
            Close();
            if (_myWindow == null)
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
                MinWidth = 300;
                Title = GarrisonButler.NameStatic + " v" + GarrisonButler.Version;
                var tabControl = new TabControl();
                tabControl.Height = double.NaN;
                tabControl.Width = double.NaN;

                //Splash screen
                var splashTabItem = new TabItem { Header = "Welcome", Content = ContentTabSplash() };
                tabControl.Items.Add(splashTabItem);

                var generalTabItem = new TabItem {Header = "General", Content = ContentTabGeneral()};
                tabControl.Items.Add(generalTabItem);

                var WorkOrderTabItem = new TabItem {Header = "Work Orders", Content = ContentTabWorkOrder()};
                tabControl.Items.Add(WorkOrderTabItem);

                var ProfessionTabItem = new TabItem {Header = "Professions", Content = ContentTabProfession()};
                tabControl.Items.Add(ProfessionTabItem);
                
                mailingTab = new MailingTab();
                var Mailing = new TabItem { Header = "Mailing", Content = mailingTab.ContentTabMailing() };
                tabControl.Items.Add(Mailing);

                var aboutTabItem = new TabItem { Header = "About", Content = ContentTabAbout() };
                tabControl.Items.Add(aboutTabItem);
                
                Content = tabControl;
            }

            public List<CheckBox> AlldailiesCheckbox { get; set; }

            private UIElement ProfessionBox(List<DailyProfession> dailies)
            {
                var border = new Border();
                border.HorizontalAlignment = HorizontalAlignment.Left;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                var name = new Label();
                name.Content = dailies.GetEmptyIfNull().FirstOrDefault() == default(DailyProfession)
                    ? "Error"
                    : dailies.GetEmptyIfNull().FirstOrDefault().TradeskillId.ToString();
                name.FontSize = 14;
                name.FontWeight = FontWeights.Black;
                name.HorizontalAlignment = HorizontalAlignment.Left;
                name.VerticalAlignment = VerticalAlignment.Top;
                name.Width = 163;
                name.Height = 28;
                name.Margin = new Thickness(10, 5, 0, 0);

                CheckBox daily1 = CreateCheckBoxWithBindingBuilding(dailies.GetEmptyIfNull().FirstOrDefault() == default(DailyProfession)
                    ? "Error" : dailies.GetEmptyIfNull().FirstOrDefault().Name, "Activated",
                    dailies.GetEmptyIfNull().FirstOrDefault());
                daily1.Margin = new Thickness(10, 33, 0, 0);
                AlldailiesCheckbox.Add(daily1);

                CheckBox daily2 = CreateCheckBoxWithBindingBuilding(dailies.GetEmptyIfNull().Count() < 2
                    ? "Error" : dailies.GetEmptyIfNull().ElementAt(1).Name, "Activated",
                    dailies.ElementAt(1));
                daily2.Margin = new Thickness(10, 60, 0, 0);
                AlldailiesCheckbox.Add(daily2);


                grid.Children.Add(name);
                grid.Children.Add(daily1);
                grid.Children.Add(daily2);

                border.Child = grid;

                return border;
            }

            protected object ContentTabProfession()
            {
                AlldailiesCheckbox = new List<CheckBox>();

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;

                var gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                var gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                var gridmainRow2 = new RowDefinition();
                gridmainRow2.Height = new GridLength(30);
                grid.RowDefinitions.Add(gridmainRow2);


                var mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                var mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Horizontal;
                mainWrapPanel.Width = double.NaN;
                List<List<DailyProfession>> listOflists = GaBSettings.Get().DailySettings
                    .GetEmptyIfNull()
                    .Select((x, i) => new {Index = x.TradeskillId, Value = x})
                    .GroupBy(x => x.Index)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (List<DailyProfession> dailies in listOflists)
                {
                    UIElement b = ProfessionBox(dailies);
                    mainWrapPanel.Children.Add(b);
                }


                mainFrame.Content = mainWrapPanel;
                Grid.SetColumn(mainFrame, 0);
                Grid.SetRow(mainFrame, 0);
                grid.Children.Add(mainFrame);


                //  bar
                var barPanel = new Grid();
                barPanel.Width = double.NaN;
                barPanel.Height = 30;
                barPanel.MinHeight = 30;
                barPanel.MinWidth = 250;
                barPanel.VerticalAlignment = VerticalAlignment.Bottom;
                barPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                var gridCol1 = new ColumnDefinition();
                var gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                var gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(30);
                barPanel.RowDefinitions.Add(gridRow1);

                var Select = new Button();
                Select.Content = "Select All";
                Select.Tag = 1;
                Select.Click += SelectAllProfession_Click;
                Grid.SetRow(Select, 0);
                Grid.SetColumn(Select, 0);
                barPanel.Children.Add(Select);

                var StartAll = new Button();
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

            protected object ContentTabWorkOrder()
            {
                startCheckBoxes = new List<CheckBox>();
                collectCheckBoxes = new List<CheckBox>();

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;

                var gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                var gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                var gridmainRow2 = new RowDefinition();
                gridmainRow2.Height = new GridLength(30);
                grid.RowDefinitions.Add(gridmainRow2);


                var mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                var mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Horizontal;
                mainWrapPanel.Width = double.NaN;

                foreach (BuildingSettings buildingsSetting in 
                    GaBSettings.Get().BuildingsSettings.GetEmptyIfNull().Where(bs=> Building.HasOrder((buildings)bs.BuildingIds.GetEmptyIfNull().FirstOrDefault()))
                    .OrderBy(b => b.Name))
                {
                    UIElement b = BuildingBox(buildingsSetting);
                    mainWrapPanel.Children.Add(b);
                }
                mainFrame.Content = mainWrapPanel;

                Grid.SetRow(mainFrame, 0);
                Grid.SetColumn(mainFrame, 0);

                grid.Children.Add(mainFrame);

                //  bar
                var barPanel = new Grid();
                barPanel.Width = double.NaN;
                barPanel.Height = 30;
                barPanel.MinHeight = 30;
                barPanel.MinWidth = 250;
                barPanel.VerticalAlignment = VerticalAlignment.Bottom;
                barPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                var gridCol1 = new ColumnDefinition();
                var gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                var gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(30);
                barPanel.RowDefinitions.Add(gridRow1);

                var collectAll = new Button();
                collectAll.Content = "Select Collect All";
                collectAll.Tag = 1;
                collectAll.Click += newBtn_Click;
                Grid.SetRow(collectAll, 0);
                Grid.SetColumn(collectAll, 0);
                barPanel.Children.Add(collectAll);

                var StartAll = new Button();
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
            
            private void SendBugReport(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                GarrisonButler.Diagnostic("zzzzzzzzzz");
                if (btn != null)
                {
                    GarrisonButler.Diagnostic("yyyyyyyyyyyyyy");
                    new FreshDesk().SendBugReport("TEST title", "TEst body");
                }
            }
            private void SelectAllProfession_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    foreach (CheckBox checkBox in AlldailiesCheckbox)
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
                    foreach (CheckBox checkBox in AlldailiesCheckbox)
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
                        switch ((int) btn.Tag)
                        {
                            case 1:
                                foreach (CheckBox checkBox in collectCheckBoxes)
                                {
                                    checkBox.IsChecked = true;
                                    btn.Tag = 2;
                                    btn.Content = "Unselect Collect All";
                                }
                                break;

                            case 2:
                                foreach (CheckBox checkBox in collectCheckBoxes)
                                {
                                    checkBox.IsChecked = false;
                                    btn.Tag = 1;
                                    btn.Content = "Select Collect All";
                                }
                                break;

                            case 3:
                                foreach (CheckBox checkBox in startCheckBoxes)
                                {
                                    checkBox.IsChecked = true;
                                    btn.Tag = 4;
                                    btn.Content = "Unselect Start All";
                                }
                                break;

                            case 4:
                                foreach (CheckBox checkBox in startCheckBoxes)
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

            protected object ContentTabAbout()
            {
                var grid = new Grid();

                grid.Height = double.NaN;
                grid.Width = double.NaN;

                var mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                var mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Vertical;
                mainWrapPanel.Width = double.NaN;

                var Submit = new Button();
                Submit.Content = "Submit";
                Submit.Click += SendBugReport;
                mainWrapPanel.Children.Add(Submit);
                
                mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }
            object ContentTabSplash()
            {
                var grid = new Grid();

                grid.Height = double.NaN;
                grid.Width = double.NaN;

                var mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                var mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = System.Windows.Controls.Orientation.Vertical;
                mainWrapPanel.Width = double.NaN;

                ImageBrush splashBrush = new ImageBrush();
                BitmapImage myImage = new BitmapImage();
                System.IO.MemoryStream myMemStream = new System.IO.MemoryStream();
                System.Drawing.Bitmap garrisonButlerSplashImage =
                    GarrisonButler.NameStatic == "GarrisonButler ICE"
                    ? GarrisonButlerImages.GarrisonButlerICESplashImage
                    : GarrisonButlerImages.GarrisonButlerLiteSplashImage;
                garrisonButlerSplashImage.Save(myMemStream, garrisonButlerSplashImage.RawFormat);
                myMemStream.Seek(0, System.IO.SeekOrigin.Begin);

                myImage.BeginInit();
                myImage.StreamSource = myMemStream;
                myImage.EndInit();

                splashBrush.ImageSource = myImage;
                mainWrapPanel.Background = splashBrush;

                mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }
            protected object ContentTabGeneral()
            {
                var grid = new Grid();

                grid.Height = double.NaN;
                grid.Width = double.NaN;

                var mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                var mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Vertical;
                mainWrapPanel.Width = double.NaN;

                CheckBox UseGarrisonHearthstone = CreateCheckBoxWithBinding("Use Garrison Hearthstone",
                    "UseGarrisonHearthstone", GaBSettings.Get());
                mainWrapPanel.Children.Add(UseGarrisonHearthstone);

                CheckBox HBRelogMode =
                    CreateCheckBoxWithBinding(
                        "Activate HBRelog Mode: auto skip to next task when done (Cautious, not tested!)", "HBRelogMode",
                        GaBSettings.Get());
                mainWrapPanel.Children.Add(HBRelogMode);


                CheckBox GarrisonCache = CreateCheckBoxWithBinding("Collect garrison cache", "GarrisonCache",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(GarrisonCache);


                CheckBox HarvestGarden = CreateCheckBoxWithBinding("Harvest herbs in garden", "HarvestGarden",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(HarvestGarden);


                CheckBox HarvestMine = CreateCheckBoxWithBinding("Harvest ores in mine", "HarvestMine",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(HarvestMine);

                CheckBox UseCoffee = CreateCheckBoxWithBinding("Use coffee in mine", "UseCoffee", GaBSettings.Get());
                mainWrapPanel.Children.Add(UseCoffee);

                CheckBox UseMiningPick = CreateCheckBoxWithBinding("Use mining pick in mine", "UseMiningPick",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(UseMiningPick);

                CheckBox DeleteCoffee = CreateCheckBoxWithBinding("Delete coffee when at 5.", "DeleteCoffee", GaBSettings.Get());
                mainWrapPanel.Children.Add(DeleteCoffee);

                CheckBox DeleteMiningPick = CreateCheckBoxWithBinding("Delete mining when at 5.", "DeleteMiningPick",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(DeleteMiningPick);


                CheckBox ActivateBuildings = CreateCheckBoxWithBinding("Activate finished buildings",
                    "ActivateBuildings", GaBSettings.Get());
                mainWrapPanel.Children.Add(ActivateBuildings);


                CheckBox SalvageCrates = CreateCheckBoxWithBinding("Open Salvage crates", "SalvageCrates",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(SalvageCrates);


                CheckBox StartMissions = CreateCheckBoxWithBinding("Start missions if possible", "StartMissions",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(StartMissions);

                CheckBox CompletedMissions = CreateCheckBoxWithBinding("Turn in completed missions", "CompletedMissions",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(CompletedMissions);


                mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }

            protected CheckBox CreateCheckBoxWithBinding(string Label, string AttributeName, object source)
            {
                var checkBox = new CheckBox();
                checkBox.Content = Label;
                checkBox.Height = 25;
                // binding
                var binding = new Binding(AttributeName);
                binding.Source = source;
                checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
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

            protected override void OnClosing(CancelEventArgs e)
            {
                //do my stuff before closing
                GaBSettings.Save();
                base.OnClosing(e);
                _myWindow = null;
            }

            private UIElement BuildingBox(BuildingSettings building)
            {
                var border = new Border();
                border.HorizontalAlignment = HorizontalAlignment.Left;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);

                var grid = new Grid();
                grid.Height = double.NaN;
                grid.Width = double.NaN;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                var name = new Label();
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

                var max = new Label();
                max.HorizontalAlignment = HorizontalAlignment.Left;
                max.VerticalAlignment = VerticalAlignment.Top;
                max.Width = 150;
                max.Height = 23;
                max.Margin = new Thickness(9, 75, 0, 0);
                max.Content = "Max (0 = unlimited):";

                var maxTextBox = new TextBox();
                maxTextBox.Height = 23;
                maxTextBox.TextWrapping = TextWrapping.Wrap;
                maxTextBox.Text = "0";
                maxTextBox.Margin = new Thickness(127, 76, 11, 9);
                maxTextBox.MaxLength = 3;
                // binding
                var maxStartBinding = new Binding("MaxCanStartOrder");
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
                    if (uint.TryParse(str, out test) && test < 0 || test > 100)
                    {
                        return new ValidationResult(false, "Please enter a number between 0 and 100.");
                    }
                    return new ValidationResult(true, null);
                }
            }
        }
    }
}