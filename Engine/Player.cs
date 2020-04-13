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
using System.Windows.Forms;
using Tao.OpenGl;
using utilities;
using Tao.FreeGlut;
using System.Diagnostics;

namespace engine
{
	/// <summary>
	/// Clipping enabled and also shooting enabled
	/// </summary>
	public class Player:Engine
	{
		// ### MEMBER VARIABLES
		const double mcd_Height = .98;

		bool m_bDrawBoundingBoxesDuringDebugging = true;

		// These are the base projectiles for which additional projectiles are born. New projectiles
		// point to the same shapes and display lists. Therefore only delete one display list per type of projectile.
		private Axe m_figInitialAxe = new Axe(); 
		private NinjaStar m_figInitialStar = new NinjaStar();
		private Edge m_ray = new Edge();
		private List<IntersectionInfo> m_lRayIntersectionInfos = new List<IntersectionInfo>();
		IntersectionInfo m_Intersection = new IntersectionInfo(); // this is for two reasons. so I don't have to recreate it all the time and also for use around the class.
		double[] m_pvUtilMatrix = new double[16];
        SoundManager m_SoundManager = null;
		int m_nStepCounter = 0;
		
		MoveStates m_MovesForThisTick = new MoveStates();				

        EProjectiles m_ProjectileMode = EProjectiles.AXE;

		double m_dLastGameTickMoveScale = 0.0;

		StopWatchManager m_swmgr = null;

		Dictionary<MovableCamera.DIRECTION, double> m_dictLastMoveScales = new Dictionary<MovableCamera.DIRECTION, double>();

		// ###

		public enum EProjectiles { AXE, NINJASTAR };

		public Player(OpenGLControlModded.simpleOpenGlControlEx win, SoundManager sm) : base(win) 
		{ 
			m_SoundManager = sm; 
			m_swmgr = new StopWatchManager(m_cam, this); 
		}

		/// <summary>
		/// Copy driver to this
		/// </summary>
		/// <param m_DisplayName="driver">driver to copy</param>
		public Player(Engine driver):base(driver) 
		{
			InitializeProjectiles();
			m_swmgr = new StopWatchManager(m_cam, this);
		}		

		public void SetSoundManager(SoundManager sm)
		{
			m_SoundManager = sm;
		}

		public SoundManager GetSoundManager()
		{
			return m_SoundManager;
		}

		override public string GetGameMode()
		{
			return "PLAYER";
		}

		override public void PreSwitchModes() 
		{
			DeleteProjectiles();
		}

		private void DeleteProjectiles()
		{
			m_dynamicFigList.Clear(false); // Remove flying projectiles before their display lists are deleted
			m_figInitialAxe.Delete();
			m_figInitialStar.Delete();
		}

		private void InitializeProjectiles()
		{
			m_figInitialAxe.InitializeLists();
			m_figInitialStar.InitializeLists();
		}

		override public EEngineType GetClass()
		{
			return EEngineType.PLAYER;
		}

		override public void Delete()
		{
			base.Delete();

			DeleteProjectiles();
		}

		override public void InitializeLists()
		{
			base.InitializeLists();

			InitializeProjectiles();
		}

		override public void LoadMap(MapInfo map)
		{
			base.LoadMap(map);

			// Set m_cam to starting position
			Viewpoint vp = m_lStaticFigList[0].GetRandomViewPoint(true);
			if (vp != null)
			{
				m_cam.Position = vp.Position;
				m_cam.Position[2] += JUMP_OFFSET;
				m_cam.PHI_RAD = Math.PI / 2;
				m_cam.THETA_RAD = vp.Orientation[3];

				for (int i = 0; i < 5; i++ )
					m_cam.MoveForward(1.0); // get away from wall a bit
			}
			else
			{
				m_cam.Position = new D3Vect(10, 0, 10);
				m_cam.PHI_RAD = Math.PI / 2;
				m_cam.THETA_RAD = 0;
			}
		}

