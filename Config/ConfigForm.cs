#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using Orientation = System.Windows.Controls.Orientation;
using TabControl = System.Windows.Controls.TabControl;
using TextBox = System.Windows.Controls.TextBox;
using WebBrowser = System.Windows.Forms.WebBrowser;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MessageBox = System.Windows.Forms.MessageBox;

#endregion

namespace GarrisonButler.Config
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ConfigForm : Form
    {
        private static MyWindow _myWindow;
        private static List<CheckBox> _collectCheckBoxes;
        private static List<CheckBox> _startCheckBoxes;
        public string firstName = "Manas";
        public ConfigForm()
        {
            InitializeComponent();

            ////Close();
            //if (_myWindow == null)
            //    _myWindow = new MyWindow();
            
            //_myWindow.Activate();
            //_myWindow.Show();
        }
        public void Test(String message)
        {
            MessageBox.Show(message, "client code");
        }
        public string Test2()
        {
            return "my Message";
        }
        private void ConfigForm_Load_1(object sender, EventArgs e)
        {
            var html = ResourceWebUI.mainUI_html;
            html = ReplaceFromFilesToResources(html);
            
            webBrowser1.ScriptErrorsSuppressed = false;
            webBrowser1.AllowWebBrowserDrop = false;
            webBrowser1.AllowNavigation = false;
            
            webBrowser1.IsWebBrowserContextMenuEnabled = false;
            webBrowser1.WebBrowserShortcutsEnabled = false;
            webBrowser1.ObjectForScripting = GaBSettings.Get();
            webBrowser1.DocumentText = html;


            //var window = webBrowser1.Document.Window;
            //return window.GetType().InvokeMember(propertyName, BindingFlags.GetProperty, null, window, null);

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //do my stuff before closing
            GaBSettings.Save();
            base.OnClosing(e);
        }

        private string ReplaceFromFilesToResources(string html)
        {
            Dictionary<string, string> dictJs = new Dictionary<string, string>()
            {
                {"<script src=\"./mainUI.js\"></script>", ResourceWebUI.mainUI_js},
                
                {"<script src=\"./Modules/general-tab.module.js\"></script>", ResourceWebUI.general_tab_module_js},

                {"<script src=\"./Modules/work-order-tab.module.js\"></script>", ResourceWebUI.work_order_tab_module_js},

                {"<script src=\"./Modules/profession-tab.module.js\"></script>", ResourceWebUI.profession_tab_module_js},

                {"<script src=\"./Modules/mailing-tab.module.js\"></script>", ResourceWebUI.mailing_tab_module_js},
                
                {"<script src=\"./Modules/milling-tab.module.js\"></script>", ResourceWebUI.milling_tab_module_js},

                {"<script src=\"./Modules/missions-tab.module.js\"></script>", ResourceWebUI.missions_tab_module_js},

                {"<script src=\"./Modules/trading-post.module.js\"></script>", ResourceWebUI.trading_post_module},

                {"<script src=\"./Libraries/SmartTable/smart-table.min.js\"></script>", ResourceWebUI.smart_table_min},

                {"<script src=\"./Libraries/slider/slider.js\"></script>", ResourceWebUI.slider_js},

                {"<script src=\"./Libraries/angular-xeditable/js/xeditable.min.js\"></script>", ResourceWebUI.xeditable_min_js},

                //{"<script src=\"./Libraries/Angular/angular.min.js\"></script>", ResourceWebUI.angular_min_js},
                
                //{"<script src=\"./Libraries/Angular/angular-route.min.js\"></script>", ResourceWebUI.angular_route_min_js},
     
            };
            Dictionary<string, string> dictCss = new Dictionary<string, string>()
            {
                {"<link rel=\"stylesheet\" href=\"./Libraries/angular-xeditable/css/xeditable.css\" />", ResourceWebUI.xeditable_css},

                {"<link rel=\"stylesheet\" href=\"./mainUI.css\" />", ResourceWebUI.mainUI_css},
                
            };
            foreach (var file in dictJs)
            {
                if (file.Key == null)
                {
                    MessageBox.Show("Key null");

                }
                else if (file.Value == null)
                {
                    MessageBox.Show("value null for key: " + file.Key);
                }
                else
                {
                    html = html.Replace(file.Key, "<script>" + file.Value + "</script>");
                }
            }
            foreach (var file in dictCss)
            {
                if (file.Key == null)
                {
                    MessageBox.Show("css Key null");

                }
                else if (file.Value == null)
                {
                    MessageBox.Show("css value null for key: " + file.Key);
                }
                else
                {
                    html = html.Replace(file.Key, "<style>" + file.Value + "</style>");
                }
            }
            return html;
        }








        [ComVisible(true)]
        public class ViewInterface
        {
            private static ViewInterface instance;
            public static ViewInterface Instance
            {
                get { return instance ?? new ViewInterface(); }
                set { instance = value; }
            }
            
            private ViewInterface()
            {}


            [ComVisible(true)]
            public void UpdateBooleanValue(string propertyName, bool value)
            {
                var prop = GetType().GetProperty(propertyName);
                GarrisonButler.Diagnostic("Update called for {0}, old value={1}, new value={2}", propertyName, prop.GetValue(this), value);
                prop.SetValue(this, value);
            }

            [ComVisible(true)]
            public bool GetBooleanValue(string propertyName)
            {
                var prop = GetType().GetProperty(propertyName);
                GarrisonButler.Diagnostic("GetValue called for {0}, old value={1}", propertyName, prop.GetValue(this));
                return (bool)prop.GetValue(this);
            }
            
        
        
        }












        public class MyWindow : Window
        {
            public MyWindow()
            {
                Width = 600;
                Height = 400;
                MinHeight = 400;
                MinWidth = 600;
                Title = GarrisonButler.NameStatic + " v" + GarrisonButler.Version;
                var tabControl = new TabControl {Height = double.NaN, Width = double.NaN};

                //Splash screen
                var splashTabItem = new TabItem {Header = "Welcome", Content = ContentTabSplash()};
                tabControl.Items.Add(splashTabItem);

                var generalTabItem = new TabItem {Header = "General", Content = ContentTabGeneral()};
                tabControl.Items.Add(generalTabItem);

                var workOrderTabItem = new TabItem {Header = "Work Orders", Content = ContentTabWorkOrder()};
                tabControl.Items.Add(workOrderTabItem);

                var missionTabItem = new TabItem { Header = "Missions", Content = ContentTabMissions() };
                tabControl.Items.Add(missionTabItem);

                var professionTabItem = new TabItem {Header = "Professions", Content = ContentTabProfession()};
                tabControl.Items.Add(professionTabItem);

                if (GarrisonButler.IsIceVersion())
                {
                    var mailingTab = new MailingTab();
                    var mailing = new TabItem { Header = "Mailing", Content = mailingTab.ContentTabMailing() };
                    tabControl.Items.Add(mailing);
                
                    var millingTab = new MillingTab();
                    var milling = new TabItem { Header = "Milling", Content = millingTab.ContentTabMilling() };
                    tabControl.Items.Add(milling);
                }

                var tradingPostTab = new TradingPostTab();
                var tradingPost = new TabItem { Header = "Trading Post", Content = tradingPostTab.ContentTradingPostTab() };
                tabControl.Items.Add(tradingPost);

                var aboutTabItem = new TabItem {Header = "About", Content = ContentTabAbout()};
                tabControl.Items.Add(aboutTabItem);

                Content = tabControl;
            }

            public List<CheckBox> AlldailiesCheckbox { get; set; }

            private System.Windows.Controls.ComboBox _optimizeForListBox = new System.Windows.Controls.ComboBox();

            private UIElement ProfessionBox(List<DailyProfession> dailies)
            {
                var border = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2)
                };

                var grid = new Grid
                {
                    Height = double.NaN,
                    Width = double.NaN,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                    
                }; 
                var name = new Label();
                var dailyProfession = dailies.GetEmptyIfNull().FirstOrDefault();
                name.Content = dailyProfession != null ? dailyProfession.TradeskillId.ToString() : "Error";
                name.FontSize = 14;
                name.FontWeight = FontWeights.Black;
                name.HorizontalAlignment = HorizontalAlignment.Left;
                name.VerticalAlignment = VerticalAlignment.Top;
                name.Width = 163;
                name.Height = 28;
                name.Margin = new Thickness(10, 5, 0, 0);
                var daily1 = CreateCheckBoxWithBindingBuilding(dailyProfession != null
                    ? dailyProfession.Name
                    : "Error: Unknown building name", "Activated", dailyProfession);
                daily1.Margin = new Thickness(10, 33, 0, 0);
                AlldailiesCheckbox.Add(daily1);
                var daily2 = CreateCheckBoxWithBindingBuilding(dailies.GetEmptyIfNull().Count() < 2
                    ? "Error"
                    : dailies.GetEmptyIfNull().ElementAt(1).Name, "Activated", dailies.ElementAt(1));
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

                var grid = new Grid {Height = double.NaN, Width = double.NaN};

                var gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                var gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                var gridmainRow2 = new RowDefinition {Height = new GridLength(30)};
                grid.RowDefinitions.Add(gridmainRow2);


                var mainFrame = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};

                var mainWrapPanel = new WrapPanel {Orientation = Orientation.Horizontal, Width = double.NaN};
                var listOflists = GaBSettings.Get().DailySettings
                    .GetEmptyIfNull()
                    .Select((x, i) => new {Index = x.TradeskillId, Value = x})
                    .GroupBy(x => x.Index)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var b in listOflists.Select(ProfessionBox))
                {
                    mainWrapPanel.Children.Add(b);
                }


                mainFrame.Content = mainWrapPanel;
                Grid.SetColumn(mainFrame, 0);
                Grid.SetRow(mainFrame, 0);
                grid.Children.Add(mainFrame);


                //  bar
                var barPanel = new Grid
                {
                    Width = double.NaN,
                    Height = 30,
                    MinHeight = 30,
                    MinWidth = 250,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var gridCol1 = new ColumnDefinition();
                var gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                var gridRow1 = new RowDefinition {Height = new GridLength(30)};
                barPanel.RowDefinitions.Add(gridRow1);

                var select = new Button();
                select.Content = "Select All";
                select.Tag = 1;
                select.Click += SelectAllProfession_Click;
                Grid.SetRow(select, 0);
                Grid.SetColumn(select, 0);
                barPanel.Children.Add(select);

                var startAll = new Button {Content = "Unselect All", Tag = 3};
                startAll.Click += UnSelectAllProfession_Click;
                Grid.SetRow(startAll, 0);
                Grid.SetColumn(startAll, 1);
                barPanel.Children.Add(startAll);

                Grid.SetRow(barPanel, 1);
                Grid.SetColumn(barPanel, 0);
                grid.Children.Add(barPanel);


                return grid;
            }

            protected object ContentTabWorkOrder()
            {
                _startCheckBoxes = new List<CheckBox>();
                _collectCheckBoxes = new List<CheckBox>();

                var grid = new Grid {Height = double.NaN, Width = double.NaN};

                var gridmainCol1 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(gridmainCol1);

                var gridmainRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridmainRow1);

                var gridmainRow2 = new RowDefinition {Height = new GridLength(30)};
                grid.RowDefinitions.Add(gridmainRow2);


                var mainFrame = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};

                var mainWrapPanel = new WrapPanel {Orientation = Orientation.Horizontal, Width = double.NaN};

                foreach (
                    var b in
                        GaBSettings.Get()
                            .BuildingsSettings.GetEmptyIfNull()
                            .Where(bs => Building.HasOrder((Buildings) bs.BuildingIds.GetEmptyIfNull().FirstOrDefault()))
                            .OrderBy(b => b.Name).Select(BuildingBox))
                {
                    mainWrapPanel.Children.Add(b);
                }
                mainFrame.Content = mainWrapPanel;

                Grid.SetRow(mainFrame, 0);
                Grid.SetColumn(mainFrame, 0);

                grid.Children.Add(mainFrame);

                //  bar
                var barPanel = new Grid
                {
                    Width = double.NaN,
                    Height = 30,
                    MinHeight = 30,
                    MinWidth = 250,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var gridCol1 = new ColumnDefinition();
                var gridCol2 = new ColumnDefinition();
                barPanel.ColumnDefinitions.Add(gridCol1);
                barPanel.ColumnDefinitions.Add(gridCol2);

                var gridRow1 = new RowDefinition {Height = new GridLength(30)};
                barPanel.RowDefinitions.Add(gridRow1);

                var collectAll = new Button {Content = "Select Collect All", Tag = 1};
                collectAll.Click += newBtn_Click;
                Grid.SetRow(collectAll, 0);
                Grid.SetColumn(collectAll, 0);
                barPanel.Children.Add(collectAll);

                var startAll = new Button {Content = "Select Start All", Tag = 3};
                startAll.Click += newBtn_Click;
                Grid.SetRow(startAll, 0);
                Grid.SetColumn(startAll, 1);
                barPanel.Children.Add(startAll);

                Grid.SetRow(barPanel, 1);
                Grid.SetColumn(barPanel, 0);
                grid.Children.Add(barPanel);

                return grid;
            }

            private static void SendBugReport(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                GarrisonButler.Diagnostic("zzzzzzzzzz");
                if (btn == null) return;
                GarrisonButler.Diagnostic("yyyyyyyyyyyyyy");
                new FreshDesk().SendBugReport("TEST title", "TEst body");
            }

            private void SelectAllProfession_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn == null) return;
                foreach (var checkBox in AlldailiesCheckbox)
                {
                    checkBox.IsChecked = true;
                }
            }

            private void UnSelectAllProfession_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn == null) return;
                foreach (var checkBox in AlldailiesCheckbox)
                {
                    checkBox.IsChecked = false;
                }
            }

            private static void newBtn_Click(object sender, RoutedEventArgs e)
            {
                var btn = sender as Button;
                if (btn == null) return;
                if (!(btn.Tag is int)) return;
                switch ((int) btn.Tag)
                {
                    case 1:
                        foreach (var checkBox in _collectCheckBoxes)
                        {
                            checkBox.IsChecked = true;
                            btn.Tag = 2;
                            btn.Content = "Unselect Collect All";
                        }
                        break;

                    case 2:
                        foreach (var checkBox in _collectCheckBoxes)
                        {
                            checkBox.IsChecked = false;
                            btn.Tag = 1;
                            btn.Content = "Select Collect All";
                        }
                        break;

                    case 3:
                        foreach (var checkBox in _startCheckBoxes)
                        {
                            checkBox.IsChecked = true;
                            btn.Tag = 4;
                            btn.Content = "Unselect Start All";
                        }
                        break;

                    case 4:
                        foreach (var checkBox in _startCheckBoxes)
                        {
                            checkBox.IsChecked = false;
                            btn.Tag = 3;
                            btn.Content = "Select Start All";
                        }
                        break;
                }
            }

            protected object ContentTabAbout()
            {
                var mainFrame = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};

                //var mainWrapPanel = new WrapPanel {Orientation = Orientation.Vertical, Width = double.NaN};

                //var submit = new Button {Content = "Submit"};
                //submit.Click += SendBugReport;
                //mainWrapPanel.Children.Add(submit);

                //mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }
           

            private static object ContentTabSplash()
            {
                var mainFrame = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};

                var mainWrapPanel = new WrapPanel {Orientation = Orientation.Vertical, Width = double.NaN};

                var splashBrush = new ImageBrush();
                var myImage = new BitmapImage();
                var myMemStream = new MemoryStream();
                var garrisonButlerSplashImage =
                    GarrisonButler.IsIceVersion()
                        ? GarrisonButlerImages.GarrisonButlerICESplashImage
                        : GarrisonButlerImages.GarrisonButlerLiteSplashImage;
                garrisonButlerSplashImage.Save(myMemStream, garrisonButlerSplashImage.RawFormat);
                myMemStream.Seek(0, SeekOrigin.Begin);

                myImage.BeginInit();
                myImage.StreamSource = myMemStream;
                myImage.EndInit();

                splashBrush.ImageSource = myImage;
                mainWrapPanel.Background = splashBrush;

                mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }

            protected object ContentTabMissions()
            {
                var mainFrame = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

                var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Width = double.NaN };

                var startMissions = CreateCheckBoxWithBinding("Start missions if possible", "StartMissions",
                    GaBSettings.Get());
                stackPanel.Children.Add(startMissions);

                var completedMissions = CreateCheckBoxWithBinding("Turn in completed missions", "CompletedMissions",
                    GaBSettings.Get());
                stackPanel.Children.Add(completedMissions);

                //var excludeEpicMaxLevel = CreateCheckBoxWithBinding("Exclude Epic Max Level followers from experience considerations", "ExcludeEpicMaxLevelFollowersForExperience",
                //   GaBSettings.Get());
                //stackPanel.Children.Add(excludeEpicMaxLevel);

                //var optimizeFor = new Label
                //{
                //    VerticalAlignment = VerticalAlignment.Center,
                //    Content = "Optimize For:",
                //    Margin = new Thickness(5, 0, 5, 0)
                //};
                //stackPanel.Children.Add(optimizeFor);

                //_optimizeForListBox = new System.Windows.Controls.ComboBox
                //{
                //    Width = double.NaN,
                //    MinWidth = 40,
                //    VerticalContentAlignment = VerticalAlignment.Center,
                //    ItemsSource = new List<string>()
                //    {
                //        "Experience",
                //        "Apexis Crystal",
                //        "Seal of Tempered Fate",
                //        "Garrison Resources",
                //        "Uncommon (Green) Gear",
                //        "Rare (Blue) Gear",
                //        "Epic (Purple) Gear",
                //        "Armor Enhancement Token",
                //        "Weapon Enhancement Token",
                //        "Balanced Weapon Enhancement",
                //        ""
                //    }
                //};
                //_optimizeForListBox.SelectionChanged += OnOptimizeForListBoxOnSelectionChanged;
                //stackPanel.Children.Add(_optimizeForListBox);

                mainFrame.Content = stackPanel;
                return mainFrame;
            }

            private void OnOptimizeForListBoxOnSelectionChanged(object o, SelectionChangedEventArgs e)
            {
                //if (CurrentBinding != null)
                //{
                //    var selected = _addRuleListBox.SelectedItem as MailCondition;
                //    if (selected != null)
                //    {
                //        selected.CheckValue = _addRuleValueTextBox.Text.ToInt32();
                //        CurrentBinding.Condition = selected;
                //    }
                //}
            }

            protected object ContentTabGeneral()
            {
                var mainFrame = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};

                var mainWrapPanel = new WrapPanel {Orientation = Orientation.Vertical, Width = double.NaN};

                var useGarrisonHearthstone = CreateCheckBoxWithBinding("Use Garrison Hearthstone",
                    "UseGarrisonHearthstone", GaBSettings.Get());
                mainWrapPanel.Children.Add(useGarrisonHearthstone);

                var hbRelogMode =
                    CreateCheckBoxWithBinding(
                        "Activate HBRelog Mode: auto skip to next task when done.", "HbRelogMode",
                        GaBSettings.Get());
                mainWrapPanel.Children.Add(hbRelogMode);


                var garrisonCache = CreateCheckBoxWithBinding("Collect garrison cache", "GarrisonCache",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(garrisonCache);


                var harvestGarden = CreateCheckBoxWithBinding("Harvest herbs in garden", "HarvestGarden",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(harvestGarden);


                var harvestMine = CreateCheckBoxWithBinding("Harvest ores in mine", "HarvestMine",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(harvestMine);

                var useCoffee = CreateCheckBoxWithBinding("Use coffee in mine", "UseCoffee", GaBSettings.Get());
                mainWrapPanel.Children.Add(useCoffee);

                var useMiningPick = CreateCheckBoxWithBinding("Use mining pick in mine", "UseMiningPick",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(useMiningPick);

                var deleteCoffee = CreateCheckBoxWithBinding("Delete coffee when at 5.", "DeleteCoffee",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(deleteCoffee);

                var deleteMiningPick = CreateCheckBoxWithBinding("Delete mining when at 5.", "DeleteMiningPick",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(deleteMiningPick);


                var activateBuildings = CreateCheckBoxWithBinding("Activate finished buildings",
                    "ActivateBuildings", GaBSettings.Get());
                mainWrapPanel.Children.Add(activateBuildings);


                var salvageCrates = CreateCheckBoxWithBinding("Open Salvage crates", "SalvageCrates",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(salvageCrates);


                //var startMissions = CreateCheckBoxWithBinding("Start missions if possible", "StartMissions",
                //    GaBSettings.Get());
                //mainWrapPanel.Children.Add(startMissions);

                //var completedMissions = CreateCheckBoxWithBinding("Turn in completed missions", "CompletedMissions",
                //    GaBSettings.Get());
                //mainWrapPanel.Children.Add(completedMissions);

                var forceJunkSell = CreateCheckBoxWithBinding("Force auto sell grey items", "ForceJunkSell",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(forceJunkSell);

                var disableLastRoundCheck = CreateCheckBoxWithBinding("Disable last round check", "DisableLastRoundCheck",
                    GaBSettings.Get());
                mainWrapPanel.Children.Add(disableLastRoundCheck);

                mainFrame.Content = mainWrapPanel;
                return mainFrame;
            }

            protected CheckBox CreateCheckBoxWithBinding(string label, string attributeName, object source)
            {
                var checkBox = new CheckBox {Content = label, Height = 25};
                // binding
                var binding = new Binding(attributeName) {Source = source};
                binding.UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged;
                checkBox.SetBinding(ToggleButton.IsCheckedProperty, binding);
                return checkBox;
            }

            protected CheckBox CreateCheckBoxWithBindingBuilding(string label, string attributeName, object source)
            {
                var checkBox = CreateCheckBoxWithBinding(label, attributeName, source);
                checkBox.HorizontalAlignment = HorizontalAlignment.Left;
                checkBox.VerticalAlignment = VerticalAlignment.Top;
                checkBox.Height = 23;
                checkBox.Width = 150;
                return checkBox;
            }

            protected override void OnClosing(CancelEventArgs e)
            {
                //do my stuff before closing
                base.OnClosing(e);
                _myWindow = null;
                GaBSettings.Save();
            }

            private UIElement BuildingBox(BuildingSettings building)
            {
                var border = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2)
                };

                var grid = new Grid
                {
                    Height = double.NaN,
                    Width = double.NaN,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var name = new Label
                {
                    Content = building.Name,
                    FontSize = 14,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 163,
                    Height = 28,
                    Margin = new Thickness(10, 5, 0, 0)
                };

                var collect = CreateCheckBoxWithBindingBuilding("Collect work orders", "CanCollectOrder", building);
                collect.Margin = new Thickness(10, 33, 0, 0);
                _collectCheckBoxes.Add(collect);

                var start = CreateCheckBoxWithBindingBuilding("Start work orders", "CanStartOrder", building);
                start.Margin = new Thickness(10, 60, 0, 0);
                _startCheckBoxes.Add(start);

                var max = new Label
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 150,
                    Height = 23,
                    Margin = new Thickness(9, 75, 0, 0),
                    Content = "Max (0 = unlimited):"
                };

                var maxTextBox = new TextBox
                {
                    Height = 23,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "0",
                    Margin = new Thickness(127, 76, 11, 9),
                    MaxLength = 3
                };
                // binding
                var maxStartBinding = new Binding("MaxCanStartOrder") {Source = building};
                maxStartBinding.UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged;
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
                    if (!uint.TryParse(str, out test) || test > 100)
                    {
                        return new ValidationResult(false, "Please enter a number between 0 and 100.");
                    }
                    return new ValidationResult(true, null);
                }
            }
        }

    }
}