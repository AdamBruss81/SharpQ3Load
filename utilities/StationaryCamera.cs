using System;
using System.Drawing;
using Tao.OpenGl;
using System.Windows.Forms;
using Tao.Platform.Windows;

namespace utilities
{
	public class StationaryCamera
	{
		D3Vect m_curpoint = new D3Vect();
		D3Vect m_lastpoint = new D3Vect();
		ECamManip m_cammanip = ECamManip.NONE;
		SimpleOpenGlControl m_window = null;
		double[] m_modelview = new double[16];
		float[] m_rotation = new float[4];
		float[] m_translation = new float[3];
		double m_dZoomFactor = 1.0;
		double m_dfov = 20;
		float m_RotationIncrement = 3.0f;

		public enum ECamManip { NONE, ROTATE, ZOOM };

		public StationaryCamera(SimpleOpenGlControl window) 
		{
			m_window = window;
		}

		public double ZoomFactor 
		{ 
			get { return m_dZoomFactor; } 
			set { m_dZoomFactor = value; }
		}

		public float RotationIncrement
		{
			get { return m_RotationIncrement; }
			set { m_RotationIncrement = value; }
		}

		public double GetFOV { get { return m_dfov; } }

		public float[] GetRotation
		{
			get { return m_rotation; }
		}

		public float[] GetTranslation
		{
			get { return m_translation; }
		}

		public void GLResize()
		{
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glLoadIdentity();
			m_dfov = 20 * m_dZoomFactor;
			if (m_dfov > 180) m_dfov = 180;
			else if (m_dfov < 0) m_dfov = 0;
			Glu.gluPerspective(m_dfov, (double)(m_window.Width / m_window.Height), 1.0, 100.0);
			Gl.glViewport(0, 0, m_window.Width, m_window.Height);
			Gl.glTranslatef(0.0f, 0.0f, -20.0f);
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
		}

		public void Rotate()
		{
			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_modelview);
			Gl.glLoadIdentity();
			Gl.glRotatef(m_rotation[0], m_rotation[1], m_rotation[2], m_rotation[3]);
			Gl.glMultMatrixd(m_modelview);
		}

		public void Translate()
		{
			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_modelview);
			Gl.glLoadIdentity();
			Gl.glTranslatef(m_translation[0], m_translation[1], m_translation[2]);
			Gl.glMultMatrixd(m_modelview);
		}

		// http://www.cse.ohio-state.edu/~crawfis/Graphics/VirtualTrackball.html
		private D3Vect trackBallMapping(Point point)
		{
			D3Vect v = new D3Vect();
			double d;

			v.x = (2.0 * point.X - m_window.Width) / m_window.Width;
			v.y = (m_window.Height - 2.0 * point.Y) / m_window.Height;
			v.z = 0.0;
			d = v.Length;
			d = (d < 1.0) ? d : 1.0;  // If d is > 1, then clamp it at one.
			v.z = Math.Sqrt(1.001 - d * d);  // project the line segment up to the surface of the sphere.

			v.normalize();  // We forced d to be less than one, not v, so need to normalize somewhere.

			return v;
		}

		public void MouseWheel(MouseEventArgs e)
		{
			int sysDelta = SystemInformation.MouseWheelScrollDelta;

			m_dZoomFactor -= (e.Delta / sysDelta) * .1;

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_modelview);

			GLResize();

			Gl.glLoadIdentity();
			Gl.glMultMatrixd(m_modelview);
		}

		public void MouseMove(Point point)
		{
			switch(m_cammanip)
			{
				case ECamManip.ROTATE:
				{
					// http://www.cse.ohio-state.edu/~crawfis/Graphics/VirtualTrackball.html
					m_curpoint = trackBallMapping(point);
					D3Vect direction = m_curpoint - m_lastpoint;
					double velocity = direction.Length;
					if (velocity > 0.001)
					{
						//
						// Rotate about the axis that is perpendicular to the great circle connecting the mouse movements.
						//
						D3Vect rotAxis = new D3Vect(m_lastpoint, m_curpoint);
						double rot_angle = velocity * 90.0;
						//
						// We need to apply the rotation as the last transformation.
						//   1. Get the current matrix and save it.
						//   2. Set the matrix to the identity matrix (clear it).
						//   3. Apply the trackball rotation.
						//   4. Pre-multiply it by the saved matrix.
						//
						Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_modelview);
						Gl.glLoadIdentity();
						Gl.glRotated(rot_angle, rotAxis.x, rotAxis.y, rotAxis.z);
						Gl.glMultMatrixd(m_modelview);
					}
					break;
				}
				case ECamManip.ZOOM:
				{
					double pixel_diff = m_lastpoint.x - point.X;

					m_dZoomFactor -= pixel_diff * .005;

					Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_modelview);

					GLResize();

					Gl.glLoadIdentity();
					Gl.glMultMatrixd(m_modelview);

					//
					// Set the current point, so the lastPoint will be saved properly below.
					//
					m_curpoint.x = (double)point.X; 
					m_curpoint.y = (double)point.Y;
					m_curpoint.z = 0.0;
					break;
				}
			}

			m_lastpoint = m_curpoint;
		}

		public void MouseDown(MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left) {
				m_cammanip = ECamManip.ROTATE;
				m_lastpoint = trackBallMapping(e.Location);
			}
			else if(e.Button == MouseButtons.Right) {
				m_cammanip = ECamManip.ZOOM;
				m_lastpoint.x = e.X;
				m_lastpoint.y = e.Y;
			}
		}

		public void MouseUp(MouseEventArgs e)
		{
			m_cammanip = ECamManip.NONE;
		}

		public void UpArrow()
		{
			m_rotation[0] = -m_RotationIncrement; m_rotation[1] = 1.0f; m_rotation[2] = 0.0f; m_rotation[3] = 0.0f;
			Rotate();
		}

		public void DownArrow()
		{
			m_rotation[0] = m_RotationIncrement; m_rotation[1] = 1.0f; m_rotation[2] = 0.0f; m_rotation[3] = 0.0f;
			Rotate();
		}

		public void LeftArrow()
		{
			m_rotation[0] = -m_RotationIncrement; m_rotation[1] = 0.0f; m_rotation[2] = 1.0f; m_rotation[3] = 0.0f;
			Rotate();
		}

		public void RightArrow()
		{
			m_rotation[0] = m_RotationIncrement; m_rotation[1] = 0.0f; m_rotation[2] = 1.0f; m_rotation[3] = 0.0f;
			Rotate();
		}
	}
}