		protected override void Draw(int nNumFaceCount)
		{
			base.Draw(nNumFaceCount);

			sgl.PUSHATT(Gl.GL_CURRENT_BIT | Gl.GL_TEXTURE_BIT | Gl.GL_LINE_BIT);
			Gl.glDisable(Gl.GL_TEXTURE_2D);
			Gl.glLineWidth(1.5f);

			if (STATE.DebuggingMode)
			{
				if (m_bDrawBoundingBoxesDuringDebugging)
				{
					if(STATE.AllowPrinting) m_fonter.PrintTopRight("\n\n\nDRAWING BOUNDING BOXES", m_GControl.Width, m_GControl.Height, 0);
					m_lStaticFigList[0].DrawBoundingBoxes();
				}

				Gl.glColor3d(0.0, 1.0, 0.2);
				if (STATE.AllowPrinting) m_fonter.PrintTopRight("\n\nDEBUGGING ON", m_GControl.Width, m_GControl.Height, 0);
			}
			else {
				Gl.glColor3d(0.8, 0.0, 0.0);
				if (STATE.AllowPrinting) m_fonter.PrintTopRight("\n\nDEBUGGING OFF", m_GControl.Width, m_GControl.Height, 0);
			}

            string sFigureString = m_lStaticFigList[0].GetNumFaces.ToString() + " face(s)\n" + m_lStaticFigList[0].GetNumShapes +
				" shape(s)\n" + m_lStaticFigList[0].GetNumBoundingBoxes.ToString() + " bounding box(es)\n" + 
				m_lStaticFigList[0].GetLastBBoxText();

			int nLineCounter = 0;

			Gl.glColor3d(1.0, 1.0, 0.0);
			if (STATE.AllowPrinting) m_fonter.PrintTopLeft(sFigureString, m_GControl.Width, m_GControl.Height, 0);
			nLineCounter += 3;
			nLineCounter += m_lStaticFigList[0].GetNumBBoxesLastInside;

			int nCounter = 0;
			string sRayIntersectedFaces = "";
			foreach (IntersectionInfo i in m_lRayIntersectionInfos)
			{
				if (nCounter == 0)
				{
					Gl.glColor3ub(230,230,230);
					if (STATE.AllowPrinting)
					{
						m_fonter.PrintTopLeft("Ray Intersections " + m_lRayIntersectionInfos.Count.ToString(), m_GControl.Width, m_GControl.Height, nLineCounter++);
					}
				}
                string sTextureInfo = "";
                if(i.Face.GetParentShape().GetTextures().Count > 0)
                {
                    if(i.Face.GetParentShape().GetTextures().Count > 1)
                        sTextureInfo = i.Face.GetParentShape().GetTextures()[1].GetPath();
                    else
                        sTextureInfo = i.Face.GetParentShape().GetTextures()[0].GetPath();
                }
                sRayIntersectedFaces += "Index " + i.Face.Index.ToString() + ", Normal " + i.Face.GetNewNormal.ToString();
                if (!string.IsNullOrEmpty(sTextureInfo)) sRayIntersectedFaces += ", Path " + sTextureInfo; 
				nCounter++;
				if (nCounter < m_lRayIntersectionInfos.Count) 
					sRayIntersectedFaces += "\n";
			}

			Gl.glColor3ub(255, 255, 0);
			if(m_lRayIntersectionInfos.Count > 0) {
				if (STATE.AllowPrinting) m_fonter.PrintTopLeft(sRayIntersectedFaces, m_GControl.Width, m_GControl.Height, nLineCounter);
				nLineCounter += m_lRayIntersectionInfos.Count;
			}

			Gl.glColor3d(0.0, 1.0, 1.0);
			if (STATE.AllowPrinting) m_fonter.PrintTopLeft(m_cam.GetCurrentStateDataString(true), m_GControl.Width, m_GControl.Height, nLineCounter++);

			m_ray.Draw(new Color(180, 180, 255), true);

			byte intensity = 255;
			foreach(IntersectionInfo i in m_lRayIntersectionInfos) 
			{
				sgl.PUSHMAT();
				Gl.glTranslated(i.Intersection.x, i.Intersection.y, i.Intersection.z);
				Gl.glColor3ub(intensity, intensity, intensity);
				if (intensity == 15) intensity = 255;
				else intensity -= 60;
				Glut.glutSolidSphere(0.1, 20, 20);
				sgl.POPMAT();
			}

			sgl.POPATT();
		}

