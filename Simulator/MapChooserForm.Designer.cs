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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapChooserForm));
            this.m_lstdm = new System.Windows.Forms.ListView();
            this.m_lstctf = new System.Windows.Forms.ListView();
            this.m_lsttrny = new System.Windows.Forms.ListView();
            this.m_lsttest = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.m_tsbtnOpenMap = new System.Windows.Forms.ToolStripSplitButton();
            this.loadCustomMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadCustomMapNOCDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.m_tsbtnExit = new System.Windows.Forms.ToolStripButton();
            this.m_tsbtnClose = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_lstdm
            // 
            this.m_lstdm.HideSelection = false;
            this.m_lstdm.Location = new System.Drawing.Point(12, 47);
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
            this.m_lstctf.Location = new System.Drawing.Point(176, 47);
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
            this.m_lsttrny.Location = new System.Drawing.Point(339, 47);
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
            this.m_lsttest.Location = new System.Drawing.Point(500, 47);
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
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Q3 DeathMatch";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(173, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Q3 Capture the Flag";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(336, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Q3 Tournament";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(497, 30);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Test";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_tsbtnOpenMap,
            this.m_tsbtnExit,
            this.m_tsbtnClose});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(658, 25);
            this.toolStrip1.TabIndex = 7;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // m_tsbtnOpenMap
            // 
            this.m_tsbtnOpenMap.AutoToolTip = false;
            this.m_tsbtnOpenMap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.m_tsbtnOpenMap.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadCustomMapToolStripMenuItem,
            this.loadCustomMapNOCDToolStripMenuItem});
            this.m_tsbtnOpenMap.Image = global::simulator.Properties.Resources.open_map;
            this.m_tsbtnOpenMap.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.m_tsbtnOpenMap.Name = "m_tsbtnOpenMap";
            this.m_tsbtnOpenMap.Size = new System.Drawing.Size(32, 22);
            this.m_tsbtnOpenMap.Text = "Load Custom";
            this.m_tsbtnOpenMap.ButtonClick += new System.EventHandler(this.m_btnFromFile_Click);
            // 
            // loadCustomMapToolStripMenuItem
            // 
            this.loadCustomMapToolStripMenuItem.Name = "loadCustomMapToolStripMenuItem";
            this.loadCustomMapToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.loadCustomMapToolStripMenuItem.Text = "Load Custom Map";
            this.loadCustomMapToolStripMenuItem.Click += new System.EventHandler(this.m_btnFromFile_Click);
            // 
            // loadCustomMapNOCDToolStripMenuItem
            // 
            this.loadCustomMapNOCDToolStripMenuItem.Name = "loadCustomMapNOCDToolStripMenuItem";
            this.loadCustomMapNOCDToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.loadCustomMapNOCDToolStripMenuItem.Text = "Load Custom Map(NO CD)";
            this.loadCustomMapNOCDToolStripMenuItem.ToolTipText = "No collision detection. This means load the map and don\'t build the BSP bounding " +
    "boxes. The BSP building sometimes takes very long and may even never finish on s" +
    "ome maps. ";
            this.loadCustomMapNOCDToolStripMenuItem.Click += new System.EventHandler(this.loadCustomMapNOCDToolStripMenuItem_Click);
            // 
            // m_tsbtnExit
            // 
            this.m_tsbtnExit.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.m_tsbtnExit.BackColor = System.Drawing.Color.OrangeRed;
            this.m_tsbtnExit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.m_tsbtnExit.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbtnExit.Image")));
            this.m_tsbtnExit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.m_tsbtnExit.Name = "m_tsbtnExit";
            this.m_tsbtnExit.Size = new System.Drawing.Size(79, 22);
            this.m_tsbtnExit.Text = "Exit Program";
            this.m_tsbtnExit.Click += new System.EventHandler(this.m_tsbtnExit_Click);
            // 
            // m_tsbtnClose
            // 
            this.m_tsbtnClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.m_tsbtnClose.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_tsbtnClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.m_tsbtnClose.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbtnClose.Image")));
            this.m_tsbtnClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.m_tsbtnClose.Name = "m_tsbtnClose";
            this.m_tsbtnClose.Size = new System.Drawing.Size(77, 22);
            this.m_tsbtnClose.Text = "Close Dialog";
            this.m_tsbtnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // MapChooserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(658, 391);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_lsttest);
            this.Controls.Add(this.m_lsttrny);
            this.Controls.Add(this.m_lstctf);
            this.Controls.Add(this.m_lstdm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapChooserForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Choose Map to Load";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MapChooserForm_FormClosing);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ListView m_lstdm;
		private System.Windows.Forms.ListView m_lstctf;
		private System.Windows.Forms.ListView m_lsttrny;
		private System.Windows.Forms.ListView m_lsttest;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSplitButton m_tsbtnOpenMap;
        private System.Windows.Forms.ToolStripMenuItem loadCustomMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadCustomMapNOCDToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton m_tsbtnClose;
        private System.Windows.Forms.ToolStripButton m_tsbtnExit;
    }
}