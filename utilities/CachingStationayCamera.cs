using System;
using System.Drawing;
using Tao.OpenGl;
using System.Windows.Forms;
using Tao.Platform.Windows;

namespace utilities
{
	public class CachedStationaryCamera
	{
		D3Vect m_curpoint = new D3Vect();
		D3Vect m_lastpoint = new D3Vect();
		ECamManip m_cammanip = ECamManip.NONE;
		SimpleOpenGlControl m_window = null;
		double[] m_pdTransformationMatrix = new double[16];
		float[] m_rotation = new float[4];
		float m_RotationIncrement = 3.0f;

		public enum ECamManip { NONE, ROTATE };

		public double[] GetTransformation
		{
			get { return m_pdTransformationMatrix; }
		}

		public CachedStationaryCamera(SimpleOpenGlControl window) 
		{
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_pdTransformationMatrix);
			Gl.glPopMatrix();

			m_window = window;
		}

		public void Reset()
		{
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_pdTransformationMatrix);
			Gl.glPopMatrix();
		}

		public void Transform()
		{
			Gl.glMultMatrixd(m_pdTransformationMatrix);
		}

		public void Rotate()
		{
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			Gl.glRotatef(m_rotation[0], m_rotation[1], m_rotation[2], m_rotation[3]);
			Gl.glMultMatrixd(m_pdTransformationMatrix);
			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_pdTransformationMatrix);
			Gl.glPopMatrix();
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

		public bool MouseMove(Point point)
		{
			bool bChanged = false;
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
						Gl.glPushMatrix();
						Gl.glLoadIdentity();
						Gl.glRotated(rot_angle, rotAxis.x, rotAxis.y, rotAxis.z);
						Gl.glMultMatrixd(m_pdTransformationMatrix);
						Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_pdTransformationMatrix);
						Gl.glPopMatrix();
						bChanged = true;
					}
					
					break;
				}
			}

			m_lastpoint = m_curpoint;

			return bChanged;
		}

		public void MouseDown(MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left) {
				m_cammanip = ECamManip.ROTATE;
				m_lastpoint = trackBallMapping(e.Location);
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
