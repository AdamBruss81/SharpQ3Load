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
using utilities;
using OpenTK.Graphics.OpenGL;

namespace engine
{
	/// <summary>
	/// Stationary view for creatively looking at map
	/// </summary>
	public class Spectator:Engine
	{
		private const int m_PosDuration = 1500;

		private int m_CamPosCounter;
		private int m_ViewPointIndex;
		private int m_NumShowScenesPerCamPosition;
		private List<Viewpoint> m_CamPositions;

		public Spectator(Engine driver):base(driver)
		{
			m_ViewPointIndex = 0;

			m_NumShowScenesPerCamPosition = m_PosDuration / 8;

			m_CamPositions = m_lStaticFigList[0].SpawnPoints;

			RepositionMovableCamera();
		}

		public override void MouseMove(MouseEventArgs e, ref bool bPostOpen)
		{
			MoveMovableCameraViaMouse(e, ref bPostOpen);
		}

		override public string GetGameMode()
		{
			return "SPECTATOR";
		}

		protected override void Draw()
		{
			base.Draw();

			if (STATE.AllowPrinting)
			{
				sgl.PUSHATT(AttribMask.CurrentBit | AttribMask.TextureBit);
				GL.Color3(0.0, 1.0, 0.0);
				GL.Disable(EnableCap.Texture2D);

				m_fonter.PrintTopLeft(m_CamPositions[m_ViewPointIndex].Name, m_GControl.Width, m_GControl.Height, 0);

				sgl.POPATT();
			}
		}

		override public EEngineType GetClass()
		{
			return EEngineType.SPECTATOR;
		}

		private void RepositionMovableCamera()
		{
			if (m_CamPositions.Count > 0)
			{
				m_cam = new MovableCamera(m_CamPositions[m_ViewPointIndex].Position[0], m_CamPositions[m_ViewPointIndex].Position[1],
								   m_CamPositions[m_ViewPointIndex].Position[2] + JUMP_OFFSET,
								   Math.PI / 2, m_CamPositions[m_ViewPointIndex].Orientation[3], m_GControl as IGLControl);

				m_CamPosCounter = 0;
			}
		}

		public override void showScene(Keys keys)
		{
			m_CamPosCounter++;

			if (m_CamPosCounter == m_NumShowScenesPerCamPosition)
			{
				if (m_ViewPointIndex == m_CamPositions.Count - 1)
				{
					m_ViewPointIndex = 0;
				}
				else
				{
					m_ViewPointIndex++;
				}

				RepositionMovableCamera();
			}

			base.showScene(keys);
		}

		public override void RightMouseDown()
		{
			if (m_ViewPointIndex == m_CamPositions.Count - 1)
			{
				m_ViewPointIndex = 0;
			}
			else
			{
				m_ViewPointIndex++;
			}

			RepositionMovableCamera();
		}

		public override void LeftMouseDown()
		{
			if (m_ViewPointIndex == 0)
			{
				m_ViewPointIndex = m_CamPositions.Count - 1;
			}
			else
			{
				m_ViewPointIndex--;
			}

			RepositionMovableCamera();
		}
	}
}
