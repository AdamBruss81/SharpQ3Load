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
using System.IO;
using System;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace engine
{
   /// <summary>
   /// OpenGL m_pnTextures
   /// </summary>
    public class Texture
    {
        private uint[] m_pnTextures;
		private string m_sInternalZipPath;
        bool m_bShouldBeTGA = false;
        bool m_bClamp = false;
        bool m_bWideTexture = false; // if image is wider than high
        bool m_bTGA = false;
        
        public enum EFileType { PNG, TGA, JPG };

        // Form the complete path of the m_pnTextures filename and create the m_pnTextures with the filename
        // If the m_pnTextures cannot be found then create a m_pnTextures with a default file
        public Texture( string sInternalZipPath )
        {
            m_pnTextures = new uint[1];
            m_sInternalZipPath = sInternalZipPath;
        }

        public string GetPath() { return m_sInternalZipPath; }

		public void Delete()
		{
            if(m_pnTextures != null)
			    GL.DeleteTextures(1, m_pnTextures);
		}  

        public static float[] GetAverageColor(string sPath)
        {
            float[] fCol = { 0f, 0f, 0f, 0f };
            System.Drawing.Bitmap bm = GetBitmapFromImageFile(sPath);
            int nCounter = 0;
            for(int i = 0; i < bm.Width; i++)
            {
                for(int j = 0; j < bm.Height; j++)
                {
                    System.Drawing.Color pcol = bm.GetPixel(i, j);
                    if(pcol.R != 0 || pcol.G != 0 || pcol.B != 0)
                    {
                        nCounter++;
                        fCol[0] += pcol.R;
                        fCol[1] += pcol.G;
                        fCol[2] += pcol.B;
                        if(Path.GetExtension(sPath) == ".tga")
                            fCol[3] += pcol.A;
                        else
                        {
                            //sb.AppendLine("float texelA" + sIndex + " = sqrt(0.299 * pow(" + sTexel + ".r, 2) + 0.587 * pow(" + sTexel + ".g, 2) + 0.114 * pow(" + sTexel + ".b, 2));");
                            fCol[3] += Convert.ToSingle(Math.Sqrt(0.299f * Math.Pow((float)pcol.R / 255f, 2) + 0.587f * Math.Pow((float)pcol.G / 255f, 2) + 0.114f * Math.Pow((float)pcol.B / 255f, 2)));
                        }
                    }
                }
            }
            fCol[0] /= nCounter;
            fCol[1] /= nCounter;
            fCol[2] /= nCounter;
            if(Path.GetExtension(sPath) != ".tga")
            {
                fCol[3] *= 255;
            }
            fCol[3] /= nCounter;

            return fCol;
        }

        public void SetShouldBeTGA(bool b) { m_bShouldBeTGA = b; }
        public void SetClamp(bool b) { m_bClamp = b; }
        public bool GetShouldBeTGA() { return m_bShouldBeTGA; }
        public bool IsTGA()
        {
            return m_bTGA;
        }
        public bool GetWide() { return m_bWideTexture; }

        private static System.Drawing.Bitmap GetBitmapFromImageFile(string sFullPath)
        {
            System.Drawing.Bitmap image;

            if (Path.GetExtension(sFullPath) == ".tga")
            {
                IImageFormat format;
                using (var image2 = Image.Load(sFullPath, out format))
                {
                    MemoryStream memStr = new MemoryStream();
                    image2.SaveAsPng(memStr);
                    image = new System.Drawing.Bitmap(memStr);
                    memStr.Dispose();
                }
            }
            else
                image = new System.Drawing.Bitmap(sFullPath);

            return image;
        }

        public void SetTexture(string sFullPath)
        {
            if (string.IsNullOrEmpty(sFullPath)) return; // for example fog

            if(Path.GetExtension(sFullPath) == ".tga") m_bTGA = true;

            LOGGER.Debug("Set texture to " + sFullPath);

            System.Drawing.Bitmap image = GetBitmapFromImageFile(sFullPath);

            if (image.Width > image.Height) m_bWideTexture = true;

            image.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);

            System.Drawing.Imaging.BitmapData bitmapdata;

            bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.GenTextures(1, m_pnTextures);
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            if(m_bClamp)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            }
       
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width,
                image.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmapdata.Scan0);

            string sErrors = "";
            int nRet = utilities.ShaderHelper.GetOpenGLErrors(ref sErrors);
            if(nRet != 0)
            {
                LOGGER.Error("Texture open gl errors: " + sErrors);
            }
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            image.UnlockBits(bitmapdata);
            image.Dispose();

			//File.Delete(sFullPath);
        }

        public void bindMeRaw()
        {
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);
        }
    }
}