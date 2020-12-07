//*===================================================================================
//* ----||||Simulator||||----
//*
//* By Adam Bruss and Scott Nykl
//*
//* Scott participated in Fall of 2005. Adam has participated from fall 2005 
//* until the present.
//*
//* Loads in quake 3 m_maps. Three modes of interaction are Player, Ghost and Spectator.
//*===================================================================================

namespace simulator
{
	partial class MapChooserForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param m_DisplayName="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.btnClose = new System.Windows.Forms.Button();
            this.m_lstdm = new System.Windows.Forms.ListView();
            this.m_lstctf = new System.Windows.Forms.ListView();
            this.m_lsttrny = new System.Windows.Forms.ListView();
            this.m_lsttest = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.m_btnFromFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(567, 376);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // m_lstdm
            // 
            this.m_lstdm.HideSelection = false;
            this.m_lstdm.Location = new System.Drawing.Point(12, 33);
            this.m_lstdm.MultiSelect = false;
            this.m_lstdm.Name = "m_lstdm";
            this.m_lstdm.Size = new System.Drawing.Size(142, 329);
            this.m_lstdm.TabIndex = 1;
            this.m_lstdm.UseCompatibleStateImageBehavior = false;
            this.m_lstdm.View = System.Windows.Forms.View.List;
            this.m_lstdm.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_lstdm_KeyDown);
            this.m_lstdm.MouseClick += new System.Windows.Forms.MouseEventHandler(this.m_lstdm_MouseClick);
            // 
            // m_lstctf
            // 
            this.m_lstctf.HideSelection = false;
            this.m_lstctf.Location = new System.Drawing.Point(176, 33);
            this.m_lstctf.MultiSelect = false;
            this.m_lstctf.Name = "m_lstctf";
            this.m_lstctf.Size = new System.Drawing.Size(142, 329);
            this.m_lstctf.TabIndex = 2;
            this.m_lstctf.UseCompatibleStateImageBehavior = false;
            this.m_lstctf.View = System.Windows.Forms.View.List;
            this.m_lstctf.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_lstctf_KeyDown);
            this.m_lstctf.MouseClick += new System.Windows.Forms.MouseEventHandler(this.m_lstctf_MouseClick);
            // 
            // m_lsttrny
            // 
            this.m_lsttrny.HideSelection = false;
            this.m_lsttrny.Location = new System.Drawing.Point(339, 33);
            this.m_lsttrny.MultiSelect = false;
            this.m_lsttrny.Name = "m_lsttrny";
            this.m_lsttrny.Size = new System.Drawing.Size(142, 329);
            this.m_lsttrny.TabIndex = 3;
            this.m_lsttrny.UseCompatibleStateImageBehavior = false;
            this.m_lsttrny.View = System.Windows.Forms.View.List;
            this.m_lsttrny.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_lsttrny_KeyDown);
            this.m_lsttrny.MouseClick += new System.Windows.Forms.MouseEventHandler(this.m_lsttrny_MouseClick);
            // 
            // m_lsttest
            // 
            this.m_lsttest.HideSelection = false;
            this.m_lsttest.Location = new System.Drawing.Point(500, 33);
            this.m_lsttest.MultiSelect = false;
            this.m_lsttest.Name = "m_lsttest";
            this.m_lsttest.Size = new System.Drawing.Size(142, 329);
            this.m_lsttest.TabIndex = 4;
            this.m_lsttest.UseCompatibleStateImageBehavior = false;
            this.m_lsttest.View = System.Windows.Forms.View.List;
            this.m_lsttest.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_lsttest_KeyDown);
            this.m_lsttest.MouseClick += new System.Windows.Forms.MouseEventHandler(this.m_lsttest_MouseClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Q3 DeathMatch";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(173, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Q3 Capture the Flag";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(336, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Q3 Tournament";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(497, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Test";
            // 
            // m_btnFromFile
            // 
            this.m_btnFromFile.Location = new System.Drawing.Point(279, 376);
            this.m_btnFromFile.Name = "m_btnFromFile";
            this.m_btnFromFile.Size = new System.Drawing.Size(100, 23);
            this.m_btnFromFile.TabIndex = 6;
            this.m_btnFromFile.Text = "Load From File";
            this.m_btnFromFile.UseVisualStyleBackColor = true;
            this.m_btnFromFile.Click += new System.EventHandler(this.m_btnFromFile_Click);
            this.m_btnFromFile.Enabled = true;
            // 
            // MapChooserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(658, 411);
            this.Controls.Add(this.m_btnFromFile);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_lsttest);
            this.Controls.Add(this.m_lsttrny);
            this.Controls.Add(this.m_lstctf);
            this.Controls.Add(this.m_lstdm);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapChooserForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Choose Map to Load";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MapChooserForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.ListView m_lstdm;
		private System.Windows.Forms.ListView m_lstctf;
		private System.Windows.Forms.ListView m_lsttrny;
		private System.Windows.Forms.ListView m_lsttest;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button m_btnFromFile;
	}
}