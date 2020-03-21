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
		private bool m_bFalling = false;
		Stopwatch m_swFallTimer = new Stopwatch();

		Stopwatch m_swPostMoveDecelTimerForward = new Stopwatch();
		Stopwatch m_swPostMoveDecelTimerRight = new Stopwatch();
		Stopwatch m_swPostMoveDecelTimerLeft = new Stopwatch();
		Stopwatch m_swPostMoveDecelTimerBackward = new Stopwatch();

		EProjectiles m_ProjectileMode = EProjectiles.AXE;

		const double m_dDecelTimeMS = 200.0;

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

		private void ProcessDecelTimer(Stopwatch sw, MovableCamera.DIRECTION eSourceMovement, 
			MovableCamera.DIRECTION comparisonMovement, ref double dAccelDecelScale)
		{
			if ((eSourceMovement == comparisonMovement) && sw.IsRunning &&
				sw.ElapsedMilliseconds <= m_dDecelTimeMS)
			{
				dAccelDecelScale = GetSlowDownScale(sw);
			}
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

			if (nMoveAttemptCount >= 4) 
				return false;

			if (m_lStaticFigList[0].CanMove(d3MoveTo, d3Position, m_Intersection, m_cam))
			{
				double dAccelDecelScale = 1.0;
				if (m_bFalling)
				{
					Debug.Assert(m_swFallTimer.IsRunning);

					dAccelDecelScale = GetFallScale();
				}
				else {
					ProcessDecelTimer(m_swPostMoveDecelTimerForward, eSourceMovement, MovableCamera.DIRECTION.FORWARD, ref dAccelDecelScale);
					ProcessDecelTimer(m_swPostMoveDecelTimerBackward, eSourceMovement, MovableCamera.DIRECTION.BACK, ref dAccelDecelScale);
					ProcessDecelTimer(m_swPostMoveDecelTimerLeft, eSourceMovement, MovableCamera.DIRECTION.LEFT, ref dAccelDecelScale);
					ProcessDecelTimer(m_swPostMoveDecelTimerRight, eSourceMovement, MovableCamera.DIRECTION.RIGHT, ref dAccelDecelScale);
                }
				
				//m_SoundManager.PlayEffect(SoundManager.EEffects.FOOTSTEP1);
				// need to learn how to know when a sound has stopped to do these footstep sounds correctly

				m_cam.MoveToPosition(d3MoveTo, !AcceleratingOrDecelerating(eSourceMovement) && !AreFalling(), dAccelDecelScale);

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

		private double GetSlowDownScale(Stopwatch sw)
		{
            double dScale = 1.0;

			double dRatio = (double)sw.ElapsedMilliseconds / m_dDecelTimeMS; // 250ms comes from
			dScale = (1.0 - dRatio) * 1.5; // took 1.5 from normal move speed scale. need to get it from last user movement speed somehow

			LOGGER.Debug("Slowdown scale is : " + dScale + " with elapsed milli being " + sw.ElapsedMilliseconds);

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

        override public bool MoveForward()
		{
			if (AreFalling()) return false;

			if (m_swPostMoveDecelTimerBackward.IsRunning) m_swPostMoveDecelTimerBackward.Reset();

			m_cam.LookStraight();
			int nMoveAttemptCount = 0;
			bool bSuccess = TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, true, MovableCamera.DIRECTION.FORWARD);
			m_cam.RestoreOrientation();
			return bSuccess;
		}

		override public void MoveBackward() 
		{
			if (AreFalling()) return;

			if (m_swPostMoveDecelTimerForward.IsRunning) m_swPostMoveDecelTimerForward.Reset();

			m_cam.TurnBack();
            int nMoveAttemptCount = 0;
            TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, true, MovableCamera.DIRECTION.BACK);
			m_cam.RestoreOrientation();
		}

		override public void MoveLeft()
		{
			if (AreFalling()) return;

			if (m_swPostMoveDecelTimerRight.IsRunning) m_swPostMoveDecelTimerRight.Reset();

			m_cam.TurnLeft();
			int nMoveAttemptCount = 0;
			TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, true, MovableCamera.DIRECTION.LEFT);
			m_cam.RestoreOrientation();
		}

		override public void MoveRight()
		{
			if (AreFalling()) return;

			if (m_swPostMoveDecelTimerLeft.IsRunning) m_swPostMoveDecelTimerLeft.Reset();

			m_cam.TurnRight();
			int nMoveAttemptCount = 0;
			TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, true, MovableCamera.DIRECTION.RIGHT);
			m_cam.RestoreOrientation();
		}

		private bool AreFalling()
		{ 
			return m_bFalling;
		}

		private bool AcceleratingOrDecelerating(MovableCamera.DIRECTION eSourceMovement)
		{
			return (m_swPostMoveDecelTimerRight.IsRunning && eSourceMovement == MovableCamera.DIRECTION.RIGHT) ||
				(m_swPostMoveDecelTimerForward.IsRunning && eSourceMovement == MovableCamera.DIRECTION.FORWARD) ||
				(m_swPostMoveDecelTimerBackward.IsRunning && eSourceMovement == MovableCamera.DIRECTION.BACK) ||
				(m_swPostMoveDecelTimerLeft.IsRunning && eSourceMovement == MovableCamera.DIRECTION.LEFT);
		}

		override public void GameTick(MoveStates stoppedMovingStates) 
		{
			if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.FORWARD))
			{
				m_swPostMoveDecelTimerForward.Start();
			}
			if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.RIGHT))
			{
                m_swPostMoveDecelTimerRight.Start();
            }
			if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.LEFT))
			{
                m_swPostMoveDecelTimerLeft.Start();
            }
			if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.BACK))
			{
                m_swPostMoveDecelTimerBackward.Start();
            }

            if (m_swPostMoveDecelTimerForward.IsRunning)
			{
				// player stopped moving and they are decelerating to a stop
				if (m_swPostMoveDecelTimerForward.ElapsedMilliseconds >= (long)m_dDecelTimeMS)
				{
					m_swPostMoveDecelTimerForward.Reset();
				}
				else
				{
					MoveForward();				
                }
			}

            if (m_swPostMoveDecelTimerBackward.IsRunning)
            {
                if (m_swPostMoveDecelTimerBackward.ElapsedMilliseconds >= (long)m_dDecelTimeMS)
                {
					m_swPostMoveDecelTimerBackward.Reset();
                }
                else
                {
                    MoveBackward();
                }
            }

            if (m_swPostMoveDecelTimerLeft.IsRunning)
            {
                if (m_swPostMoveDecelTimerLeft.ElapsedMilliseconds >= (long)m_dDecelTimeMS)
                {
					m_swPostMoveDecelTimerLeft.Reset();
                }
                else
                {
                    MoveLeft();
                }
            }

            if (m_swPostMoveDecelTimerRight.IsRunning)
            {
                if (m_swPostMoveDecelTimerRight.ElapsedMilliseconds >= (long)m_dDecelTimeMS)
                {
					m_swPostMoveDecelTimerRight.Reset();
                }
                else
                {
					MoveRight();
                }
            }           
        }

		override public void Fall()
        {
            m_cam.TurnDown();
            int nMoveAttemptCount = 0;
            bool bCanMove = TryToMoveForward(m_cam.GetLookAtNew, m_cam.Position, ref nMoveAttemptCount, false, MovableCamera.DIRECTION.DOWN);
			if (bCanMove)
			{
				if (!m_bFalling)
				{
					Debug.Assert(!m_swFallTimer.IsRunning);
					m_bFalling = true;					
					m_swFallTimer.Start();
				}
			}
			else
			{
				if (m_bFalling)
				{
					Debug.Assert(m_swFallTimer.IsRunning);
					m_bFalling = false;
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
