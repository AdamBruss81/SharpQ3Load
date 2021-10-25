using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace simulator
{
    public partial class AboutBox : Form
    {
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
