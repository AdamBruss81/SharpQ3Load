using System;
using System.Windows.Forms;
using Tao.FreeGlut;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using utilities;

namespace ShaderTest
{
	public partial class ShaderTestForm : Form
	{
		int m_nShaderProgram = 0;
		float m_fRotation = 1.0f;
		double[] m_modelview = new double[16];
		float m_fTime = 0.0f;
		float m_fTimeChanger = 0.1f;
		double m_dZoomAwayAmount = -7;
		float m_fLightXDir = 0f;
		float m_fLightXPosChanger = 0.08f;

		public ShaderTestForm()
		{
			InitializeComponent();

			Glut.glutInit();

			m_gl.Load += m_gl_Load;
		}

		private void m_gl_Load(object sender, EventArgs e)
		{
			GL.ClearColor(.9f, .9f, 1.0f, 0.0f);

			GL.ShadeModel(ShadingModel.Smooth);

            GL.Enable(EnableCap.DepthTest);

            m_timerRedrawer.Start();
        }

		private void m_timerRedrawer_Tick(object sender, EventArgs e)
		{
            Draw();
        }

		private void SetupProjectionMatrix()
		{
			if (m_gl.Height != 0)
			{
				GL.MatrixMode(MatrixMode.Projection);
				GL.LoadIdentity();

				Matrix4 perspec = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI / 4.0), m_gl.Width / m_gl.Height, .5f, 500);
				GL.LoadMatrix(ref perspec);
			}
		}

		private void Draw()
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			for (int i = 1; i <= 9; i++)
			{
				// setup viewing volumes and viewports
				if (i == 1)
				{
					// setup frustrum and viewport top left
					GL.Viewport(0, m_gl.Height * 2/3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();					
				}
				if ( i == 2)
				{
					// setup frustrum and viewport top center
					GL.Viewport(m_gl.Width / 3, m_gl.Height * 2/3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 3)
				{
					// setup frustrum and viewport top right
					GL.Viewport(m_gl.Width * 2/3, m_gl.Height * 2/3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 4)
				{
					// setup frustrum and viewport middle left
					GL.Viewport(0, m_gl.Height / 3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 5)
				{
					// setup frustrum and viewport middle
					GL.Viewport(m_gl.Width / 3, m_gl.Height / 3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 6)
				{
					// setup frustrum and viewport middle right
					GL.Viewport(m_gl.Width * 2/3, m_gl.Height / 3, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 7)
				{
					// setup frustrum and viewport left bottom
					GL.Viewport(0, 0, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 8)
				{
					// setup frustrum and viewport middle bottom
					GL.Viewport(m_gl.Width / 3, 0, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}
				if (i == 9)
				{
					// setup frustrum and viewport right bottom
					GL.Viewport(m_gl.Width * 2/3, 0, m_gl.Width / 3, m_gl.Height / 3);
					SetupProjectionMatrix();
				}

				GL.MatrixMode(MatrixMode.Modelview);
				GL.LoadIdentity();

				GL.Clear(ClearBufferMask.DepthBufferBit);

				// draw to viewports
				if(i == 1)
				{
					// setup lighting
					GL.Enable(EnableCap.Lighting);
					float[] ambient = { 0.5f, 0.5f, 0.5f, 1.0f };
					GL.LightModel(LightModelParameter.LightModelAmbient, ambient);
					GL.LightModel(LightModelParameter.LightModelLocalViewer, 1);

					GL.PushMatrix();
					GL.LoadIdentity();
					float[] lightpos = { 20, 20, 0 };
					GL.Light(LightName.Light0, LightParameter.Position, lightpos);
					GL.Enable(EnableCap.Light0);
					GL.PopMatrix();

					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					GL.Disable(EnableCap.Lighting); 
				}
				if(i == 2)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("sine.vert", "fragcolor.frag");
					GL.UseProgram(m_nShaderProgram);

					// get location of time variable in shader
					int nTimeLoc = GL.GetUniformLocation(m_nShaderProgram, "time");

					// set time variable in the shader to a local variable m_fTime
					GL.Uniform1(nTimeLoc, m_fTime);
					
					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					GL.Rotate(30.0f, 0.0f, 1.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					m_fTime += m_fTimeChanger;

					if (m_fTime >= 10 || m_fTime <= -10) m_fTimeChanger *= -1; // bounce back between 10 and -10

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 3)
				{
					

					m_nShaderProgram = ShaderHelper.CreateProgram("glcolor.vert", "glcolor.frag");
					GL.UseProgram(m_nShaderProgram);

					GL.Color3(System.Drawing.Color.Aquamarine);

					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 4)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("flat.vert", "fragcolor.frag");
					GL.UseProgram(m_nShaderProgram);

					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					GL.Rotate(m_fRotation, 0.0f, 1.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);					
					GL.PopMatrix();

					m_fRotation += 3.5f;
					if (m_fRotation >= 360) m_fRotation = 0.0f;					

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 5)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("toonver1.vert", "toonver1.frag");
					GL.UseProgram(m_nShaderProgram);

					// nLightDirLoc is a reference to a shader variable
					int nLightDirLoc = GL.GetUniformLocation(m_nShaderProgram, "lightdir");

					float[] fLightDir = { 1.0f, 0.0f, 0.0f };
					GL.Uniform3(nLightDirLoc, 1, fLightDir);

					// this uniform works
					//int nTest = GL.GetUniformLocation(m_nShaderProgram, "test");
					//GL.Uniform1(nTest, 5.0f);

					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					//GL.Rotate(90.0f, 0.0f, 1.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 6)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("toonver2.vert", "toonver2.frag");
					GL.UseProgram(m_nShaderProgram);

					int nLightDirLoc = GL.GetUniformLocation(m_nShaderProgram, "lightDir");
					GL.Uniform3(nLightDirLoc, m_fLightXDir, -1f, 0f);

					GL.PushMatrix();
					GL.Translate(0.0, 0.0, m_dZoomAwayAmount);
					GL.Rotate(35.0f, 1.0f, 1.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					m_fLightXDir += m_fLightXPosChanger;
					if (m_fLightXDir > 3f || m_fLightXDir < -3f) 
						m_fLightXPosChanger *= -1f;

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 7)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("toonver3.vert", "toonver3.frag");
					GL.UseProgram(m_nShaderProgram);

					float[] lightdir = { -1, 0, 1 };
					GL.Light(LightName.Light0, LightParameter.Position, lightdir);

					Matrix4 lookat = Matrix4.LookAt(0, 0, (float)m_dZoomAwayAmount, 0, 0, 0, 0, 1, 0);
					GL.LoadMatrix(ref lookat);

					Glut.glutSolidTeapot(1.5);

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 8)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("dirlight1.vert", "glcolor.frag");
					GL.UseProgram(m_nShaderProgram);

					float[] lightdir = { 0, 0, 1 };
					float[] lightSpec = { 1.0f, 1.0f, 1.0f, 1.0f };
					float[] lightAm = { 0.0f, 0.0f, 0.0f, 1.0f };

					GL.Light(LightName.Light0, LightParameter.Position, lightdir);
					GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 100f);
					GL.Material(MaterialFace.Front, MaterialParameter.Specular, lightSpec);
					GL.Material(MaterialFace.Front, MaterialParameter.Ambient, lightAm);

                    Matrix4 lookat = Matrix4.LookAt(0, 0, (float)m_dZoomAwayAmount, 0, 0, 0, 0, 1, 0);
                    GL.LoadMatrix(ref lookat);

                    GL.PushMatrix();
					GL.Rotate(m_fRotation, -1.0f, 0.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
				if (i == 9)
				{
					m_nShaderProgram = ShaderHelper.CreateProgram("dirlightpixel.vert", "dirlightpixel.frag");
					GL.UseProgram(m_nShaderProgram);

                    float[] lightdir = { 0, 0, 1 };
                    float[] lightSpec = { 1.0f, 1.0f, 1.0f, 1.0f };
                    float[] lightAm = { 0.0f, 0.0f, 0.0f, 1.0f };

                    GL.Light(LightName.Light0, LightParameter.Position, lightdir);
                    GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 100f);
                    GL.Material(MaterialFace.Front, MaterialParameter.Specular, lightSpec);
                    GL.Material(MaterialFace.Front, MaterialParameter.Ambient, lightAm);

					Matrix4 lookat = Matrix4.LookAt(0, 0, (float)m_dZoomAwayAmount, 0, 0, 0, 0, 1, 0);
                    GL.LoadMatrix(ref lookat);

                    GL.PushMatrix();
					GL.Rotate(m_fRotation, -1.0f, 0.0f, 0.0f);
					Glut.glutSolidTeapot(1.5);
					GL.PopMatrix();

					ShaderHelper.CloseProgram(m_nShaderProgram);
				}
			}

			m_gl.SwapBuffers();
		}

		private void m_glControl_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Q:
					Close();
					break;
			}
		}
	}
}
