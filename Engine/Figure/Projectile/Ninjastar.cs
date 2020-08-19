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

using utilities;
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace engine
{
	/// <summary>
	/// Flies through space on a straight line
	/// Originates in front of MovableCamera and moves along rho.
	/// </summary>
	public class NinjaStar : VRMLProjectile
	{
		private float SPEED = 0.3F;
		private double ROTATE_DELTA = 7.0;
		private double ORIENTATION_ADJUSTMENT = 10;
		private double m_SpinRotation = 0.0;
		private double m_setupRotateAmount = 0.0;

		private D3Vect BulletFlyTranslationAmt = new D3Vect();
		private D3Vect m_translateAmt = new D3Vect();

		private string StarResource = "Test/ninjastar.wrl";

		public NinjaStar() 
		{
			string sMapPath = m_zipper.ExtractMap(StarResource);
			MapInfo info = new MapInfo(StarResource, null, null, -1);
			info.GetMapPathOnDisk = sMapPath;
			Read(info);
			m_srMapReader.Close();
		}

		public NinjaStar(MovableCamera cam, NinjaStar star) : base(cam, star) { }

		public override void Setup(MovableCamera cam)
		{
			D3Vect shootVector = cam.GetLookAtNew - cam.Position;
			shootVector.normalize();
			D3Vect shiftRightVector = cam.GetVector(MovableCamera.DIRECTION.RIGHT);
			shiftRightVector.normalize();
			BulletFlyTranslationAmt = shootVector * SPEED;
			m_setupRotateAmount = (cam.THETA_RAD + Math.PI / 2) * GLB.RadToDeg + ORIENTATION_ADJUSTMENT;
			m_translateAmt = cam.GetLookAtNew + shiftRightVector;
		}

		public override void Show(Engine.EGraphicsMode mode, MovableCamera cam)
		{
			sgl.PUSHMAT();

			m_translateAmt = m_translateAmt + BulletFlyTranslationAmt;
			GL.Translate(m_translateAmt[0], m_translateAmt[1], m_translateAmt[2]);

			GL.Rotate(m_setupRotateAmount, 0, 0, 1);

			m_SpinRotation += ROTATE_DELTA;
			if (m_SpinRotation > 360) m_SpinRotation -= 360;
			GL.Rotate(m_SpinRotation, 1, 0, 0);

			base.Show(mode, cam);

			sgl.POPMAT();
		}
	}
}
