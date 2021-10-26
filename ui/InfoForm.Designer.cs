namespace simulator
{
	partial class InfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoForm));
            this.btnMoreInfo = new System.Windows.Forms.Button();
            this.m_btnAbout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnMoreInfo
            // 
            this.btnMoreInfo.BackColor = System.Drawing.Color.Yellow;
            this.btnMoreInfo.Font = new System.Drawing.Font("Engravers MT", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMoreInfo.Location = new System.Drawing.Point(784, 283);
            this.btnMoreInfo.Name = "btnMoreInfo";
            this.btnMoreInfo.Size = new System.Drawing.Size(105, 52);
            this.btnMoreInfo.TabIndex = 0;
            this.btnMoreInfo.Text = "More Controls";
            this.btnMoreInfo.UseVisualStyleBackColor = false;
            this.btnMoreInfo.Click += new System.EventHandler(this.btnMoreInfo_Click);
            // 
            // m_btnAbout
            // 
            this.m_btnAbout.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.m_btnAbout.Font = new System.Drawing.Font("Engravers MT", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_btnAbout.Location = new System.Drawing.Point(784, 341);
            this.m_btnAbout.Name = "m_btnAbout";
            this.m_btnAbout.Size = new System.Drawing.Size(105, 37);
            this.m_btnAbout.TabIndex = 1;
            this.m_btnAbout.Text = "About";
            this.m_btnAbout.UseVisualStyleBackColor = false;
            this.m_btnAbout.Click += new System.EventHandler(this.m_btnAbout_Click);
            // 
            // InfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::simulator.Properties.Resources.movement1;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(901, 687);
            this.Controls.Add(this.m_btnAbout);
            this.Controls.Add(this.btnMoreInfo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InfoForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Controls";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Info_KeyDown);
            this.ResumeLayout(false);

		}


		#endregion

		private System.Windows.Forms.Button btnMoreInfo;
        private System.Windows.Forms.Button m_btnAbout;
    }
}