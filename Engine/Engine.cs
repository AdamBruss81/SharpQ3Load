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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using gl_font;
using Tao.OpenGl;
using utilities;
using Tao.Platform.Windows;
using obsvr;
using System.IO;

namespace engine
{
	/// <summary>
	/// The basic game driver for the Simulator
	/// </summary>
	public abstract class Engine : Subject
	{
		public enum ESignals { DONE_READING_MAP = SignalStarts.g_nEngineStart };

		bool m_bDrawAxis = false;

		int m_nDrawAxisList;

		public enum EEngineType { GHOST, PLAYER, SPECTATOR };
		public enum EGraphicsMode { SINGLE_TEXTURE_VERTICE_COLOR, MULTI_TEXTURE_WHITE, WIREFRAME, SINGLE_WHITE };

		protected FigureList m_dynamicFigList = null;
		protected FigureList m_lStaticFigList = null;
		protected BasicFont m_fonter = null;
		protected MovableCamera m_cam = null;
		protected OpenGLControlModded.simpleOpenGlControlEx m_GControl = null;
		protected D3Vect m_d3LastMovableCameraLookAt = new D3Vect();
		protected double[] m_pdModelview = new double[16];
		List<Plane> m_lFrustrumPlanes = new List<Plane>(6);

		protected const float JUMP_OFFSET = 1.0f;

		private EGraphicsMode m_GraphicsMode = EGraphicsMode.SINGLE_WHITE;

		public Engine(OpenGLControlModded.simpleOpenGlControlEx window)
		{
			m_dynamicFigList = new FigureList();
			m_lStaticFigList = new FigureList();
			m_fonter = new BasicFont();
			m_GControl = window;
			IGLControl blah = m_GControl as IGLControl;
			m_cam = new MovableCamera(0.0, 0.0, 0.0, Math.PI / 2, Math.PI, m_GControl as IGLControl);

			GenerateDrawAxesDisplayList();
		}

		/// <summary>
		/// Copy driver to this
		/// </summary>
		/// <param m_DisplayName="driver">driver to copy</param>
		public Engine(Engine driver)
		{
			m_dynamicFigList = new FigureList();
			m_lStaticFigList = driver.GetStaticFigList;
			m_fonter = driver.Fonter;
			m_cam = driver.Cam;
			m_GControl = driver.GetWindow;
			m_GraphicsMode = driver.GraphicsMode;
			m_bDrawAxis = driver.DrawAxes;
			m_nDrawAxisList = driver.GetDrawAxesList;
		}

		abstract public EEngineType GetClass();

		abstract public string GetGameMode();

		virtual public void Fall(ref SoundManager.EEffects eEffectToPlay) { }

		public BasicFont Fonter
		{
			get { return m_fonter; }
		}

		public virtual void PreSwitchModes() {}

		public OpenGLControlModded.simpleOpenGlControlEx GetWindow
		{
			get { return m_GControl; }
		}

		public bool DrawAxes
		{
			get { return m_bDrawAxis; }
		}

		public int GetDrawAxesList
		{
			get { return m_nDrawAxisList; }
		}
	
		public FigureList GetStaticFigList
		{
			get { return m_lStaticFigList; }
		}

		public MovableCamera Cam
		{
			get { return m_cam; }
			set { m_cam = value; }
		}

		public EGraphicsMode GraphicsMode
		{
			get { return m_GraphicsMode; }
			set { m_GraphicsMode = value; }
		}

		public void TurnOffDebugging()
		{
			m_lStaticFigList[0].TurnOffDebugging();
		}

		public string GetGraphicsMode
		{
			get 
			{
				switch (m_GraphicsMode)
				{
					case EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR:
						return "SINGLE TEXTURE VERT COLOR";
					case EGraphicsMode.MULTI_TEXTURE_WHITE:
						return "MULTI-TEXTURE WHITE";
					case EGraphicsMode.WIREFRAME:
						return "WIREFRAME";
					case EGraphicsMode.SINGLE_WHITE:
						return "SINGLE TEXTURE WHITE";
				}
				throw new Exception("Invalid graphics mode " + m_GraphicsMode.ToString());
			}
		}

		public int NumStaticFigs
		{
			get { return m_lStaticFigList.Count(); }
		}

		/// <summary>
		/// Reads in a map bundled in a MapInfo object
		/// </summary>
		/// <param name="map">Map Wrapper to read in</param>
		public virtual void LoadMap(MapInfo map)
		{
			Figure mapFigure = new Figure();
			mapFigure.CheckBoundingBoxes(map);
			m_lStaticFigList.Add(mapFigure);
			mapFigure.Read(map);
		}

		public void Initialize()
		{
			if (m_lStaticFigList.Count() > 0) m_lStaticFigList[0].Initialize();
		}

		virtual public void InitializeLists()
		{
			if(m_lStaticFigList.Count() > 0) m_lStaticFigList[0].InitializeLists();
		}

		protected void SetMovableCameraToRandomViewPoint()
		{
			if (m_lStaticFigList[0].GetNumViewPoints > 0)
			{
				Viewpoint vp = m_lStaticFigList[0].GetRandomViewPoint(true);
				m_cam.Position = vp.Position;
				m_cam.PHI_RAD = Math.PI / 2;
				m_cam.THETA_RAD = vp.Orientation[3];
			}
		}

