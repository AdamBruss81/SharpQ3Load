using System.Windows.Forms;

#pragma warning disable CS1591

namespace sharpq3load_ui
{
    public partial class MoreInfoForm : Form
    {
        public MoreInfoForm()
        {
            InitializeComponent();
        }

        public void AddText(string s)
        {
            rtbMoreInfo.AppendText(s);
        }
    }
}
