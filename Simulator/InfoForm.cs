using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace simulator
{
	/// <summary>
	/// Tell user how to do everything with the simulator
	/// </summary>
	public partial class InfoForm : Form
	{
		/// <summary>
		/// Initialize an info screen
		/// </summary>
		public InfoForm()
		{
			InitializeComponent();
		}

		private void Info_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyData == Keys.Escape || e.KeyData == Keys.H)
			{
				Close();
			}
		}

		private void btnMoreInfo_Click(object sender, EventArgs e)
		{
			new MoreInfoForm().ShowDialog(this);
		}
	}
}
