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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Diagnostics;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;
using utilities;
using engine;
using System.Threading;

namespace simulator
{
	/// <summary>
	/// Console for driving entire application
	/// </summary>
	public partial class SimulatorForm : Form
	{
		private Engine m_Engine = null;

		private bool m_bRunning = false;
		private bool m_bPostOpen = false;
		private bool m_bOpeningMap = false;
		private bool m_bPaused = false;
		private bool m_bClosed = false;
		private IntPtr m_mainDC = IntPtr.Zero;
		private IntPtr m_mainRC = IntPtr.Zero;
        private Stopwatch m_swFramerate = new Stopwatch();
        private Stopwatch m_swDelayMusicStart = new Stopwatch();
        private SoundManager m_SoundManager = new SoundManager();
        private int m_nFrameCounter = 0;
        private double m_dElapsedSecondsShowScene = 0.0;

		private OpenGLControlModded.simpleOpenGlControlEx m_openGLControl;		
		private MapChooserForm m_menu;

		Point m_CursorPoint = new Point();
		private Zipper m_zipper = new Zipper();

		private const int MOVEMENT_INTERVAL = 10;

		Keys m_RecentKey;

		gl_font.BasicFont m_fonter = null;

		/// <summary>
		/// Global engine to be accessed by MapProgress
		/// </summary>
		public static Engine static_theEngine = null;

		/// <summary>
		/// Global map to be accessed by MapProgress
		/// </summary>
		public static MapInfo static_theMap = null;

		/// <summary>
		/// Global semaphore to keep track of cursor visibility
		/// </summary>
		public static bool m_bCursorShown = true;

		/// <summary>
		/// Global static indicator of map load progress
		/// </summary>
		public static bool m_bLoadingMap = false;

		/// <summary>
		/// Initialize a Simulator
		/// </summary>
		public SimulatorForm()
		{
			InitializeComponent();

			m_menu = new MapChooserForm();

			m_openGLControl.ProcessKey += new KeyEventHandler(m_openGLControl_ProcessKey);

			timerMoveForward.Interval = MOVEMENT_INTERVAL;
			timerMoveBackward.Interval = MOVEMENT_INTERVAL;
			timerStrafeLeft.Interval = MOVEMENT_INTERVAL;
			timerStrafeRight.Interval = MOVEMENT_INTERVAL;
			timerMoveUp.Interval = MOVEMENT_INTERVAL;
			timerMoveDown.Interval = MOVEMENT_INTERVAL;
			timerFallChecker.Interval = MOVEMENT_INTERVAL;
			timerRedrawer.Interval = 8;

			SetViewMode(true);
		}

		private void Simulator_Load(object sender, EventArgs e)
		{
			m_openGLControl.InitializeContexts();
			Glut.glutInit();

			m_mainDC = Wgl.wglGetCurrentDC();
			m_mainRC = Wgl.wglGetCurrentContext();

			m_fonter = new gl_font.BasicFont();

			Gl.glShadeModel(Gl.GL_SMOOTH);

            Gl.glEnable(Gl.GL_CULL_FACE);
            Gl.glCullFace(Gl.GL_FRONT);

			Gl.glClearColor(0.0f, 0.0f, 0.0f, .5f);
			Gl.glClearDepth(1.0f);

			Gl.glEnable(Gl.GL_DEPTH_TEST);
			Gl.glMatrixMode(Gl.GL_PROJECTION);

			Gl.glDepthFunc(Gl.GL_LEQUAL);
			Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);

			Gl.glLoadIdentity();
			Glu.gluPerspective(70, (float)m_openGLControl.Width / (float)m_openGLControl.Height, .005f, 200.0f);

			Gl.glMatrixMode(Gl.GL_MODELVIEW);

			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

			m_openGLControl.Focus();

			SetCursor(false, false);

			OpenMap();
		}				

