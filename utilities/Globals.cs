using Tao.OpenGl;
using System.Drawing;

namespace utilities
{
	public class GLB
	{
		public static double RadToDeg { get { return 57.2957795; } }
	}

	/// <summary>
	/// OpenGL helper functions
	/// </summary>
	public class sgl
	{
		public static void PUSHATT(int mask)
		{
			Gl.glPushAttrib(mask);
		}

		public static void POPATT()
		{
			Gl.glPopAttrib();
		}

		public static void PUSHMAT()
		{
			Gl.glPushMatrix();
		}

		public static void POPMAT()
		{
			Gl.glPopMatrix();
		}
	}

	public class ImageHelper
	{
		public static Bitmap GetImage(Size dims)
		{
			Bitmap bmp = new Bitmap(dims.Width, dims.Height);
			System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, dims.Width, dims.Height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			Gl.glReadPixels(0, 0, dims.Width, dims.Height, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return bmp;
		}
	}
}