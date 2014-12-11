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

        private void ConfigForm_Load(object sender, System.EventArgs e)
        {

        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            GaBSettings.Mono.Save();
        }

    }
}