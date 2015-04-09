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
        public ConfigForm()
        {
            InitializeComponent();
        }
        private void ConfigForm_Load_1(object sender, EventArgs e)
        {
            var html = ResourceWebUI.mainUI_html;
            html = ReplaceFromFilesToResources(html);
            
            webBrowser1.ScriptErrorsSuppressed = false;
            webBrowser1.AllowWebBrowserDrop = false;
            webBrowser1.AllowNavigation = false;
            webBrowser1.IsWebBrowserContextMenuEnabled = false;
            webBrowser1.WebBrowserShortcutsEnabled = true;
            webBrowser1.ObjectForScripting = GaBSettings.Get();
            webBrowser1.DocumentText = html;

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

                {"<script src=\"./Modules/enchanting-tab.module.js\"></script>", ResourceWebUI.enchanting_tab_module_js},

                {"<script src=\"./Libraries/SmartTable/smart-table.min.js\"></script>", ResourceWebUI.smart_table_min},

                {"<script src=\"./Libraries/slider/slider.js\"></script>", ResourceWebUI.slider_js},
                
                {"<script src=\"./Libraries/angular-xeditable/js/xeditable.min.js\"></script>", ResourceWebUI.xeditable_min_js},
                
                {"<script src=\"./Libraries/Angular/angular.min.js\"></script>", ResourceWebUI.angular_min},

                {"<script src=\"./Libraries/Angular/angular-animate.min.js\"></script>", ResourceWebUI.angular_animate_min},

                {"<script src=\"./Libraries/Angular/angular-aria.min.js\"></script>", ResourceWebUI.angular_aria_min},

                {"<script src=\"./Libraries/Angular/angular-material.min.js\"></script>", ResourceWebUI.angular_material_min_js},

                {"<script src=\"./Libraries/Angular/hammer.min.js\"></script>", ResourceWebUI.hammer_min_js},

                {"<script src=\"./Libraries/Angular/bootstrap.js\"></script>", ResourceWebUI.bootstrap_js},
                
     
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
    }
}