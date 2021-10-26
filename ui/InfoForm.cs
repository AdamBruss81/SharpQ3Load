using System;
using System.Windows.Forms;

namespace sharpq3load_ui
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
            MoreInfoForm mif = new MoreInfoForm();
            mif.AddText("\n\nThe E key does a forward warp to get through doors and sometimes get up to unreachable places.");
            mif.ShowDialog(this);
		}

		private void m_btnAbout_Click(object sender, EventArgs e)
		{
			AboutBox abt = new AboutBox();
			abt.ShowDialog(this);
		}
	}
}