		override public void KeyDown(KeyEventArgs e)
		{
			base.KeyDown(e);

			switch (e.KeyData)
			{
				case Keys.K:
					m_dynamicFigList.Clear(false);
					m_lRayIntersectionInfos.Clear();
					m_ray.Clear();
					break;
                case Keys.E:
                    WarpForward();
                    break;
				case Keys.D1:
					m_ProjectileMode = EProjectiles.AXE;
					break;
				case Keys.D2:
					m_ProjectileMode = EProjectiles.NINJASTAR;
					break;
				case Keys.F9:
					STATE.DebuggingMode = !STATE.DebuggingMode;
					if (!STATE.DebuggingMode)
						TurnOffDebugging();
					break;
				case Keys.F11:
					m_bDrawBoundingBoxesDuringDebugging = !m_bDrawBoundingBoxesDuringDebugging;
					break;
				case Keys.F12:
					STATE.ShowDebuggingFaces = !STATE.ShowDebuggingFaces;
					break;
				case Keys.F:
					STATE.DrawFaceNormals = !STATE.DrawFaceNormals;
					break;
			}

			if((Control.ModifierKeys & Keys.Control) == Keys.Control) {
				Fire();
			}
		}

		private void Fire()
		{
			switch (m_ProjectileMode)
			{
				case EProjectiles.AXE:
					m_dynamicFigList.Add(new Axe(m_dynamicFigList.Count(), m_cam, m_figInitialAxe));
					m_SoundManager.PlayEffect(SoundManager.EEffects.ROCKET_AWAY);
					break;
				case EProjectiles.NINJASTAR:
					m_dynamicFigList.Add(new NinjaStar(m_dynamicFigList.Count(), m_cam, m_figInitialStar));
					m_SoundManager.PlayEffect(SoundManager.EEffects.PLASMA_AWAY);
					break;
			}
		}

		override public void LeftMouseDown()
		{
			Fire(); 
		}

		public List<IntersectionInfo> GetIntersectionInfos
		{
			get { return m_lRayIntersectionInfos; }
		}

		public override void RightMouseDown()
		{
			m_lRayIntersectionInfos.Clear();

			double prevphi = Cam.PHI_RAD;
			Cam.PHI_RAD = prevphi + 90.0 / GLB.RadToDeg;
			m_ray.Vertice1 = m_cam.GetLookAtNew;
			Cam.PHI_RAD = prevphi;
			m_ray.Vertice2 = m_ray.Vertice1 + ((m_cam.GetLookAtNew - m_cam.Position) * 1000);
			m_lStaticFigList[0].IntersectionTest(m_ray, m_lRayIntersectionInfos);
			
			foreach(IntersectionInfo intersectionInfo in m_lRayIntersectionInfos) {
				intersectionInfo.DistanceFromCam = (m_cam.Position - intersectionInfo.Intersection).Length;
			}
			m_lRayIntersectionInfos.Sort(CompareIntersectionInfos);
		}

		// return values
		// i1 less than i2 < 0
		// i1 equal i2 = 0
		// i1 greater than i2 > 0
		public static int CompareIntersectionInfos(IntersectionInfo i1, IntersectionInfo i2)
		{
			if (i1.DistanceFromCam < i2.DistanceFromCam) return -1;
			else if (i1.DistanceFromCam > i2.DistanceFromCam) return 1;
			else return 0;
		}

