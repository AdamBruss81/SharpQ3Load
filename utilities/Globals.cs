using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System;

namespace utilities
{
	public class PathHelper
	{
		public static string GetUniqueTempDir()
		{
			string sTempFile = System.IO.Path.GetTempFileName();
			System.IO.File.Delete(sTempFile);
			string sTempDir = System.IO.Path.ChangeExtension(sTempFile, null);
			System.IO.Directory.CreateDirectory(sTempDir);
			return sTempDir;
		}
	}

	public class GLB
	{
		public static double RadToDeg { get { return 57.2957795; } }
		public static double DegToRad { get { return 0.01745329; } }

		public static double GoDegToRad(double d) { return d * System.Math.PI / 180.0; }
    }

    /// <summary>
    /// OpenGL helper functions
    /// </summary>
    public class sgl
	{
		public static void PUSHATT(AttribMask mask)
		{
			GL.PushAttrib(mask);
		}

		public static void POPATT()
		{
			GL.PopAttrib();
		}

		public static void PUSHMAT()
		{
			GL.PushMatrix();
		}

		public static void POPMAT()
		{
			GL.PopMatrix();
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
			GL.ReadPixels(0, 0, dims.Width, dims.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return bmp;
		}
	}
}