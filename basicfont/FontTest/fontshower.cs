using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;
using gl_font;

namespace FontTest
{
	public partial class m_form : Form
	{
		BasicFont m_font;

		public m_form()
		{
			InitializeComponent();

			m_gl.InitializeContexts();

			m_font = new BasicFont();

			Glut.glutInit();

			Display();
		}

		void Display()
		{
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

			string sOpenGL = "Printing characters in OpenGL by setting the raster position and drawing bitmaps.\n";
			sOpenGL += "A bitmap is a set of bytes forming a letter.\n";
			sOpenGL += "They are ordered in a byte array commensurate with the ASCII character set.\n";
			sOpenGL += "The supported characters in this font are from 32(space) to 126(~).";

			Gl.glColor3ub(191, 95, 255);
			m_font.PrintTopLeft("Print to upper left using\nPrintTopLeft", m_gl.Width, m_gl.Height, 0);

			Gl.glColor3ub(245, 245, 245);
			m_font.PrintTopRight("Print to upper right\nusing PrintTopRight", m_gl.Width, m_gl.Height, 0);

			Gl.glColor3ub(255, 215, 0);
			m_font.PrintTopRight("Some Characters : ~!@#$%^&*()_+", m_gl.Width, m_gl.Height, 3);

			Gl.glColor3ub(255, 51, 0);
			m_font.PrintTopCenter("PrintTopCenter\nOne\nTwo\nThree", m_gl.Width, m_gl.Height, 0);

			Gl.glColor3ub(188, 237, 145);
			m_font.PrintLowerCenter("Print to lower middle\nusing PrintLowerCenter", m_gl.Width, 0);

			Gl.glColor3ub(51, 255, 51);
			m_font.PrintLowerRight("Print to lower right\nusing PrintLowerRight", m_gl.Width, 0);

			Gl.glColor3ub(219, 254, 248);
			m_font.PrintLowerLeft("Print to lower left\nusing PrintLowerLeft", m_gl.Width, 0);

			Gl.glColor3ub(255, 255, 255);
			m_font.PrintGLUTCenter("Printing in GLUT\n!@#$%^&*()_+\nIsn't this fun?\n123456789", m_gl.Width, m_gl.Height, 0);

			Gl.glFlush();
		}

		private void m_gl_Paint(object sender, PaintEventArgs e)
		{
			Display();
		}

		private void m_gl_Resize(object sender, EventArgs e)
		{
			Gl.glViewport(0, 0, m_gl.Width, m_gl.Height);
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glLoadIdentity();
			Glu.gluPerspective(30, (double)(m_gl.Width / m_gl.Height), 1.0, 100.0);
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glLoadIdentity();
			Glu.gluLookAt(0, 0, 10, 0, 0, 0, 0, 1, 0);
		}

		private void m_gl_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Q)
			{
				m_font.Delete();

				Close();
			}
		}
	}
}
