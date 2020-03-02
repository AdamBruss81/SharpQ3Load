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
using utilities;

namespace engine
{
	public abstract class VRMLProjectile : Figure
	{
		protected Zipper m_zipper = new Zipper();

		public VRMLProjectile() { }

		public VRMLProjectile(int figureID, MovableCamera cam, VRMLProjectile projectile) : base(figureID)
		{
			this.m_lShapes = projectile.m_lShapes;
			this.m_lMapFaceReferences = projectile.m_lMapFaceReferences;

			Setup(cam);
		}

		abstract public void Setup(MovableCamera cam);
	}
}
