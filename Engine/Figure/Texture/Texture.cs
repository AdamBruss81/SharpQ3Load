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

        private static float CalculateAlphaNormalized(System.Drawing.Color pcol, string sShaderName)
        {
            float aVal = Convert.ToSingle(Math.Sqrt(0.299f * Math.Pow((float)pcol.R / 255f, 2) + 0.587f * Math.Pow((float)pcol.G / 255f, 2) + 0.114f * Math.Pow((float)pcol.B / 255f, 2)));

            // revisit if something doesn't look good. maybe glass for example in dm0?

            if (sShaderName.Contains("glass"))
            {
                aVal *= 0.5f; // make glass more transparent
            }
            else if (sShaderName.Contains("lamp"))
            {
                // make things a lot less transparent from original value
                // in dm4 the skull lights have lightbulbs rendered in the lamp glass

                aVal *= 2.5f;
            }
            else if(sShaderName.Contains("pjgrate2"))
            {
                aVal *= 100; // special case. only remove black sections which should have alpha 0
            }
            else
            {
                //aVal *= 1.5f; // make things less transparent in general as well
            }

            if (aVal > 1.0f) aVal = 1.0f;
            return aVal;
        }

        public static float[] GetAverageColor255(string sPath, bool bShouldBeTGA)
        {
            float[] fCol = { 0f, 0f, 0f, 0f };
            System.Drawing.Bitmap bm = GetBitmapFromImageFile(sPath, bShouldBeTGA, "");
            float fCounter = 0;
            for(int i = 0; i < bm.Width; i++)
            {
                for(int j = 0; j < bm.Height; j++)
                {
                    System.Drawing.Color pcol = bm.GetPixel(i, j);
                    fCounter++;

                    fCol[0] += pcol.R;
                    fCol[1] += pcol.G;
                    fCol[2] += pcol.B;                        
                    fCol[3] += pcol.A;                        
                }
            }
            fCol[0] /= fCounter;
            fCol[1] /= fCounter;
            fCol[2] /= fCounter;
            fCol[3] /= fCounter;

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

        private static bool SpecialTexture(string sFullPath)
        {
            // incoming is jpg which should be tga
            // in some cases i can't tell if i should set alpha values on the image or not
            // this is frustrating

            return sFullPath.Contains("liquids/proto_gruel3");
        }

        private static void AddAlphaToImage(ref System.Drawing.Bitmap image, string sShaderName)
        {
            System.Drawing.Bitmap imageWithA = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // add alpha to image
            // loop bits in image and set their alpha value based on rgb values of bit

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    System.Drawing.Color pcol = image.GetPixel(i, j);
                    System.Diagnostics.Debug.Assert(pcol.A == 255);
                    float fAlpha = CalculateAlphaNormalized(pcol, sShaderName);
                    System.Drawing.Color tempCol = System.Drawing.Color.FromArgb((int)(fAlpha * 255f), pcol.R, pcol.G, pcol.B);
                    imageWithA.SetPixel(i, j, tempCol);
                }
            }

            image = imageWithA;
        }

        private static System.Drawing.Bitmap GetBitmapFromImageFile(string sFullPath, bool bShouldBeTGA, string sShaderName)
        {
            System.Drawing.Bitmap image;

            if (Path.GetExtension(sFullPath) == ".tga")
            { // already tga
                IImageFormat format;
                using (var image2 = Image.Load(sFullPath, out format))
                {
                    MemoryStream memStr = new MemoryStream();
                    image2.SaveAsPng(memStr);
                    image = new System.Drawing.Bitmap(memStr);

                    if(sFullPath.Contains("pjgrate2")) // only real tga in the game that converts incorrectly from tga to png to bmp so add alpha manually
                    {
                        AddAlphaToImage(ref image, sShaderName);
                    }

                    memStr.Dispose();
                }
            }
            else
            {                
                image = new System.Drawing.Bitmap(sFullPath);

                if (bShouldBeTGA && !SpecialTexture(sFullPath))
                {
                    System.Diagnostics.Debug.Assert(Path.GetExtension(sFullPath) == ".jpg");

                    AddAlphaToImage(ref image, sShaderName);
                }
            }

            return image;
        }

        public void SetTexture(string sFullPath, bool bShouldBeTGA, string sShaderName)
        {
            if (string.IsNullOrEmpty(sFullPath)) return; // for example fog

            if(Path.GetExtension(sFullPath) == ".tga") m_bTGA = true;

            LOGGER.Debug("Set texture to " + sFullPath);

            System.Drawing.Bitmap image = GetBitmapFromImageFile(sFullPath, bShouldBeTGA, sShaderName);

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