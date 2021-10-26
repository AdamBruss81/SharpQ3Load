using System.Windows.Forms;

#pragma warning disable CS1591

namespace simulator
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
