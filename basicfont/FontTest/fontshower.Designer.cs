namespace FontTest
{
	partial class m_form
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
			this.m_gl = new Tao.Platform.Windows.SimpleOpenGlControl();
			this.SuspendLayout();
			// 
			// m_gl
			// 
			this.m_gl.AccumBits = ((byte)(0));
			this.m_gl.AutoCheckErrors = false;
			this.m_gl.AutoFinish = false;
			this.m_gl.AutoMakeCurrent = true;
			this.m_gl.AutoSwapBuffers = true;
			this.m_gl.BackColor = System.Drawing.Color.Black;
			this.m_gl.ColorBits = ((byte)(32));
			this.m_gl.DepthBits = ((byte)(16));
			this.m_gl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_gl.Location = new System.Drawing.Point(0, 0);
			this.m_gl.Name = "m_gl";
			this.m_gl.Size = new System.Drawing.Size(808, 497);
			this.m_gl.StencilBits = ((byte)(0));
			this.m_gl.TabIndex = 0;
			this.m_gl.Paint += new System.Windows.Forms.PaintEventHandler(this.m_gl_Paint);
			this.m_gl.Resize += new System.EventHandler(this.m_gl_Resize);
			this.m_gl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_gl_KeyDown);
			// 
			// m_form
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(808, 497);
			this.Controls.Add(this.m_gl);
			this.Name = "m_form";
			this.Text = "Font";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.ResumeLayout(false);
		}

		#endregion

		private Tao.Platform.Windows.SimpleOpenGlControl m_gl;
	}
}

