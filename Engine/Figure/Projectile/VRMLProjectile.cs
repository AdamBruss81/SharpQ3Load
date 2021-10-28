using utilities;

namespace engine
{
	public abstract class VRMLProjectile : Figure
	{
		protected Zipper m_zipper = new Zipper();

		public VRMLProjectile() { }

		public VRMLProjectile(MovableCamera cam, VRMLProjectile projectile) : base()
		{
			this.m_lShapes = projectile.m_lShapes;
			this.m_lMapFaceReferences = projectile.m_lMapFaceReferences;

			Setup(cam);
		}

		abstract public void Setup(MovableCamera cam);
	}	
}