		/// <summary>
		/// Passed collision detection by this point. Just move here.
		/// </summary>
		/// <param name="eSourceMovement"></param>
		/// <param name="d3MoveTo"></param>
		/// <param name="nMoveAttemptCount"></param>
		/// <returns></returns>
		private bool InternalMove(MovableCamera.DIRECTION eSourceMovement, D3Vect d3MoveTo, int nMoveAttemptCount)
		{
			double dAccelDecelScale;

			if(eSourceMovement == MovableCamera.DIRECTION.DOWN)
			{
				dAccelDecelScale = m_swmgr.GetFallScale();
			}
			else
			{
				dAccelDecelScale = m_swmgr.GetAccelDecelScale(eSourceMovement);
			}

			bool bAllowKeyBasedScaling = !m_swmgr.IsRunning(eSourceMovement, true) && !m_swmgr.IsRunning(eSourceMovement, false);

			PlaySteps();

			double d = m_cam.MoveToPosition(d3MoveTo, bAllowKeyBasedScaling, dAccelDecelScale);
			if (eSourceMovement != MovableCamera.DIRECTION.DOWN)
			{
				m_dLastGameTickMoveScale = d; // revisit this double and the dict below. do we need? why the check on esourcemovement equaling DOWN?
				m_dictLastMoveScales[eSourceMovement] = d;
			}

			if (nMoveAttemptCount > 1)
				return false;
			else
				return true;
		}

		/// <summary>
		/// Attempt to move forward by determining if a face is in the way. If a collision is detected,
		/// slide the MovableCamera along the wall.
		/// </summary>
		/// <param name="d3MoveTo">target location to move to</param>
		/// <param name="d3Position">current location of the MovableCamera</param>
		private bool TryToMove(D3Vect d3MoveTo, D3Vect d3Position, ref int nMoveAttemptCount, bool bMoveAlongWallOrUpStairs, 
			MovableCamera.DIRECTION eSourceMovement, bool bDoTheMove = true)
		{
			nMoveAttemptCount++;

			if (bDoTheMove && nMoveAttemptCount >= 3) 
				return false;

			if (m_lStaticFigList[0].CanMove(d3MoveTo, d3Position, m_Intersection, m_cam))
			{
				if (!bDoTheMove) return true;								
				return InternalMove(eSourceMovement, d3MoveTo, nMoveAttemptCount);
			}
			else if(bMoveAlongWallOrUpStairs)
			{
				if (!bDoTheMove) return false;

				// first try to move up stairs if not jumping or falling
				// ===
				if (m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false) == false && m_swmgr.IsRunning(MovableCamera.DIRECTION.DOWN, true) == false)
				{
					D3Vect d3StepUpPos = new D3Vect(d3Position);
					D3Vect d3StepUpMoveTo = new D3Vect(d3MoveTo);
					d3StepUpPos.z = d3StepUpPos.z + 0.3;
					d3StepUpMoveTo.z = d3StepUpMoveTo.z + 0.3;

					if (m_lStaticFigList[0].CanMove(d3StepUpMoveTo, d3StepUpPos, null, m_cam))
					{
						bool bNoWallCollides = InternalMove(eSourceMovement, d3StepUpMoveTo, nMoveAttemptCount);
						return bNoWallCollides;
					}
				}
				// ===

				D3Vect normal = m_Intersection.Face.GetNewNormal;
				D3Vect dcamfor = d3MoveTo - d3Position;
				dcamfor.normalize();
				if (D3Vect.DotProduct(normal, dcamfor) > 0)
					normal.Negate(); // Force normal of face to point toward MovableCamera

				D3Vect cross = new D3Vect(dcamfor, normal);

				double dBounceAngle = Math.Acos(D3Vect.DotProduct(dcamfor, normal)) * GLB.RadToDeg;

				dBounceAngle -= 90;

				if (dBounceAngle <= 0 || dBounceAngle > 90)
					throw new Exception(Convert.ToString(dBounceAngle) + " is an invalid bounce angle. Range is (0 < angle <= 90)");

				Gl.glPushMatrix();
				Gl.glLoadIdentity();
				Gl.glRotated(dBounceAngle, cross.x, cross.y, cross.z);
				Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, m_pvUtilMatrix);
				Gl.glPopMatrix();

				D3Vect d3SlideVector = D3Vect.Mult(m_pvUtilMatrix, dcamfor);

				double len;
				if (dBounceAngle == 0) len = 0;
				else len = Cam.RHO * ((90 - dBounceAngle) / 90);
				d3SlideVector.Length = len;

