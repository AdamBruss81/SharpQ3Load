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
using Tao.Platform.Windows;
using Tao.OpenGl;
using utilities;

namespace engine
{
	/// <summary>
	/// No clipping mode flyer
	/// </summary>
	public class Ghost:Engine
	{
		/// <summary>
		/// Copy driver to this
		/// </summary>
		/// <param m_DisplayName="driver">driver to copy</param>
		public Ghost(Engine driver):base(driver){}

		override public EEngineType GetClass()
		{
			return EEngineType.GHOST;
		}

		override public void LoadMap(MapInfo map)
		{
			base.LoadMap(map);

			if (m_lStaticFigList[0].GetNumViewPoints > 0)
				SetMovableCameraToRandomViewPoint();
			else
			{
				m_cam.Position = new D3Vect(10, 0, 10);
				m_cam.PHI_RAD = Math.PI / 2;
				m_cam.THETA_RAD = 0;
			}
		}

		override public string GetGameMode()
		{
			return "GHOST";
		}

		override protected void Draw(int nNumFaceCount)
		{
			base.Draw(nNumFaceCount);

			if (STATE.AllowPrinting)
			{
				sgl.PUSHATT(Gl.GL_CURRENT_BIT | Gl.GL_TEXTURE_BIT);

				Gl.glColor3d(0.0, 1.0, 1.0);
				Gl.glDisable(Gl.GL_TEXTURE_2D);

				m_fonter.PrintTopLeft(m_cam.GetCurrentStateDataString(true), m_GControl.Width, m_GControl.Height, 0);

				sgl.POPATT();
			}
		}

		override public bool MoveForward()
		{
			m_cam.MoveForward();
			return true;
		}

		override public void MoveBackward()
		{
			m_cam.TurnBack();
			m_cam.MoveForward();
			m_cam.RestoreOrientation();
		}

		override public void MoveLeft()
		{
			m_cam.TurnLeft();
			m_cam.MoveForward();
			m_cam.RestoreOrientation();
		}

		override public void MoveRight()
		{
			m_cam.TurnRight();
			m_cam.MoveForward();
			m_cam.RestoreOrientation();
		}

		public override void MoveUp()
		{
			m_cam.TurnUp();
			m_cam.MoveForward();
			m_cam.RestoreOrientation();
		}

		public override void MoveDown()
		{
			m_cam.TurnDown();
			m_cam.MoveForward();
			m_cam.RestoreOrientation();
		}

		public override void MouseMove(MouseEventArgs e, ref bool bPostOpen)
		{
			MoveMovableCameraViaMouse(e, ref bPostOpen);
		}
	}
}
