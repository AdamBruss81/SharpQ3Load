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
	/// Display an error message
	/// </summary>
	public partial class ExceptionForm : Form
	{
		/// <summary>
		/// Initialize the form with error message
		/// </summary>
		/// <param name="sMessage">The message to display</param>
		public ExceptionForm(string sMessage)
		{
			InitializeComponent();

			m_rtxtMessage.Text = sMessage;

			m_btnClose.Focus();
		}
	}
}
