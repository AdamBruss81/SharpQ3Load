﻿//*===================================================================================
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
	partial class SimulatorForm
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
            this.components = new System.ComponentModel.Container();
            this.m_openGLControl = new OpenGLControlModded.simpleOpenGlControlEx();
            this.timerRedrawer = new System.Windows.Forms.Timer(this.components);
            this.timerMoveForward = new System.Windows.Forms.Timer(this.components);
            this.timerMoveBackward = new System.Windows.Forms.Timer(this.components);
            this.timerStrafeLeft = new System.Windows.Forms.Timer(this.components);
            this.timerStrafeRight = new System.Windows.Forms.Timer(this.components);
            this.timerLeftMouse = new System.Windows.Forms.Timer(this.components);
            this.timerMoveUp = new System.Windows.Forms.Timer(this.components);
            this.timerMoveDown = new System.Windows.Forms.Timer(this.components);
            this.m_tlContainer = new System.Windows.Forms.TableLayoutPanel();
            this.m_controlMapProgress = new simulator.MapLoadControl();
            this.timerFallChecker = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // m_openGLControl
            // 
            this.m_openGLControl.AccumBits = ((byte)(0));
            this.m_openGLControl.AutoCheckErrors = false;
            this.m_openGLControl.AutoFinish = false;
            this.m_openGLControl.AutoMakeCurrent = false;
            this.m_openGLControl.AutoSwapBuffers = true;
            this.m_openGLControl.BackColor = System.Drawing.Color.Black;
            this.m_openGLControl.ColorBits = ((byte)(32));
            this.m_openGLControl.DepthBits = ((byte)(16));
            this.m_openGLControl.Location = new System.Drawing.Point(128, 80);
            this.m_openGLControl.Name = "m_openGLControl";
            this.m_openGLControl.Size = new System.Drawing.Size(313, 218);
            this.m_openGLControl.StencilBits = ((byte)(0));
            this.m_openGLControl.TabIndex = 0;
            this.m_openGLControl.GotFocus += new System.EventHandler(this.m_openGLControl_GotFocus);
            this.m_openGLControl.KeyUp += new System.Windows.Forms.KeyEventHandler(this.openGLControl_KeyUp);
            this.m_openGLControl.LostFocus += new System.EventHandler(this.m_openGLControl_LostFocus);
            this.m_openGLControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.openGLControl_MouseDown);
            this.m_openGLControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.openGLControl_MouseMove);
            this.m_openGLControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.m_openGLControl_MouseUp);
            // 
            // timerRedrawer
            // 
            this.timerRedrawer.Interval = 8;
            this.timerRedrawer.Tick += new System.EventHandler(this.timerRedrawer_Tick);
            // 
            // timerMoveForward
            // 
            this.timerMoveForward.Interval = 10;
            this.timerMoveForward.Tick += new System.EventHandler(this.timerMoveForward_Tick);
            // 
            // timerMoveBackward
            // 
            this.timerMoveBackward.Interval = 10;
            this.timerMoveBackward.Tick += new System.EventHandler(this.timerMoveBackward_Tick);
            // 
            // timerStrafeLeft
            // 
            this.timerStrafeLeft.Interval = 10;
            this.timerStrafeLeft.Tick += new System.EventHandler(this.timerStrafeLeft_Tick);
            // 
            // timerStrafeRight
            // 
            this.timerStrafeRight.Interval = 10;
            this.timerStrafeRight.Tick += new System.EventHandler(this.timerStrafeRight_Tick);
            // 
            // timerLeftMouse
            // 
            this.timerLeftMouse.Interval = 250;
            this.timerLeftMouse.Tick += new System.EventHandler(this.timerLeftMouse_Tick);
            // 
            // timerMoveUp
            // 
            this.timerMoveUp.Interval = 10;
            this.timerMoveUp.Tick += new System.EventHandler(this.timerMoveUp_Tick);
            // 
            // timerMoveDown
            // 
            this.timerMoveDown.Interval = 10;
            this.timerMoveDown.Tick += new System.EventHandler(this.timerMoveDown_Tick);
            // 
            // m_tlContainer
            // 
            this.m_tlContainer.BackColor = System.Drawing.Color.Black;
            this.m_tlContainer.ColumnCount = 3;
            this.m_tlContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.m_tlContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.m_tlContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.m_tlContainer.Location = new System.Drawing.Point(368, 356);
            this.m_tlContainer.Name = "m_tlContainer";
            this.m_tlContainer.RowCount = 3;
            this.m_tlContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.m_tlContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.m_tlContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.m_tlContainer.Size = new System.Drawing.Size(331, 231);
            this.m_tlContainer.TabIndex = 2;
            // 
            // m_controlMapProgress
            // 
            this.m_controlMapProgress.BackColor = System.Drawing.Color.MediumAquamarine;
            this.m_controlMapProgress.Loading = false;
            this.m_controlMapProgress.Location = new System.Drawing.Point(612, 193);
            this.m_controlMapProgress.Name = "m_controlMapProgress";
            this.m_controlMapProgress.Size = new System.Drawing.Size(336, 66);
            this.m_controlMapProgress.Status = "Loading...";
            this.m_controlMapProgress.TabIndex = 1;
            this.m_controlMapProgress.Visible = false;
            // 
            // timerFallChecker
            // 
            this.timerFallChecker.Tick += new System.EventHandler(this.timerFallChecker_Tick);
            // 
            // SimulatorForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(1600, 1000);
            this.Controls.Add(this.m_controlMapProgress);
            this.Controls.Add(this.m_tlContainer);
            this.Controls.Add(this.m_openGLControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(100, 100);
            this.Name = "SimulatorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Simulator";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Simulator_Load);
            this.ResumeLayout(false);

		}
		#endregion

		private System.Windows.Forms.Timer timerRedrawer;
		private System.Windows.Forms.Timer timerMoveForward;
		private System.Windows.Forms.Timer timerMoveBackward;
		private System.Windows.Forms.Timer timerStrafeLeft;
		private System.Windows.Forms.Timer timerStrafeRight;
		private System.Windows.Forms.Timer timerLeftMouse;
		private System.Windows.Forms.Timer timerMoveUp;
		private System.Windows.Forms.Timer timerMoveDown;
		private MapLoadControl m_controlMapProgress;
		private System.Windows.Forms.TableLayoutPanel m_tlContainer;
		private System.Windows.Forms.Timer timerFallChecker;
	}
}