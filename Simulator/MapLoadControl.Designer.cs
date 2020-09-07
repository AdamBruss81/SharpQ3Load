namespace simulator
{
	partial class MapLoadControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapLoadControl));
            this.m_progress = new System.Windows.Forms.ProgressBar();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.m_lblDetail = new System.Windows.Forms.Label();
            this.m_lblPercentage = new System.Windows.Forms.Label();
            this.m_lblStatus = new System.Windows.Forms.Label();
            this.m_picLevelShot = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_picLevelShot)).BeginInit();
            this.SuspendLayout();
            // 
            // m_progress
            // 
            this.m_progress.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_progress.BackColor = System.Drawing.SystemColors.Control;
            this.tableLayoutPanel1.SetColumnSpan(this.m_progress, 3);
            this.m_progress.ForeColor = System.Drawing.Color.SteelBlue;
            this.m_progress.Location = new System.Drawing.Point(3, 716);
            this.m_progress.MarqueeAnimationSpeed = 500;
            this.m_progress.Name = "m_progress";
            this.m_progress.Size = new System.Drawing.Size(775, 31);
            this.m_progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.m_progress.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 225F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 488F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 68F));
            this.tableLayoutPanel1.Controls.Add(this.m_progress, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.m_lblDetail, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.m_lblPercentage, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.m_lblStatus, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.m_picLevelShot, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(780, 780);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // m_lblDetail
            // 
            this.m_lblDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_lblDetail.BackColor = System.Drawing.Color.Yellow;
            this.m_lblDetail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_lblDetail.Font = new System.Drawing.Font("Stencil", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_lblDetail.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.m_lblDetail.Location = new System.Drawing.Point(228, 750);
            this.m_lblDetail.Name = "m_lblDetail";
            this.m_lblDetail.Size = new System.Drawing.Size(482, 30);
            this.m_lblDetail.TabIndex = 4;
            this.m_lblDetail.Text = "details";
            // 
            // m_lblPercentage
            // 
            this.m_lblPercentage.AutoSize = true;
            this.m_lblPercentage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_lblPercentage.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_lblPercentage.Location = new System.Drawing.Point(716, 750);
            this.m_lblPercentage.Name = "m_lblPercentage";
            this.m_lblPercentage.Size = new System.Drawing.Size(62, 30);
            this.m_lblPercentage.TabIndex = 3;
            this.m_lblPercentage.Text = "100%";
            // 
            // m_lblStatus
            // 
            this.m_lblStatus.AutoSize = true;
            this.m_lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_lblStatus.Location = new System.Drawing.Point(3, 750);
            this.m_lblStatus.Name = "m_lblStatus";
            this.m_lblStatus.Size = new System.Drawing.Size(219, 30);
            this.m_lblStatus.TabIndex = 2;
            this.m_lblStatus.Text = "Loading...";
            // 
            // m_picLevelShot
            // 
            this.m_picLevelShot.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tableLayoutPanel1.SetColumnSpan(this.m_picLevelShot, 3);
            this.m_picLevelShot.Image = ((System.Drawing.Image)(resources.GetObject("m_picLevelShot.Image")));
            this.m_picLevelShot.Location = new System.Drawing.Point(40, 6);
            this.m_picLevelShot.Name = "m_picLevelShot";
            this.m_picLevelShot.Size = new System.Drawing.Size(700, 700);
            this.m_picLevelShot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.m_picLevelShot.TabIndex = 5;
            this.m_picLevelShot.TabStop = false;
            // 
            // MapLoadControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MapLoadControl";
            this.Size = new System.Drawing.Size(780, 780);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.m_picLevelShot)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ProgressBar m_progress;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label m_lblStatus;
		private System.Windows.Forms.Label m_lblPercentage;
		private System.Windows.Forms.Label m_lblDetail;
        private System.Windows.Forms.PictureBox m_picLevelShot;
    }
}
