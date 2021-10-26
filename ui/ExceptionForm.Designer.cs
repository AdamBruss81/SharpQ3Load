namespace simulator
{
	partial class ExceptionForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_tlMain = new System.Windows.Forms.TableLayoutPanel();
			this.m_rtxtMessage = new System.Windows.Forms.RichTextBox();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_tlMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tlMain
			// 
			this.m_tlMain.ColumnCount = 2;
			this.m_tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
			this.m_tlMain.Controls.Add(this.m_rtxtMessage, 0, 0);
			this.m_tlMain.Controls.Add(this.m_btnClose, 1, 1);
			this.m_tlMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tlMain.Location = new System.Drawing.Point(0, 0);
			this.m_tlMain.Name = "m_tlMain";
			this.m_tlMain.RowCount = 2;
			this.m_tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
			this.m_tlMain.Size = new System.Drawing.Size(646, 350);
			this.m_tlMain.TabIndex = 0;
			// 
			// m_rtxtMessage
			// 
			this.m_tlMain.SetColumnSpan(this.m_rtxtMessage, 2);
			this.m_rtxtMessage.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_rtxtMessage.Location = new System.Drawing.Point(3, 3);
			this.m_rtxtMessage.Name = "m_rtxtMessage";
			this.m_rtxtMessage.Size = new System.Drawing.Size(640, 304);
			this.m_rtxtMessage.TabIndex = 0;
			this.m_rtxtMessage.Text = "";
			// 
			// m_btnClose
			// 
			this.m_btnClose.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnClose.Location = new System.Drawing.Point(569, 318);
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Size = new System.Drawing.Size(74, 23);
			this.m_btnClose.TabIndex = 1;
			this.m_btnClose.Text = "&Close";
			this.m_btnClose.UseVisualStyleBackColor = true;
			// 
			// ExceptionForm
			// 
			this.AcceptButton = this.m_btnClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(646, 350);
			this.Controls.Add(this.m_tlMain);
			this.Name = "ExceptionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Error";
			this.m_tlMain.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel m_tlMain;
		private System.Windows.Forms.RichTextBox m_rtxtMessage;
		private System.Windows.Forms.Button m_btnClose;
	}
}