		private void SetViewMode(bool bSimming)
		{
			SuspendLayout();
			if (bSimming == true)
			{
				m_openGLControl.Visible = true;
				if (m_tlContainer.Controls.Contains(m_controlMapProgress))
				{
					m_tlContainer.Controls.Remove(m_controlMapProgress);
					Debug.Assert(!m_tlContainer.Controls.Contains(m_controlMapProgress));
				}
				m_controlMapProgress.Visible = false;
				if (!m_tlContainer.Controls.Contains(m_openGLControl))
				{
					m_tlContainer.Controls.Add(m_openGLControl, 0, 0);
					Debug.Assert(m_tlContainer.Controls.Contains(m_openGLControl));
				}
				m_tlContainer.SetRowSpan(m_openGLControl, 3);
				m_tlContainer.SetColumnSpan(m_openGLControl, 3);
				m_openGLControl.Dock = DockStyle.Fill;
				m_tlContainer.Dock = DockStyle.Fill;
			}
			else 
			{
				m_controlMapProgress.Visible = true;
				if(m_tlContainer.Controls.Contains(m_openGLControl))
				{
					m_tlContainer.Controls.Remove(m_openGLControl);
					Debug.Assert(!m_tlContainer.Controls.Contains(m_openGLControl));
				}
				m_openGLControl.Visible = false;
				if(!m_tlContainer.Controls.Contains(m_controlMapProgress))
				{
					m_tlContainer.Controls.Add(m_controlMapProgress, 1, 1);
					Debug.Assert(m_tlContainer.Controls.Contains(m_controlMapProgress));
				}
				m_tlContainer.SetRowSpan(m_controlMapProgress, 1);
				m_tlContainer.SetColumnSpan(m_controlMapProgress, 1);
				m_controlMapProgress.Dock = DockStyle.Fill;
				m_tlContainer.Dock = DockStyle.Fill;
			}
			ResumeLayout();
		}

		/// <summary>
		/// Set state of cursor
		/// </summary>
		/// <param name="bShow">true to show, false to hide</param>
		/// <param name="bHalting">override cursor error checking because program has crashed and we don't care</param>
		public static void SetCursor(bool bShow, bool bHalting)
		{
			if ((m_bCursorShown && bShow && !bHalting) || (!m_bCursorShown && !bShow && !bHalting))
				throw new Exception("Invalid cursor operation");

			if (bShow) Cursor.Show();
			else Cursor.Hide();

			m_bCursorShown = bShow;
		}

		private void bw_LoadMap(object sender, DoWorkEventArgs e)
		{
			m_bLoadingMap = true;

			m_controlMapProgress.Details("Loading map");
			static_theEngine.LoadMap(static_theMap);

			m_controlMapProgress.DoneLoadingMap();

			m_controlMapProgress.Details("Initializing engine");
			static_theEngine.Initialize();

			m_controlMapProgress.DoneCreatingBoundingBoxes();

			Wgl.wglMakeCurrent(m_mainDC, m_mainRC);

			m_controlMapProgress.Details("Initializing lists");
			static_theEngine.InitializeLists();

			Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);

			m_controlMapProgress.Details("Cleaning up map");
			static_theMap.CleanUpMap();

			m_controlMapProgress.Details("Finalizing");
			m_controlMapProgress.DoneInitializingLists();

