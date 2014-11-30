using System.Windows.Forms;

namespace GarrisonBuddy.Config
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = GaBSettings.Mono;
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            GaBSettings.Mono.Save();
        }
    }
}