				D3Vect d3NewMoveTo = new D3Vect(m_cam.Position + d3SlideVector);

				return TryToMove(d3NewMoveTo, d3Position, ref nMoveAttemptCount, true, eSourceMovement);
			}
			else
			{
				return false;
			}
		}

		public override double GetVelocity()
		{
			return m_dLastGameTickMoveScale;
		}        

		private void WarpForward()
        {
            // to get through doors...
            m_cam.MoveForward(30.0);
        }

		private void MoveInternal(MovableCamera.DIRECTION dir)
		{ 
			switch(dir)
			{
				case MovableCamera.DIRECTION.FORWARD: 
					m_cam.LookStraight();
					break;
				case MovableCamera.DIRECTION.BACK: 
					m_cam.TurnBack();
					break;
				case MovableCamera.DIRECTION.LEFT: 
					m_cam.TurnLeft();
					break;
				case MovableCamera.DIRECTION.RIGHT: 
					m_cam.TurnRight();
					break;
				case MovableCamera.DIRECTION.FORWARD_LEFT: 
					m_cam.TurnLeftHalf();
					break;
				case MovableCamera.DIRECTION.FORWARD_RIGHT: 
					m_cam.TurnRightHalf();
					break;
				case MovableCamera.DIRECTION.BACK_LEFT: 
					m_cam.TurnBackLeft();
					break;
				case MovableCamera.DIRECTION.BACK_RIGHT: 
					m_cam.TurnBackRight();
					break;
				case MovableCamera.DIRECTION.UP:
					m_cam.TurnUp();
					break;
				case MovableCamera.DIRECTION.DOWN:
					m_cam.TurnDown();
					break;
				default:
					throw new Exception("Invalid move: " + dir);
			}			

			int nMoveAttemptCount = 0;
			bool bMoveAlongWall = dir != MovableCamera.DIRECTION.UP;
			bool bSuccess = TryToMove(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, bMoveAlongWall, dir);
			if(!bSuccess && dir == MovableCamera.DIRECTION.UP)
			{
				m_swmgr.Command(MovableCamera.DIRECTION.UP, false, StopWatchManager.SWCommand.RESET);
			}
			m_cam.RestoreOrientation();
		}

		override public void CacheMove(MovableCamera.DIRECTION direction)
		{
			m_MovesForThisTick.SetState(direction, true);
		}

		private void HandleAccelDecel(MoveStates stoppedMovingStates, MoveStates startedMovingStates)
		{
			m_swmgr.HandleStoppedMoving(stoppedMovingStates, m_dictLastMoveScales);
			m_swmgr.HandleStartedMoving(startedMovingStates, m_dictLastMoveScales);

			// do deceleration movement if needed
			if (m_swmgr.IsRunning(MovableCamera.DIRECTION.FORWARD, false))
			{
				// player stopped moving and they are decelerating to a stop
				if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.FORWARD, false) >= m_swmgr.GetMaxMS(MovableCamera.DIRECTION.FORWARD, false))
				{
					m_swmgr.Command(MovableCamera.DIRECTION.FORWARD, false, StopWatchManager.SWCommand.RESET);
				}
				else
				{
					MoveInternal(MovableCamera.DIRECTION.FORWARD);
				}               
            }

			if (m_swmgr.IsRunning(MovableCamera.DIRECTION.BACK, false))
			{
				if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.BACK, false) >= m_swmgr.GetMaxMS(MovableCamera.DIRECTION.BACK, false))
				{
					m_swmgr.Command(MovableCamera.DIRECTION.BACK, false, StopWatchManager.SWCommand.RESET);
				}
				else
				{
					MoveInternal(MovableCamera.DIRECTION.BACK);
				}
			}

			if (m_swmgr.IsRunning(MovableCamera.DIRECTION.LEFT, false))
			{
				if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.LEFT, false) >= m_swmgr.GetMaxMS(MovableCamera.DIRECTION.LEFT, false))
				{
					m_swmgr.Command(MovableCamera.DIRECTION.LEFT, false, StopWatchManager.SWCommand.RESET);
				}
				else
				{
					MoveInternal(MovableCamera.DIRECTION.LEFT);
				}
			}

			if (m_swmgr.IsRunning(MovableCamera.DIRECTION.RIGHT, false))
			{
				if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.RIGHT, false) >= m_swmgr.GetMaxMS(MovableCamera.DIRECTION.RIGHT, false))
				{
					m_swmgr.Command(MovableCamera.DIRECTION.RIGHT, false, StopWatchManager.SWCommand.RESET);
				}
				else
				{
					MoveInternal(MovableCamera.DIRECTION.RIGHT);
				}
			}

			if(m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false))
			{
                if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.UP, false) >= m_swmgr.GetMaxMS(MovableCamera.DIRECTION.UP, false))
                {
                    m_swmgr.Command(MovableCamera.DIRECTION.UP, false, StopWatchManager.SWCommand.RESET);
                }
                else
                {
                    MoveInternal(MovableCamera.DIRECTION.UP);
                }
            }

			// stop accel timers if needed
			m_swmgr.StopAccelTimers();
		}

		override public void GameTick(MoveStates stoppedMovingStates, MoveStates startedMovingStates) 
		{
			// Handle cached moves here. The cached moves represent the movement keys the user pressed since the last game tick
			// If they pressed left and forward we will move diagnolly once instead of left and then forward. This reduces
			// the number of collision detection tests and should provide smoother movement especially along walls

			HandleAccelDecel(stoppedMovingStates, startedMovingStates);

			// standard moves
			if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.FORWARD))
			{
				MoveInternal(MovableCamera.DIRECTION.FORWARD);
			}
            if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.BACK))
            {
				MoveInternal(MovableCamera.DIRECTION.BACK);
			}
            if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.LEFT))
            {
				MoveInternal(MovableCamera.DIRECTION.LEFT);
			}
            if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.RIGHT))
            {
				MoveInternal(MovableCamera.DIRECTION.RIGHT);
			}

			if(!m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false)) // don't fall if jumping
				Fall();

			bool bUserMoved = m_MovesForThisTick.AnyTrue();

            // clear way for next tick
            m_MovesForThisTick.Clear();

			if (!m_swmgr.GetAnyRunning(StopWatchManager.AccelModes.DECEL) && !bUserMoved)
            {
                m_dLastGameTickMoveScale = 0.0;
				m_dictLastMoveScales[MovableCamera.DIRECTION.FORWARD] = 0.0;
				m_dictLastMoveScales[MovableCamera.DIRECTION.BACK] = 0.0;
				m_dictLastMoveScales[MovableCamera.DIRECTION.LEFT] = 0.0;
				m_dictLastMoveScales[MovableCamera.DIRECTION.RIGHT] = 0.0;
			}
        }

		private SoundManager.EEffects HandleJumppads(string sTextureInfo)
		{
			SoundManager.EEffects eEffect = SoundManager.EEffects.NONE;

			// jumppads
			if (sTextureInfo.Contains("diamond2cjumppad") || sTextureInfo.Contains("bouncepad01_block17") || sTextureInfo.Contains("bounce_largeblock3b")
				|| sTextureInfo.Contains("bounce_concrete"))
			{
				if (!m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false))
				{
					m_SoundManager.PlayEffect(SoundManager.EEffects.JUMPPAD);
					m_swmgr.Jump(1000);
					eEffect = SoundManager.EEffects.NONE;
				}
			} // launch pads
            /*else if (sTextureInfo.Contains("launchpad"))
            {
                if (!m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false))
                {
                    m_SoundManager.PlayEffect(SoundManager.EEffects.JUMPPAD);
                    m_swmgr.Jump(1000);
                    eEffect = SoundManager.EEffects.NONE;
                }
            }*/

            return eEffect;
		}

		/// <summary>
		/// Attempt to fall. This is called every game tick. It checks if there is ground below you. If there isn't, you move downward in an accelerating
		/// fashion as if gravity is pulling you down.
		/// </summary>
		override public void Fall()
        {
            m_cam.TurnDown();
            int nMoveAttemptCount = 0;
            bool bCanMove = TryToMove(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, false, MovableCamera.DIRECTION.DOWN, false);
			m_cam.RestoreOrientation();

			SoundManager.EEffects eEffectToPlay = SoundManager.EEffects.NONE;

			// enable stopwatch
			if (bCanMove && !m_swmgr.IsRunning(MovableCamera.DIRECTION.DOWN, true))
			{
				// start falling		
				m_swmgr.Command(MovableCamera.DIRECTION.DOWN, true, StopWatchManager.SWCommand.START);
			}

			if(!bCanMove && m_swmgr.IsRunning(MovableCamera.DIRECTION.DOWN, true))
			{
				// stop falling
				if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.DOWN, true) >= 1500)
				{
					eEffectToPlay = SoundManager.EEffects.FALL;					
				}
				else if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.DOWN, true) >= 750)
				{
					eEffectToPlay = SoundManager.EEffects.FALL_MINOR;
				}
				else if (m_swmgr.GetElapsed(MovableCamera.DIRECTION.DOWN, true) >= 250) 
				{
					eEffectToPlay = SoundManager.EEffects.LAND;
				}
                m_swmgr.Command(MovableCamera.DIRECTION.DOWN, true, StopWatchManager.SWCommand.RESET);
            }
		
			if (m_swmgr.IsRunning(MovableCamera.DIRECTION.DOWN, true))
			{
				// fall
				MoveInternal(MovableCamera.DIRECTION.DOWN);				
			}
			else
			{               
                string sTextureInfo = "";
                if (m_Intersection.Face.GetParentShape().GetTextures().Count > 0)
                {
                    if (m_Intersection.Face.GetParentShape().GetTextures().Count > 1)
                        sTextureInfo = m_Intersection.Face.GetParentShape().GetTextures()[1].GetPath();
                    else
                        sTextureInfo = m_Intersection.Face.GetParentShape().GetTextures()[0].GetPath();
                }

				SoundManager.EEffects eEffectReturn = SoundManager.EEffects.NONE;
				eEffectReturn = HandleJumppads(sTextureInfo);
				if (eEffectReturn != SoundManager.EEffects.NONE) eEffectToPlay = eEffectReturn;
				m_SoundManager.PlayEffect(eEffectToPlay);

                // check that we are the right distance from the ground(player's height)
                double dCurrentPlayerHeight = m_cam.Position.z - m_Intersection.Intersection.z;
                if (dCurrentPlayerHeight < mcd_Height)
                {
                    D3Vect d3CamPos = m_cam.Position;
                    d3CamPos.z = d3CamPos.z + (mcd_Height - dCurrentPlayerHeight);
                }
            }
        }

        public override void MouseMove(MouseEventArgs e, ref bool bPostOpen)
		{
			MoveMovableCameraViaMouse(e, ref bPostOpen);
		}

        private void PlaySteps()
        {
            // if we are moving on ground, consider playing a step sound
            if (m_swmgr.IsRunning(MovableCamera.DIRECTION.UP, false) == false && m_swmgr.IsRunning(MovableCamera.DIRECTION.DOWN, true) == false)
            {
                bool bPlayStep = false;

                if (m_swmgr.GetStepper().IsRunning == false)
                {
                    bPlayStep = true;
                    m_swmgr.GetStepper().Start();
                }
                else if (m_swmgr.GetStepper().IsRunning && m_swmgr.GetStepper().ElapsedMilliseconds > 400)
                {
                    // the step sound has played already and hopefully is finished. stop stopwatch so it can start again next time it needs to
                    m_swmgr.GetStepper().Reset();
                    //bPlayStep = true;
                }

				if (bPlayStep)
				{
					/*m_nStepCounter++;
					if (m_nStepCounter == 4) m_nStepCounter = 0;
					switch(m_nStepCounter)
					{
						case 0: m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP1); break;
						case 1: m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP2); break;
						case 2: m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP3); break;
						case 3: m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP); break;
					}*/

					m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP3);
				}
            }
            // ===
        }
    }
}
