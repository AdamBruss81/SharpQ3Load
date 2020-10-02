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
        
        bool m_bOpenGLDefined = false;

        BitmapWrapper m_pWrapper = null;

        public enum EFileType { PNG, TGA, JPG };

        // Form the complete path of the m_pnTextures filename and create the m_pnTextures with the filename
        // If the m_pnTextures cannot be found then create a m_pnTextures with a default file
        public Texture( string sInternalZipPath, string sFullPath )
        {
            m_pnTextures = new uint[1];
            m_sInternalZipPath = sInternalZipPath;
            m_pWrapper = new BitmapWrapper(sFullPath);
        }

        public Texture(string sInternalZipPath)
        {
            m_pnTextures = new uint[1];
            m_sInternalZipPath = sInternalZipPath;
        }

        public bool Initialized()
        {
            return m_pWrapper != null;
        }

        public void SetFullPath(string s)
        {
            if (m_pWrapper != null) throw new Exception("Illegal allocation of bitmap wrapper");

            m_pWrapper = new BitmapWrapper(s);
        }

        public string GetPath() { return m_sInternalZipPath; }

        public void Delete()
		{
            if (m_pWrapper != null)
            {
                m_pWrapper.Delete();
                m_pWrapper = null;
            }

            if (m_pnTextures != null)
			    GL.DeleteTextures(1, m_pnTextures);
		}

        public bool Deleted() { return m_pWrapper == null; }

        public static float CalculateAlphaNormalized(System.Drawing.Color pcol, string sShaderName)
        {
            float aVal = Convert.ToSingle(Math.Sqrt(0.299f * Math.Pow((float)pcol.R / 255f, 2) + 0.587f * Math.Pow((float)pcol.G / 255f, 2) + 0.114f * Math.Pow((float)pcol.B / 255f, 2)));

            // revisit if something doesn't look good. maybe glass for example in dm0?

            if (sShaderName.Contains("glass"))
            {
                aVal *= 0.5f; // make glass more transparent
            }
            else if(sShaderName.Contains("spotlamp"))
            {

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
            else if(sShaderName.Contains("beam"))
            {
                aVal *= 1.5f;
            }

            if (aVal > 1.0f) aVal = 1.0f;

            return aVal;
        }        

        public void SetShouldBeTGA(bool b) { m_bShouldBeTGA = b; }
        public void SetClamp(bool b) { m_bClamp = b; }
        public bool GetShouldBeTGA() { return m_bShouldBeTGA; }
        public bool IsTGA()
        {
            return m_pWrapper.GetIsTGA();
        }
        public bool GetWide() { return m_pWrapper.GetIsWide(); }

        public static bool SpecialTexture(string sFullPath)
        {
            // incoming is jpg which should be tga
            // in some cases i can't tell if i should set alpha values on the image or not
            // this is frustrating

            return sFullPath.Contains("liquids/proto_gruel3");
        }                

        /// <summary>
        /// I don't know how quake3 does beams. I choose one color for the beam based on the texture files and use my derived transparency with it.
        /// It works pretty well.
        /// </summary>
        /// <param name="sFullPath"></param>
        /// <returns></returns>
        public static System.Drawing.Color GetBeamColor(string sFullPath)
        {
            if(sFullPath.Contains("sfx/beam_blue4.jpg"))
            {
                return System.Drawing.Color.FromArgb(10, 157, 255);
            }
            else if(sFullPath.Contains("sfx/beam_red.jpg"))
            {
                return System.Drawing.Color.FromArgb(255, 0, 0);
            }
            else if(sFullPath.Contains("sfx/beam_waterlight.jpg"))
            {
                return System.Drawing.Color.FromArgb(163, 255, 209);
            }
            else
            {
                return System.Drawing.Color.FromArgb(255, 255, 255);
            }
        }        

        public void GLDefineTexture()
        {
            if(m_bOpenGLDefined)
            {
                return;
            }

            GL.GenTextures(1, m_pnTextures);
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            if (m_bClamp)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            }

            m_pWrapper.TexImage2d();            

            string sErrors = "";
            int nRet = utilities.ShaderHelper.GetOpenGLErrors(ref sErrors);
            if (nRet != 0)
            {
                LOGGER.Error("Texture open gl errors: " + sErrors);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            m_bOpenGLDefined = true;
        }

        public void SetTexture(string sShaderName)
        {
            m_pWrapper.ReadIntoBitmapForTexture(m_bShouldBeTGA, sShaderName);

            /*if (string.IsNullOrEmpty(sFullPath)) return; // for example fog

            if(Path.GetExtension(sFullPath) == ".tga") m_bTGA = true;

            //LOGGER.Debug("Set texture to " + sFullPath);

            GetBitmapFromImageFile(sFullPath, bShouldBeTGA, sShaderName, m_pWrapper);

            if (m_bitmap.Width > m_bitmap.Height) m_bWideTexture = true;

            m_bitmap.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height);

            System.Drawing.Imaging.BitmapData bitmapdata;

            m_bitmapdata = m_bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);*/

			//File.Delete(sFullPath);
        }

        public void bindMeRaw()
        {
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);
        }

        // ### non thread safe static functions for review ###

        /*public static float[] GetAverageColor255(string sPath, bool bShouldBeTGA)
        {
            //GameGlobals.m_StaticTextureFcnMutex4.WaitOne();

            float[] fCol = { 0f, 0f, 0f, 0f };
            System.Drawing.Bitmap bm = GetBitmapFromImageFile(sPath, bShouldBeTGA, "");
            float fCounter = 0;
            for (int i = 0; i < bm.Width; i++)
            {
                for (int j = 0; j < bm.Height; j++)
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

            bm.Dispose();

            //GameGlobals.m_StaticTextureFcnMutex4.ReleaseMutex();

            return fCol;
        }*/

       

        /*private static System.Drawing.Bitmap GetBitmapFromImageFile(string sFullPath, bool bShouldBeTGA, string sShaderName, BitmapWrapper bw)
        {
            //GameGlobals.m_StaticTextureFcnMutex2.WaitOne();

            System.Drawing.Bitmap image;

            if (Path.GetExtension(sFullPath) == ".tga")
            { // already tga
                IImageFormat format;
                using (var image2 = Image.Load(sFullPath, out format))
                {
                    MemoryStream memStr = new MemoryStream();
                    image2.SaveAsPng(memStr);
                    image = new System.Drawing.Bitmap(memStr);

                    if (sFullPath.Contains("pjgrate2")) // only real tga in the game that converts incorrectly from tga to png to bmp so add alpha manually
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

                    if (sFullPath.Contains("sfx/beam") || sFullPath.Contains("spotlamp/beam"))
                    {
                        BrightenUpBeams(ref image, GetBeamColor(sFullPath));
                    }
                }
            }

            //GameGlobals.m_StaticTextureFcnMutex2.ReleaseMutex();

            return image;
        } */       
    }
}