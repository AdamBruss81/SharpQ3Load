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

		bool m_bDrawBoundingBoxesDuringDebugging = true;

		// These are the base projectiles for which additional projectiles are born. New projectiles
		// point to the same shapes and display lists. Therefore only delete one display list per type of projectile.
		private Axe m_figInitialAxe = new Axe(); 
		private NinjaStar m_figInitialStar = new NinjaStar();
		private Edge m_ray = new Edge();
		private List<IntersectionInfo> m_lRayIntersectionInfos = new List<IntersectionInfo>();
		IntersectionInfo m_Intersection = new IntersectionInfo();
		double[] m_pvUtilMatrix = new double[16];
        SoundManager m_SoundManager = null;
		Stopwatch m_swFallTimer = new Stopwatch();
		MoveStates m_MovesForThisTick = new MoveStates();
		Stopwatch m_swPostMoveDecelTimer = new Stopwatch();
		Stopwatch m_swStartMoveAccelTimer = new Stopwatch();
		EProjectiles m_ProjectileMode = EProjectiles.AXE;

		MovableCamera.DIRECTION m_eDecelingDirection = MovableCamera.DIRECTION.NONE;
		MovableCamera.DIRECTION m_eAccelingDirection = MovableCamera.DIRECTION.NONE;

		const double m_dDecelAccelTimeMS = 200.0;
		double m_dLastGameTickMoveScale = 0.0;
		double m_dDecelStartMoveScale = 0.0;

        double m_dAccelStartMoveScale = 0.0;

        // ###

        public enum EProjectiles { AXE, NINJASTAR };

		public Player(OpenGLControlModded.simpleOpenGlControlEx win, SoundManager sm) : base(win) { m_SoundManager = sm; }

		/// <summary>
		/// Copy driver to this
		/// </summary>
		/// <param m_DisplayName="driver">driver to copy</param>
		public Player(Engine driver):base(driver) 
		{
			InitializeProjectiles();
		}		

		public void SetSoundManager(SoundManager sm)
		{
			m_SoundManager = sm;
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
					//m_SoundManager.PlayEffect(SoundManager.EEffects.ROCKET_AWAY);
					m_SoundManager.PlayEffect(SoundManager.EEffects.ROCKET_AWAY);
					break;
				case EProjectiles.NINJASTAR:
					m_dynamicFigList.Add(new NinjaStar(m_dynamicFigList.Count(), m_cam, m_figInitialStar));
					//m_SoundManager.PlayEffect(SoundManager.EEffects.PLASMA_AWAY);
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
		/// Attempt to move forward by determining if a face is in the way. If a collision is detected,
		/// slide the MovableCamera along the wall.
		/// </summary>
		/// <param name="d3MoveTo">target location to move to</param>
		/// <param name="d3Position">current location of the MovableCamera</param>
		private bool TryToMoveForward(D3Vect d3MoveTo, D3Vect d3Position, ref int nMoveAttemptCount, bool bMoveAlongWall, MovableCamera.DIRECTION eSourceMovement)
		{
			nMoveAttemptCount++;

			if (nMoveAttemptCount >= 3) 
				return false;

			if (m_lStaticFigList[0].CanMove(d3MoveTo, d3Position, m_Intersection, m_cam))
			{
				double dAccelDecelScale = 1.0;
				if (m_swFallTimer.IsRunning)
				{
					dAccelDecelScale = GetFallScale();
				}
				else {
					if (m_swPostMoveDecelTimer.IsRunning)
					{
						dAccelDecelScale = GetSlowDownScale();
					}
					if(m_swStartMoveAccelTimer.IsRunning)
					{
						dAccelDecelScale = GetSpeedUpScale();
                    }
                 }

				double d = m_cam.MoveToPosition(d3MoveTo, !AcceleratingOrDecelerating() && !AreFalling(), dAccelDecelScale);
				if (eSourceMovement != MovableCamera.DIRECTION.DOWN) m_dLastGameTickMoveScale = d;

				if (nMoveAttemptCount > 1)
					return false;
				else 
					return true;
			}
			else if(bMoveAlongWall)
			{
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

				return TryToMoveForward(d3NewMoveTo, d3Position, ref nMoveAttemptCount, true, eSourceMovement);
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

		private double GetSlowDownScale()
		{
            double dScale = 1.0;

			double dRatio = (double)m_swPostMoveDecelTimer.ElapsedMilliseconds / m_dDecelAccelTimeMS;
			dScale = (1.0 - dRatio) * m_dDecelStartMoveScale;

			LOGGER.Debug("Decel scale is " + dScale + " for ellapsed of " + m_swPostMoveDecelTimer.ElapsedMilliseconds);

			return dScale;
        }
		 
        private double GetSpeedUpScale()
        {
            double dScale = 1.0;

			//double dRatio = (double)m_swStartMoveAccelTimer.ElapsedMilliseconds / m_dDecelAccelTimeMS;
			//dScale = (dRatio) * m_dAccelStartMoveScale;

			dScale = (double)m_swStartMoveAccelTimer.ElapsedMilliseconds * .0075;

			LOGGER.Debug("Accel scale is " + dScale + " for ellapsed of " + m_swStartMoveAccelTimer.ElapsedMilliseconds);

            return dScale;
        }

        private double GetFallScale()
		{
			double dScale = 1.0;

			// slower than 9.8 to account for fast timer tick
			dScale = 5.0 * m_swFallTimer.ElapsedMilliseconds / 1000;

            return dScale;
		}

		private void WarpForward()
        {
            // to get through doors...
            m_cam.MoveForward(30.0);
        }

		private void MoveInternal(MovableCamera.DIRECTION dir, bool bUserMove)
		{
			if (AreFalling()) return;

			if (bUserMove && m_swPostMoveDecelTimer.IsRunning)
			{
				m_swPostMoveDecelTimer.Reset(); // stop deceleration because user inputted new move command
			}

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
			}			

			int nMoveAttemptCount = 0;
			TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, true, dir);
			m_cam.RestoreOrientation();
		}

       
		private bool AreFalling()
		{
			return m_swFallTimer.IsRunning;
		}

		private bool AcceleratingOrDecelerating()
		{
			return m_swPostMoveDecelTimer.IsRunning || m_swStartMoveAccelTimer.IsRunning;
		}

		override public void CacheMove(MovableCamera.DIRECTION direction)
		{
			m_MovesForThisTick.SetState(direction, true);
		}

		private void HandleAccelDecel(MoveStates stoppedMovingStates, MoveStates startedMovingStates)
		{
			if (stoppedMovingStates.AnyTrue())
			{
				m_swStartMoveAccelTimer.Reset(); // if you stop moving, reset the accel stopwatch
				m_eDecelingDirection = stoppedMovingStates.GetRelevant();
				m_dDecelStartMoveScale = m_dLastGameTickMoveScale;
				m_swPostMoveDecelTimer.Start();
				LOGGER.Debug("Stopped moving with decel move scale " + m_dDecelStartMoveScale);
			}
			if (startedMovingStates.AnyTrue())
			{
				m_swPostMoveDecelTimer.Reset(); // if start moving, stop decel timer for now

				m_eAccelingDirection = startedMovingStates.GetRelevant();
				m_dAccelStartMoveScale = m_dLastGameTickMoveScale;
				m_swStartMoveAccelTimer.Start();
				LOGGER.Debug("Started moving with accel move scale " + m_dAccelStartMoveScale);
			}

			// do deceleration movement if needed
			if (m_swPostMoveDecelTimer.IsRunning)
			{
				// player stopped moving and they are decelerating to a stop
				if (m_swPostMoveDecelTimer.ElapsedMilliseconds >= (long)m_dDecelAccelTimeMS)
				{
					m_swPostMoveDecelTimer.Reset();
				}
				else
				{
					MoveInternal(m_eDecelingDirection, false);
				}
			}
			if (m_swStartMoveAccelTimer.IsRunning)
			{
				if (m_swStartMoveAccelTimer.ElapsedMilliseconds >= (long)m_dDecelAccelTimeMS)
				{
					m_swStartMoveAccelTimer.Reset();
				}
			}
		}

		override public void GameTick(MoveStates stoppedMovingStates, MoveStates startedMovingStates) 
		{
			// Handle cached moves here. The cached moves represent the movement keys the user pressed since the last game tick
			// If they pressed left and forward we will move diagnolly once instead of left and then forward. This reduces
			// the number of collision detection tests and should provide smoother movement especially along walls

			HandleAccelDecel(stoppedMovingStates, startedMovingStates);

			// standard moves
			if (m_MovesForThisTick.OnlyState(MovableCamera.DIRECTION.FORWARD))
			{
				MoveInternal(MovableCamera.DIRECTION.FORWARD, true);
			}
            else if (m_MovesForThisTick.OnlyState(MovableCamera.DIRECTION.BACK))
            {
				MoveInternal(MovableCamera.DIRECTION.BACK, true);
			}
            else if (m_MovesForThisTick.OnlyState(MovableCamera.DIRECTION.LEFT))
            {
				MoveInternal(MovableCamera.DIRECTION.LEFT, true);
			}
            else if (m_MovesForThisTick.OnlyState(MovableCamera.DIRECTION.RIGHT))
            {
				MoveInternal(MovableCamera.DIRECTION.RIGHT, true);
			}
			// strafe running
			else if(m_MovesForThisTick.GetState(MovableCamera.DIRECTION.FORWARD) && m_MovesForThisTick.GetState(MovableCamera.DIRECTION.RIGHT))
			{
				MoveInternal(MovableCamera.DIRECTION.FORWARD_RIGHT, true);
			}
            else if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.FORWARD) && m_MovesForThisTick.GetState(MovableCamera.DIRECTION.LEFT))
            {
                MoveInternal(MovableCamera.DIRECTION.FORWARD_LEFT, true);
            }
            else if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.BACK) && m_MovesForThisTick.GetState(MovableCamera.DIRECTION.LEFT))
            {
                MoveInternal(MovableCamera.DIRECTION.BACK_LEFT, true);
            }
            else if (m_MovesForThisTick.GetState(MovableCamera.DIRECTION.BACK) && m_MovesForThisTick.GetState(MovableCamera.DIRECTION.RIGHT))
            {
                MoveInternal(MovableCamera.DIRECTION.BACK_RIGHT, true);
            }

			bool bUserMoved = m_MovesForThisTick.AnyTrue();

            // clear way for next tick
            m_MovesForThisTick.Clear();

			if (!m_swPostMoveDecelTimer.IsRunning && !bUserMoved)
            {
                m_dLastGameTickMoveScale = 0.0;
            }
        }

		/// <summary>
		/// Attempt to fall. This is called every game tick. It checks if there is ground below you. If there isn't, you move downward in an accelerating
		/// fashion as if gravity is pulling you down.
		/// </summary>
		override public void Fall()
        {
            m_cam.TurnDown();

            int nMoveAttemptCount = 0;
            bool bCanMove = TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, false, MovableCamera.DIRECTION.DOWN);
			if (bCanMove)
			{
				if (!m_swFallTimer.IsRunning)
				{
					// start falling				
					m_swFallTimer.Start();
				}
			}
			else
			{
				if (m_swFallTimer.IsRunning)
				{
					// stop falling
					if(m_swFallTimer.ElapsedMilliseconds >= 1000)
					{
						m_SoundManager.PlayEffect(SoundManager.EEffects.FALL);
					}
					m_swFallTimer.Reset();
				}
			}
            m_cam.RestoreOrientation();
        }

        public override void MouseMove(MouseEventArgs e, ref bool bPostOpen)
		{
			MoveMovableCameraViaMouse(e, ref bPostOpen);
		}
	}
}
