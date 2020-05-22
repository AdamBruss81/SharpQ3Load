using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace utilities
{
	public class MoveStates
	{
		List<bool> m_lStates = new List<bool> { false, false, false, false, false, false, false, false, false };

		public List<bool> GetStates() { return m_lStates; }

		public bool AnyTrue()
		{
			for(int i = 0; i < m_lStates.Count; i++)
			{
				if (m_lStates[i]) return true;
			}
			return false;
		}

		public bool OnlyState(MovableCamera.DIRECTION dir)
		{
			bool bReturn = false;
			bool b = GetState(dir);
			if (b)
			{
				bReturn = true;
				for(int i = 0; i < m_lStates.Count; i++)
				{
					if (i == GetStateIndex(dir)) continue;

					if(m_lStates[i])
					{
						bReturn = false;
						break;
					}
				}
			}
			return bReturn;
		}

		public MovableCamera.DIRECTION GetRelevant()
		{
			if(GetState(MovableCamera.DIRECTION.BACK_LEFT)) return MovableCamera.DIRECTION.BACK_LEFT;
			if (GetState(MovableCamera.DIRECTION.BACK_RIGHT)) return MovableCamera.DIRECTION.BACK_RIGHT;
			if (GetState(MovableCamera.DIRECTION.FORWARD_LEFT)) return MovableCamera.DIRECTION.FORWARD_LEFT;
			if (GetState(MovableCamera.DIRECTION.FORWARD_RIGHT)) return MovableCamera.DIRECTION.FORWARD_RIGHT;
			if (GetState(MovableCamera.DIRECTION.LEFT)) return MovableCamera.DIRECTION.LEFT;
			if (GetState(MovableCamera.DIRECTION.RIGHT)) return MovableCamera.DIRECTION.RIGHT;
			if (GetState(MovableCamera.DIRECTION.FORWARD)) return MovableCamera.DIRECTION.FORWARD;
			if (GetState(MovableCamera.DIRECTION.BACK)) return MovableCamera.DIRECTION.BACK;

			return MovableCamera.DIRECTION.NONE;
		}

        private MovableCamera.DIRECTION GetDirFromIndex(int nIndex)
        {
            switch (nIndex)
            {
				case 0: return MovableCamera.DIRECTION.FORWARD;
				case 1: return MovableCamera.DIRECTION.BACK;
				case 2: return MovableCamera.DIRECTION.LEFT;
				case 3: return MovableCamera.DIRECTION.RIGHT;
				case 4: return MovableCamera.DIRECTION.FORWARD_LEFT;
				case 5: return MovableCamera.DIRECTION.FORWARD_RIGHT;
				case 6: return MovableCamera.DIRECTION.BACK_LEFT;
				case 7: return MovableCamera.DIRECTION.BACK_RIGHT;
			}

            throw new Exception("Invalid direction");
        }

        private int GetStateIndex(MovableCamera.DIRECTION dir)
		{
			switch(dir)
			{
				case MovableCamera.DIRECTION.FORWARD: return 0;
                case MovableCamera.DIRECTION.BACK: return 1;
				case MovableCamera.DIRECTION.LEFT: return 2;
				case MovableCamera.DIRECTION.RIGHT: return 3;
                case MovableCamera.DIRECTION.FORWARD_LEFT: return 4;
                case MovableCamera.DIRECTION.FORWARD_RIGHT: return 5;
                case MovableCamera.DIRECTION.BACK_LEFT: return 6;
                case MovableCamera.DIRECTION.BACK_RIGHT: return 7;
				case MovableCamera.DIRECTION.UP: return 8;
            }

			throw new Exception("Invalid direction");
		}

		public void SetState(MovableCamera.DIRECTION e, bool b)
		{
			m_lStates[GetStateIndex(e)] = b;
		}

		public bool GetState(MovableCamera.DIRECTION e)
		{
			return m_lStates[GetStateIndex(e)];
		}

		public MovableCamera.DIRECTION GetFirstTrue()
		{ 
			for(int i = 0; i < m_lStates.Count; i++)
			{
				if(m_lStates[i])
				{
					return GetDirFromIndex(i);
				}
			}
			return MovableCamera.DIRECTION.NONE;
		}

		public void Clear()
		{
			for(int i = 0; i < m_lStates.Count; i++)
			{
				m_lStates[i] = false;
			}
		}
	}

	public class MovableCamera
	{
		public enum DIRECTION { FORWARD, RIGHT, LEFT, BACK, FORWARD_RIGHT, FORWARD_LEFT, BACK_LEFT, BACK_RIGHT, UP, DOWN, NONE };

		private double m_dPhi;   
		private double m_dTheta;

		private int m_nMouseX;
		private int m_nMouseY;
		private const int MOUSE_TURN_SENSITIVITY = 1500; // increase to make it slower

		private double m_rho;

		private D3Vect m_d3LookAt = new D3Vect();
		private IGLControl m_Window = null;

		private Stack<double> m_UtilityStack = new Stack<double>();

		private const float PHI_EPSILON = .0001F;

		private D3Vect m_vCamPos;

		public MovableCamera(double XPos, double YPos, double ZPos, double phi, double theta, IGLControl window)
		{
			m_rho = 0.1; // this is the global speed
			// the problem with increasing this to increase speed is because it changes your collision detection.
			// increasing it will effectively make you hit walls earlier appearing that you are farther away when
			// hitting walls
			// .1 feels like quake 3
			// then I'm left with scaling the vector a little to adjust speed

			m_vCamPos = new D3Vect(XPos, YPos, ZPos);
			this.m_dPhi = phi;
			this.m_dTheta = theta;
			m_Window = window;
		}

		public double GetStandardMovementScale() 
		{
			//return 1.5;
			//return 5.0;
			return 1.5;
		}

		public D3Vect GetVector(DIRECTION dir)
		{
			switch (dir)
			{
				case DIRECTION.FORWARD:
					{
						return GetLookAtNew - m_vCamPos;
					}
				case DIRECTION.BACK:
					{
						PushOrientation();
						m_dTheta = Math.PI + m_dTheta;
						m_dPhi = Math.PI - m_dPhi;
						D3Vect vec = GetLookAtNew - m_vCamPos;
						RestoreOrientation();
						return vec;
					}
				case DIRECTION.LEFT:
					{
						PushOrientation();
						m_dTheta = m_dTheta + Math.PI / 2;
						m_dPhi = Math.PI / 2;
						D3Vect vec = GetLookAtNew - m_vCamPos;
						RestoreOrientation();
						return vec;
					}
				case DIRECTION.RIGHT:
					{
						PushOrientation();
						m_dTheta = m_dTheta - Math.PI / 2;
						m_dPhi = Math.PI / 2;
						D3Vect vec = GetLookAtNew - m_vCamPos;
						RestoreOrientation();
						return vec;
					}
				case DIRECTION.UP:
					{
						PushOrientation();
						m_dPhi = m_dPhi - Math.PI / 2;
						D3Vect vec = GetLookAtNew - m_vCamPos;
						RestoreOrientation();
						return vec;
					}
				case DIRECTION.DOWN:
					{
						PushOrientation();
						m_dPhi = m_dPhi + Math.PI / 2;
						D3Vect vec = GetLookAtNew - m_vCamPos;
						RestoreOrientation();
						return vec;
					}
				default:
					throw new Exception("Invalid camera direction vector requested");
			}
		}

		public D3Vect Position
		{
			get { return m_vCamPos; }
			set { m_vCamPos = value; }
		}

		private void CalculateLookAt()
		{
			m_d3LookAt[0] = m_rho * Math.Cos(m_dTheta) * Math.Sin(m_dPhi);
			m_d3LookAt[1] = m_rho * Math.Sin(m_dTheta) * Math.Sin(m_dPhi);
			m_d3LookAt[2] = m_rho * Math.Cos(m_dPhi);
		}

		public void GetLookAtRef(D3Vect d3LookAt)
		{
			CalculateLookAt();

			d3LookAt[0] = m_vCamPos[0] + m_d3LookAt[0];
			d3LookAt[1] = m_vCamPos[1] + m_d3LookAt[1];
			d3LookAt[2] = m_vCamPos[2] + m_d3LookAt[2];
		}

		// Calculate the camera look at point and return it in a D3Vect
		public D3Vect GetLookAtNew
		{
			get
			{
				D3Vect v = new D3Vect();

				CalculateLookAt();

				v[0] = m_vCamPos[0] + m_d3LookAt[0];
				v[1] = m_vCamPos[1] + m_d3LookAt[1];
				v[2] = m_vCamPos[2] + m_d3LookAt[2];

				return v;
			}
		}

		/// <summary>
		/// Supports moving normally and moving after the user stops sending move inputs(deceleration)
		/// returns velocity of this move
		/// </summary>
		/// <param name="d3Position"></param>
		/// <param name="bAllowKeyBasedScaling"></param>
		/// <param name="dModifierScale"></param>
		public void MoveToPosition(D3Vect d3Position)
		{
			m_vCamPos += (d3Position - m_vCamPos);
		}

		// Move the camera to its look at point
		// this is used by the ghost engine.
		// player uses movetoposition
		public void MoveForward(double dMultiplier)
		{
			double dTempRHO = m_rho;

            if(dMultiplier != 1.0)
            {
                m_rho *= dMultiplier;
            }
			else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				m_rho *= 5.0;
			}
			else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
			{
				m_rho *= 0.4;
			}

			m_vCamPos = GetLookAtNew;

			m_rho = dTempRHO;
		}

		public void RestoreOrientation()
		{
			if (m_UtilityStack.Count == 0) return;

			if (m_UtilityStack.Count % 2 != 0)
				throw new Exception("Invalid stack size of " + System.Convert.ToString(m_UtilityStack.Count));

			m_dPhi = m_UtilityStack.Pop();
			m_dTheta = m_UtilityStack.Pop();
		}

		private void PushOrientation()
		{
			m_UtilityStack.Push(m_dTheta);
			m_UtilityStack.Push(m_dPhi);
		}

        public void LookStraight()
        {
			PushOrientation();
			m_dPhi = Math.PI / 2;
		}

        // Move backwards:
        // Store calculation angles m_dPhi and m_theta on a stack.
        // Adjust m_dPhi and m_theta to point camera directly behind where it was just pointing
        // Now move in that direction
        // Put m_theta and m_dPhi to previous positions looking forward
        public void TurnBack()
		{
			PushOrientation();
			m_dTheta = Math.PI + m_dTheta;
			m_dPhi = Math.PI / 2;
		}

        public void TurnBackRight()
        {
            PushOrientation();
            m_dTheta = Math.PI + m_dTheta + Math.PI / 4;
            m_dPhi = Math.PI / 2;
        }

        public void TurnBackLeft()
        {
            PushOrientation();
			m_dTheta = Math.PI + m_dTheta - Math.PI / 4;
            m_dPhi = Math.PI / 2;
        }

        // Turn left along axis
        public void TurnLeft()
		{
			PushOrientation();
			m_dTheta = m_dTheta + Math.PI / 2;
			m_dPhi = Math.PI / 2;
		}

        public void TurnLeftHalf()
        {
            PushOrientation();
            m_dTheta = m_dTheta + Math.PI / 4;
            m_dPhi = Math.PI / 2;
        }

        // Turn right along axis
        public void TurnRight()
		{
			PushOrientation();
			m_dTheta = m_dTheta - Math.PI / 2;
			m_dPhi = Math.PI / 2;
		}

        public void TurnRightHalf()
        {
            PushOrientation();
            m_dTheta = m_dTheta - Math.PI / 4;
            m_dPhi = Math.PI / 2;
        }

        public void TurnUp()
		{
			PushOrientation();
			m_dPhi = 0.0;
		}

		public void TurnDown()
		{
			PushOrientation();
			m_dPhi = Math.PI;
		}

		// Adjust m_theta
		public void changeHorizontalLookDirection(double deltaTheta)
		{
			m_dTheta = m_dTheta + deltaTheta;

			if (m_dTheta >= Math.PI * 2)
				m_dTheta -= Math.PI * 2;
			else if (m_dTheta <= -Math.PI * 2)
				m_dTheta += Math.PI * 2;
		}

		// Adjust m_dPhi
		public void changeVerticalLookDirection(double deltaPhi)
		{
			if (m_dPhi + deltaPhi < 0)
				m_dPhi = PHI_EPSILON;
			else if (m_dPhi + deltaPhi > Math.PI)
				m_dPhi = Math.PI - PHI_EPSILON;
			else
				m_dPhi = m_dPhi + deltaPhi;
		}

		public double RHO { get { return m_rho; } }

		public double PHI_RAD
		{
			get { return m_dPhi; }
			set { m_dPhi = value; }
		}

		public double THETA_RAD
		{
			get { return m_dTheta; }
			set { m_dTheta = value; }
		}

		public double PHI_DEG
		{
			get { return m_dPhi * GLB.RadToDeg; }
			set { m_dPhi = value / GLB.RadToDeg; }
		}

		public double THETA_DEG
		{
			get { return m_dTheta * GLB.RadToDeg; }
			set { m_dTheta = value / GLB.RadToDeg; }
		}

		public int MouseX
		{
			get { return m_nMouseX; }
			set { m_nMouseX = value; }
		}

		public int MouseY
		{
			get { return m_nMouseY; }
			set { m_nMouseY = value; }
		}

		public int MiddleX
		{
			get { return m_Window.Location.X + m_Window.Width / 2; }
		}

		public int MiddleY
		{
			get { return m_Window.Location.Y + m_Window.Height / 2; }
		}

		// Adjust m_dPhi and m_theta based on difference between center of window and where the current mouse position is
		// My mouse position is set from the outside.
		public void changeLookAtViaMouse()
		{
			changeHorizontalLookDirection((double)(MiddleX - (m_nMouseX + m_Window.Location.X)) / MOUSE_TURN_SENSITIVITY);
			changeVerticalLookDirection((double)((m_nMouseY + m_Window.Location.Y) - MiddleY) / MOUSE_TURN_SENSITIVITY);
		}

		public string GetCurrentStateDataString(bool bDegrees)
		{
			string sData = "";
			sData += "Position " + Position.ToString() + "\n";
			if (bDegrees)
			{
				sData += "Phi " + String.Format("{0:0}", PHI_DEG) + "\n";
				sData += "Theta " + String.Format("{0:0}", THETA_DEG) + "\n";
			}
			else
			{
				sData += "Phi " + m_dPhi.ToString() + "\n";
				sData += "Theta " + m_dTheta.ToString() + "\n";
			}
			sData += "----> " + GetVector(DIRECTION.FORWARD).ToString();
			return sData;
		}
	}
}
