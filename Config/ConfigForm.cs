using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace GarrisonBuddy.Config
{
    public partial class ConfigForm : Form
    {
        private static MyWindow myWindow;
        public ConfigForm()
        {
            this.Close();
            myWindow = new MyWindow();
            myWindow.Activate();
            myWindow.Show();

        }

        public class MyWindow : Window
        {
            private Label label1;

            public MyWindow()
            {
                Width = 600;
                Height = 400;
                MinHeight = 150;
                MinHeight = 170;
                this.Title = "GarrisonBuddy Beta v" + "0.7.0";
                
                var grid = new Grid();
                
                grid.Height = double.NaN;
                grid.Width = double.NaN;


                ScrollViewer mainFrame = new ScrollViewer();
                mainFrame.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                WrapPanel mainWrapPanel = new WrapPanel();
                mainWrapPanel.Orientation = Orientation.Horizontal;
                mainWrapPanel.Width = double.NaN;



                foreach (var buildingsSetting in GaBSettings.Get().BuildingsSettings.OrderBy(b=> b.Name))
                {
                    var b = BuildingBox(buildingsSetting);
                    mainWrapPanel.Children.Add(b);

                }


                mainFrame.Content = mainWrapPanel;
                grid.Children.Add(mainFrame);
                Content = grid;



            }
            protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
            {
                //do my stuff before closing
                GaBSettings.Save();
                base.OnClosing(e);
            }

            private Border BuildingBox(BuildingSettings building)
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

                CheckBox collect = new CheckBox();
                collect.Content = "Collect work orders";
                collect.HorizontalAlignment = HorizontalAlignment.Left;
                collect.VerticalAlignment = VerticalAlignment.Top;
                collect.Margin = new Thickness(10, 33, 0, 0);
                collect.Height = 23;
                collect.Width = 150;
                // binding
                var collectBinding = new System.Windows.Data.Binding("CanCollectOrder");
                collectBinding.Source = building;
                collect.SetBinding(CheckBox.IsCheckedProperty, collectBinding);

                CheckBox start = new CheckBox();
                start.Content = "Start work orders";
                start.HorizontalAlignment = HorizontalAlignment.Left;
                start.VerticalAlignment = VerticalAlignment.Top;
                start.Margin = new Thickness(10, 60, 0, 0);
                start.Width = 150;
                // binding
                var startBinding = new System.Windows.Data.Binding("CanStartOrder");
                startBinding.Source = building;
                start.SetBinding(CheckBox.IsCheckedProperty, startBinding);

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