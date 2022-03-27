using System;
using System.Windows.Forms;

namespace sharpq3load_ui
{
    /// <summary>
    /// Simple about box
    /// </summary>
    public partial class AboutBox : Form
    {
        /// <summary>
        /// Allocate about box
        /// </summary>
        public AboutBox()
        {
            InitializeComponent();

            lblDesc.Text = "Loads built in or custom Quake 3 maps, renders them and lets you move around in first person view.";
        }

        private void lblDesc_Click(object sender, EventArgs e)
        {

        }
    }
}