			LOGGER.Info("Exiting Loadmap thread");
		}

		/// <summary>
		/// Gets run after map load is done. Back in main thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void bw_LoadMapCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			LOGGER.Info("Entered load map completed function");

			Wgl.wglMakeCurrent(m_mainDC, m_mainRC);

			m_bLoadingMap = false;
			m_bClosed = false;

			m_controlMapProgress.Reset();

			SetViewMode(true);

			m_openGLControl.Focus();

			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			Gl.glFlush();

			m_Engine.showScene(GetRecentKey);

			m_bPostOpen = true;

			resetMouseCursor();

			m_bRunning = true;
			StartStopRedrawer(true);

            //m_SoundManager.PlayEffect(SoundManager.EEffects.SPAWN);
            m_SoundManager.PlayEffect(SoundManager.EEffects.SPAWN);
            m_swDelayMusicStart.Reset(); // it could be going if you open a map right after opening a different one
            m_swDelayMusicStart.Start();
		}

		/// <summary>
		/// Halt rendering and show cursor
		/// </summary>
		public void Halt()
		{
			// disable timer to stop opengl refreshing and mouse cursor resetting
			m_bRunning = false;
			StopAllTimers();

			SetCursor(true, true);
		}

		private void OpenMapFromFile()
		{
			m_bOpeningMap = true;

			// disable timer to stop opengl refreshing and mouse cursor resetting
			m_bRunning = false;
			StopAllTimers();

			SetCursor(true, false);

			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "VRML Files (*.wrl)|*.wrl";
			DialogResult result = dlg.ShowDialog(this);
			MapInfo map = null;
			if(result == DialogResult.OK) {
				string sFile = dlg.FileName;
				map = new MapInfo(sFile);
			}

			ProcessMap(map);
		}

		/// <summary>
		/// Pop map chooser and open a new map
		/// </summary>
		private void OpenMap()
		{
			m_bOpeningMap = true;

			// disable timer to stop opengl refreshing and mouse cursor resetting
			m_bRunning = false;
			StopAllTimers();

			SetCursor(true, false);

            m_SoundManager.Stop();

			m_menu.ClearChosenMap();
			m_menu.ShowDialog();

			MapInfo map = m_menu.GetChosenMap;
			if (map != null && map.ExtractedFromZip)
				map.GetMapPathOnDisk = m_zipper.ExtractMap(map.GetPath);

			ProcessMap(map);
		}

		private void CloseMap()
		{
			if (m_bRunning)
			{
				m_bClosed = true;

				m_bRunning = false;

				StopAllTimers();

				m_Engine.Delete();

				m_Engine = null;
				static_theEngine = null;
				static_theMap = null;

				WriteOpenMapMessage();
			}
		}

		private void ProcessMap(MapInfo map)
		{
			// new map
			if (map != null)
			{
				if (m_Engine != null)
				{
					Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
					Gl.glFlush();
					Refresh();

					m_Engine.Delete();
				}

				static_theEngine = null;
				static_theMap = null;

				m_Engine = null;

				m_Engine = new Player(m_openGLControl, m_SoundManager);

				BackgroundWorker bw = new BackgroundWorker();
				bw.DoWork += bw_LoadMap;
				bw.RunWorkerCompleted += bw_LoadMapCompleted;

				static_theEngine = m_Engine;
				static_theMap = map;

				m_openGLControl.Visible = false;
				m_controlMapProgress.Visible = true;

				Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
				bw.RunWorkerAsync();

				SetViewMode(false);
				LOGGER.Info("About to call Begin() on map progress form");
				m_controlMapProgress.Begin();
				LOGGER.Info("Finished with map progress begin");

				SetCursor(false, false);
			}
			// revert to old map
			else if (map == null && m_Engine != null && m_Engine.NumStaticFigs > 0)
			{
				SetCursor(false, false);

				resetMouseCursor();

				// start timer for opengl refreshing
				m_bRunning = true;
				StartStopRedrawer(true);
                m_swDelayMusicStart.Start();
			}
			// no map currently chosen
			else
			{
				SetCursor(false, false);
				WriteOpenMapMessage();
			}

			m_bOpeningMap = false;
		}

		private void WriteOpenMapMessage()
		{
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			m_fonter.PrintGLUTCenter("Press 'O' to open a map", m_openGLControl.Width, m_openGLControl.Height, 0);
			Gl.glFlush();
			Refresh();
		}

		private void m_openGLControl_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				timerLeftMouse.Stop();
			}
			else if (e.Button == MouseButtons.Middle)
			{
				timerMoveUp.Stop();
			}
		}

		/// <summary>
		/// Get pointer to current opengl control window
		/// </summary>
		public OpenGLControlModded.simpleOpenGlControlEx OpenGLControl
		{
			get { return m_openGLControl; }
		}

		private void DisableDebuggingMode()
		{
			if (STATE.DebuggingMode)
			{
				STATE.DebuggingMode = false;
				m_Engine.TurnOffDebugging();
			}
		}

		// Set the MovableCamera mouse coordinate to new mouse position
		// show scene after every mouse movement
		private void openGLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (m_bRunning)
			{
				m_Engine.MouseMove(e, ref m_bPostOpen);
			}
		}

		// Create a bullet
		// Move created bullet along rho
		private void openGLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (m_bRunning)
			{
				if (m_Engine.GetClass() == Engine.EEngineType.PLAYER && e.Button == MouseButtons.Left)
				{
					m_Engine.LeftMouseDown();
					timerLeftMouse.Start();
				}
				else if (e.Button == MouseButtons.Left)
					m_Engine.LeftMouseDown();
				else if (e.Button == MouseButtons.Right)
					m_Engine.RightMouseDown();
				else if (e.Button == MouseButtons.Middle)
				{
					timerMoveDown.Stop();
					timerMoveUp.Start();
				}
			}
		}		

		// Centers this cursor to middle of openGlControl
		private void resetMouseCursor()
		{
			m_CursorPoint.X = Location.X + Width / 2;
			m_CursorPoint.Y = Location.Y + Height / 2;
			Cursor.Position = m_CursorPoint;
		}

		private void m_openGLControl_GotFocus(Object sender, EventArgs e)
		{
			if (!m_bOpeningMap && m_bPaused && !m_bLoadingMap && !m_bClosed && m_Engine != null)
			{
				m_bPaused = false;
				m_bRunning = true;

				StartStopRedrawer(true);
			}
			else if (m_Engine == null)
			{
				WriteOpenMapMessage();
			}
		}

		private void m_openGLControl_LostFocus(object sender, System.EventArgs e)
		{
			if (!m_bOpeningMap && !m_bPaused && !m_bLoadingMap && !m_bClosed && m_Engine != null)
			{
				m_bPaused = true;
				m_bRunning = false;
				StopAllTimers();
			}
		}	
	}
}