		/// <summary>
		/// Sets glulookat based on MovableCamera lookat point. Shows all figures. Draws text. Refreshes window
		/// by swapping buffers.
		/// </summary>
		/// <param m_DisplayName="window">the open gl window</param>
		virtual public void showScene(Keys keys)
		{
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			Gl.glLoadIdentity();

			m_cam.GetLookAtRef(m_d3LastMovableCameraLookAt);
			Glu.gluLookAt(m_cam.Position[0], m_cam.Position[1], m_cam.Position[2],
				m_d3LastMovableCameraLookAt[0], m_d3LastMovableCameraLookAt[1], m_d3LastMovableCameraLookAt[2], 0, 0, 1);

			int nNumFacesRendered = 0, nDynamicFaces = 0;

			m_lStaticFigList.ShowAllFigures(m_GraphicsMode, ref nNumFacesRendered, m_lFrustrumPlanes, m_cam);
			m_dynamicFigList.ShowAllFigures(m_GraphicsMode, ref nDynamicFaces, m_lFrustrumPlanes, m_cam); 

			Draw(nNumFacesRendered);
       
			m_GControl.Draw();
		}

		public virtual void Delete()
		{
			m_lStaticFigList.DeleteAll();
			m_dynamicFigList.DeleteAll();
			Gl.glDeleteLists(m_nDrawAxisList, 1);
			if (m_fonter != null) m_fonter.Delete();
		}

		virtual public bool MoveForward() { return false; }
		virtual public void MoveBackward() { }
		virtual public void MoveLeft() { }
		virtual public void MoveRight() { }
		virtual public void MoveUp() { }
		virtual public void MoveDown() { }
		virtual public void GameTick(MoveStates stoppedMovingStates, MoveStates startedMovingStates) { }
		virtual public void CacheMove(MovableCamera.DIRECTION direction) { }
		virtual public double GetVelocity() { return 0.0; }

		virtual protected void Draw(int nFaceCount)
		{
			sgl.PUSHATT(Gl.GL_CURRENT_BIT | Gl.GL_TEXTURE_BIT);

			if(STATE.AllowPrinting) 
			{
				Gl.glDisable(Gl.GL_TEXTURE_2D);
				Gl.glColor3d(1.0, 1.0, 0.0);
				m_fonter.PrintTopCenter(m_lStaticFigList[0].GetDisplayName, m_GControl.Width, m_GControl.Height, 0);
				Gl.glColor3d(0.0, 0.93, 0.46);
				string sModes = "Mode " + GetGameMode() + "\n" + "Style " + GetGraphicsMode;
				m_fonter.PrintTopRight(sModes, m_GControl.Width, m_GControl.Height, 0);
				Gl.glColor3ub(255, 255, 255);
				m_fonter.PrintLowerLeft("Faces Rendered: " + Convert.ToString(nFaceCount), m_GControl.Width, 0);
			}

			if (m_lFrustrumPlanes.Count == 0) {
				for (int i = 0; i < 6; i++) m_lFrustrumPlanes.Add(new Plane());
			}

			Plane.ExtractFrustrum(m_lFrustrumPlanes);

			if (STATE.DebuggingMode && STATE.AllowPrinting)
			{
				string sFrustrumOut = "";
				for (int i = 0; i < m_lFrustrumPlanes.Count; i++)
				{
					switch (i)
					{
						case 0:
							sFrustrumOut = sFrustrumOut + "Right Plane: " + m_lFrustrumPlanes[i].ToString() + "\n";
							break;
						case 1:
							sFrustrumOut = sFrustrumOut + "Left Plane: " + m_lFrustrumPlanes[i].ToString() + "\n";
							break;
						case 2:
							sFrustrumOut = sFrustrumOut + "Bottom Plane: " + m_lFrustrumPlanes[i].ToString() + "\n";
							break;
						case 3:
							sFrustrumOut = sFrustrumOut + "Top Plane: " + m_lFrustrumPlanes[i].ToString() + "\n";
							break;
						case 4:
							sFrustrumOut = sFrustrumOut + "Far Plane: " + m_lFrustrumPlanes[i].ToString() + "\n";
							break;
						case 5:
							sFrustrumOut = sFrustrumOut + "Near Plane: " + m_lFrustrumPlanes[i].ToString();
							break;
					}
				}
				m_fonter.PrintLowerRight(sFrustrumOut, m_GControl.Width, 0);
			}

			if (m_bDrawAxis)
			{
				Gl.glCallList(m_nDrawAxisList);
			}

			sgl.POPATT(); 
		}

		/// <summary>
		/// This method draws a red x-axis, green y-axis, and blue z-axis in
		/// their positive orientations.
		/// Each axis is 200 units long.
		/// </summary>
		private void GenerateDrawAxesDisplayList()
		{
			m_nDrawAxisList = Gl.glGenLists(1);
			Gl.glNewList(m_nDrawAxisList, Gl.GL_COMPILE);
			{
				Constructs.DrawAxis(true);
			}
			Gl.glEndList();
		}

		virtual public void MouseMove(MouseEventArgs e, ref bool bPostOpen) { }
		
		protected void MoveMovableCameraViaMouse(MouseEventArgs e, ref bool bPostOpen)
		{
			if (bPostOpen)
			{
				m_cam.MouseX = m_cam.MiddleX;
				m_cam.MouseY = m_cam.MiddleY;
				m_cam.changeLookAtViaMouse();
				bPostOpen = false;
			}
			else
			{
				m_cam.MouseX = e.X;
				m_cam.MouseY = e.Y;
				m_cam.changeLookAtViaMouse();
			}
		}

		virtual public void LeftMouseDown() { }
		virtual public void RightMouseDown() { }

		virtual public void KeyDown(KeyEventArgs e) 
		{
			switch (e.KeyData)
			{
				case Keys.F10:
					m_bDrawAxis = !m_bDrawAxis;
					break;
			}
		}
	}
}
