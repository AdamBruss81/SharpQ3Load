using System;
using System.Windows.Forms;
using gl_font;
using utilities;
using obsvr;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace engine
{
	/// <summary>
	/// The basic game driver for the Simulator
	/// </summary>
	public abstract class Engine : Subject
	{
		public enum ESignals { DONE_READING_MAP = SignalStarts.g_nEngineStart, ZOOMED_IN, ZOOMED_OUT };

		bool m_bDrawAxis = false;

		int m_nDrawAxisList;

		public enum EEngineType { GHOST, PLAYER, SPECTATOR };
		public enum EGraphicsMode { WIREFRAME, TEXTURED_SHADED };

		protected FigureList m_dynamicFigList = null;
		protected FigureList m_lStaticFigList = null;
		protected BasicFont m_fonter = null;
		protected MovableCamera m_cam = null;
		protected OpenGLControlModded.simpleOpenGlControlEx m_GControl = null;
		protected D3Vect m_d3LastMovableCameraLookAt = new D3Vect();
		protected double[] m_pdModelview = new double[16];
		protected const float JUMP_OFFSET = 1.0f;

		private EGraphicsMode m_GraphicsMode = EGraphicsMode.TEXTURED_SHADED;

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

		public string GetGraphicsMode
		{
			get 
			{
				switch (m_GraphicsMode)
				{
					case EGraphicsMode.TEXTURED_SHADED:
						return "TEXTURED_SHADED";
					case EGraphicsMode.WIREFRAME:
						return "WIREFRAME";
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

			// handle pk3 maps here. convert them to the vrml style and alter map to point to the vrml.
			if (System.IO.Path.GetExtension(map.GetMapPathOnDisk) == ".pk3")
			{
				string pk3 = map.GetMapPathOnDisk; // need later
				string pk3Dir = System.IO.Path.GetDirectoryName(pk3);

				string sTempDir = PathHelper.GetUniqueTempDir();

                Zipper zipper = new Zipper();

				string sBSPName = GetBspName(pk3, zipper, sTempDir);
				string wrl = System.IO.Path.Combine(pk3Dir, sBSPName + ".wrl");
				map.SetPK3(pk3);
               
				if (System.IO.File.Exists(wrl))
                {
					map.ConvertToWRL(wrl);
					string sWRLName = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(wrl), null);

                    // copy pngs from pk3 dir to temp dir in case temp dir ones got deleted by OS
                    string[] pngs = System.IO.Directory.GetFiles(pk3Dir, "*.png");
                    foreach (string sPNG in pngs)
                    {
						if (sPNG.ToLower().Contains(sWRLName.ToLower()))
						{
							string sTargetPNG = PATHS.GetTempDir + "\\" + System.IO.Path.GetFileName(sPNG);
							if (!System.IO.File.Exists(sTargetPNG))
							{
								System.IO.File.Copy(sPNG, sTargetPNG, true);
							}
						}
                    }
                }
				else
                {
					string sMapsDir = System.IO.Path.Combine(sTempDir, "maps");
					System.IO.Directory.CreateDirectory(sMapsDir);

					// extract bsp from pk3					
					zipper.ExtractToCustomTargetDir(map.GetMapPathOnDisk, "+\\.bsp$", sTempDir);

					// run q3bsp on bsp
					string sq3bsp = System.IO.Path.Combine(sMapsDir, "q3bsp.exe");
					System.IO.File.Copy("q3bsp.exe", sq3bsp, true);

					string[] sBSPFiles = System.IO.Directory.GetFiles(sMapsDir, "*.bsp");

					System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(sq3bsp, "-2me " + System.IO.Path.GetFileNameWithoutExtension(sBSPFiles[0]));
					startInfo.RedirectStandardOutput = false;
					startInfo.UseShellExecute = false;
					startInfo.WorkingDirectory = System.IO.Path.Combine(sTempDir, "maps");
					System.Diagnostics.Process.Start(startInfo).WaitForExit();					

					// copy lightmap pngs to simulator temp folder and copy wrl to be in same folder as user chosen pk3
					string[] pngs = System.IO.Directory.GetFiles(startInfo.WorkingDirectory, "*.png");
					foreach(string sPNG in pngs)
                    {
						System.IO.File.Copy(sPNG, PATHS.GetTempDir + "\\" + System.IO.Path.GetFileName(sPNG), true);
						System.IO.File.Copy(sPNG, pk3Dir + "\\" + System.IO.Path.GetFileName(sPNG), true);
					}
					System.IO.File.Copy(startInfo.WorkingDirectory + "\\" + sBSPName  + ".wrl", wrl);					

					map.ConvertToWRL(wrl);
				}

                // read custom shaders
                zipper.ExtractAllShaderFiles(pk3, sTempDir);
                Q3Shader.ReadAllShadersInDir(sTempDir, GameGlobals.m_dictQ3ShaderContent);

				System.IO.Directory.Delete(sTempDir, true);
			}

			mapFigure.CheckBoundingBoxes(map);
			m_lStaticFigList.Add(mapFigure);
			mapFigure.Read(map);
		}

        private string GetBspName(string pk3, Zipper zipper, string sTempDir)
        {
			zipper.ExtractToCustomTargetDir(pk3, "+\\.bsp$", sTempDir);
            string[] sBSPFiles = System.IO.Directory.GetFiles(sTempDir + "\\maps", "*.bsp");
			string sName = System.IO.Path.GetFileNameWithoutExtension(sBSPFiles[0]);
			System.IO.File.Delete(sBSPFiles[0]);
			return sName;
        }

        public void Initialize()
		{
			if (m_lStaticFigList.Count() > 0) m_lStaticFigList[0].Initialize();
		}

		virtual public void InitializeLists()
		{
			if(m_lStaticFigList.Count() > 0) m_lStaticFigList[0].InitializeLists(false);
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
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.LoadIdentity();

			m_cam.GetLookAtRef(m_d3LastMovableCameraLookAt);
            Matrix4 lookat = Matrix4.LookAt((float)m_cam.Position[0], (float)m_cam.Position[1], (float)m_cam.Position[2],
				(float)m_d3LastMovableCameraLookAt[0], (float)m_d3LastMovableCameraLookAt[1], (float)m_d3LastMovableCameraLookAt[2], 0, 0, 1);
            GL.LoadMatrix(ref lookat);

			GameGlobals.m_CamPosition = m_cam.Position;
			GameGlobals.m_fFrameStartElapsedS = (float)GameGlobals.m_InstanceStopWatch.ElapsedMilliseconds / 1000.0f;
			GameGlobals.m_fFrameStartElapsedMS = GameGlobals.m_InstanceStopWatch.ElapsedMilliseconds;

			m_lStaticFigList.ShowAllFigures(m_GraphicsMode, m_cam);

			for(int i = 0; i < m_dynamicFigList.Count(); i++)
			{
				if(DateTime.Now - m_dynamicFigList[i].GetCreationDate() > TimeSpan.FromSeconds(10))
				{
					m_dynamicFigList.RemoveAt(i);
					i--;
				}
			}
			m_dynamicFigList.ShowAllFigures(m_GraphicsMode, m_cam); 

			Draw();			
		}

		public virtual void Delete()
		{
			m_lStaticFigList.DeleteAll();
			m_dynamicFigList.DeleteAll();
			GL.DeleteLists(m_nDrawAxisList, 1);
			if (m_fonter != null) m_fonter.Delete();
		}

		virtual public bool MoveForward() { return false; }
		virtual public void MoveBackward() { }
		virtual public void MoveLeft() { }
		virtual public void MoveRight() { }
		virtual public void MoveUp() { }
		virtual public void MoveDown() { }
		virtual public void GameTick(MoveStates stoppedMovingStates, MoveStates startedMovingStates, long nLastFrameTimeMilli) { }
		virtual public void CacheMove(MovableCamera.DIRECTION direction) { }
		virtual public double GetVelocity() { return 0.0; }
		virtual public void DoMapSounds() { }

		virtual protected void Draw()
		{
			sgl.PUSHATT(AttribMask.CurrentBit | AttribMask.TextureBit);

			if(STATE.AllowPrinting) 
			{
				GL.Disable(EnableCap.Texture2D);
				GL.Color3(1.0, 1.0, 0.0);
				m_fonter.PrintTopCenter(m_lStaticFigList[0].GetDisplayName, m_GControl.Width, m_GControl.Height, 0);
				GL.Color3(0.0, 0.93, 0.46);
				string sModes = "Mode " + GetGameMode() + "\n" + "Style " + GetGraphicsMode;
				m_fonter.PrintTopRight(sModes, m_GControl.Width, m_GControl.Height, 0);
				GL.Color3(1.0f, 1.0f, 1.0f);
			}

			if (m_bDrawAxis)
			{
				GL.CallList(m_nDrawAxisList);
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
			m_nDrawAxisList = GL.GenLists(1);
			GL.NewList(m_nDrawAxisList, ListMode.Compile);
			{
				Constructs.DrawAxis(true);
			}
			GL.EndList();
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
		virtual public void MiddleMouseDown() { }
		virtual public void MiddleMouseUp() { }

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
