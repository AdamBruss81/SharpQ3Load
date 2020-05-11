namespace ShaderTest
{
	partial class ShaderTestForm
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
            this.m_gl = new OpenGLControlModded.simpleOpenGlControlEx();
            this.m_timerRedrawer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // m_gl
            // 
            this.m_gl.BackColor = System.Drawing.Color.Black;
            this.m_gl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_gl.Location = new System.Drawing.Point(0, 0);
            this.m_gl.Name = "m_gl";
            this.m_gl.Size = new System.Drawing.Size(1092, 1061);
            this.m_gl.TabIndex = 0;
            this.m_gl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_glControl_KeyDown);
            // 
            // m_timerRedrawer
            // 
            this.m_timerRedrawer.Interval = 10;
            this.m_timerRedrawer.Tick += new System.EventHandler(this.m_timerRedrawer_Tick);
            // 
            // ShaderTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1092, 1061);
            this.Controls.Add(this.m_gl);
            this.Name = "ShaderTestForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Shader";
            this.ResumeLayout(false);

		}

		#endregion

		private OpenGLControlModded.simpleOpenGlControlEx m_gl;
		private System.Windows.Forms.Timer m_timerRedrawer;
	}
}

