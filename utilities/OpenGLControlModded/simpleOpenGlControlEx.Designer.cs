namespace OpenGLControlModded
{
	partial class simpleOpenGlControlEx
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}

		#endregion
	
		public event System.Windows.Forms.KeyEventHandler ProcessKey;

		protected virtual void OnProcessKey(System.Windows.Forms.KeyEventArgs e)
		{
			if (ProcessKey != null) 
				ProcessKey(this, e);
		}

		protected override bool ProcessDialogKey(System.Windows.Forms.Keys keyData)
		{
			OnProcessKey(new System.Windows.Forms.KeyEventArgs(keyData));

			return base.ProcessDialogKey(keyData);
		}
	}
}
