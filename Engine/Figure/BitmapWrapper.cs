using System;
using OpenTK.Graphics.OpenGL;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace engine
{
    class BitmapWrapper
    {
        private System.Drawing.Bitmap m_bitMap = null;
        private System.Drawing.Imaging.BitmapData m_bitmapData = null;
        bool m_bDeleted = false;
        string m_sFullPath = "";
        bool m_bTGA = false;
        bool m_bWideTexture = false; // if image is wider than high

        public BitmapWrapper(string sFullPath)
        {
            m_sFullPath = sFullPath;
        }

        public void Delete()
        {
            if (m_bDeleted) throw new Exception("Already deleted bitmap with path: " + m_sFullPath);
            m_bDeleted = true;
            if (m_bitMap != null)
            {
                if (m_bitmapData == null) throw new Exception("bitmap data is null but should be valid");

                m_bitMap.UnlockBits(m_bitmapData);
                m_bitMap.Dispose();
            }
        }

        public bool GetIsTGA() { return m_bTGA; }
        public bool GetIsWide() { return m_bWideTexture; }

        public void TexImage2d()
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, m_bitMap.Width,
                m_bitMap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, m_bitmapData.Scan0);
        }

        public void ReadIntoBitmapForTexture(bool bShouldBeTGA, Q3Shader shader)
        {       
            if (string.IsNullOrEmpty(m_sFullPath)) return; // for example fog

            if (Path.GetExtension(m_sFullPath) == ".tga") m_bTGA = true;

            SetBitmapFromImageFile(bShouldBeTGA, shader);

            if (m_bitMap.Width > m_bitMap.Height) m_bWideTexture = true;

            m_bitMap.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, m_bitMap.Width, m_bitMap.Height);

            m_bitmapData = m_bitMap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        private void AddAlphaToImage(Q3Shader shader)
        {
            System.Drawing.Bitmap imageWithA = new System.Drawing.Bitmap(m_bitMap.Width, m_bitMap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // add alpha to image
            // loop bits in image and set their alpha value based on rgb values of bit

            for (int i = 0; i < m_bitMap.Width; i++)
            {
                for (int j = 0; j < m_bitMap.Height; j++)
                {
                    System.Drawing.Color pcol = m_bitMap.GetPixel(i, j);
                    System.Diagnostics.Debug.Assert(pcol.A == 255);
                    float fAlpha = Texture.CalculateAlphaNormalized(pcol, shader);
                    System.Drawing.Color tempCol = System.Drawing.Color.FromArgb((int)(fAlpha * 255f), pcol.R, pcol.G, pcol.B);
                    imageWithA.SetPixel(i, j, tempCol);
                }
            }

            m_bitMap.Dispose();
            m_bitMap = null;
            m_bitMap = imageWithA;
        }

        public bool GetHasAnyTransparency()
        {
            for (int i = 0; i < m_bitMap.Width; i++)
            {
                for (int j = 0; j < m_bitMap.Height; j++)
                {
                    System.Drawing.Color pcol = m_bitMap.GetPixel(i, j);
                    if (pcol.A != 255) return true;
                }
            }
            return false;
        }

        public float[] GetAverageColor255()
        {
            float[] fCol = { 0f, 0f, 0f, 0f };

            float fCounter = 0;
            for (int i = 0; i < m_bitMap.Width; i++)
            {
                for (int j = 0; j < m_bitMap.Height; j++)
                {
                    System.Drawing.Color pcol = m_bitMap.GetPixel(i, j);
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

        public void SetBitmapFromImageFile(bool bShouldBeTGA, Q3Shader shader)
        {
            if (m_bitMap != null) throw new Exception("Bitmap already allocated");

            if (Path.GetExtension(m_sFullPath) == ".tga")
            { // already tga
                IImageFormat format;

                GameGlobals.m_BitmapInitMutex.WaitOne();

                using (var image2 = Image.Load(m_sFullPath, out format))
                {
                    GameGlobals.m_BitmapInitMutex.ReleaseMutex();

                    MemoryStream memStr = new MemoryStream();
                    image2.SaveAsPng(memStr);                    
                    m_bitMap = new System.Drawing.Bitmap(memStr);

                    if (m_sFullPath.Contains("pjgrate2")) // only real tga in the game that converts incorrectly from tga to png to bmp so add alpha manually
                    {
                        AddAlphaToImage(shader);
                    }
                    else if(shader.GetTrans() && !GetHasAnyTransparency())
                    {                      
                        AddAlphaToImage(shader); // some tgas have no transparency but should. i don't understand these types of images so add alpha myself.                 
                    }

                    memStr.Dispose();
                }                
            }
            else
            {
                GameGlobals.m_BitmapInitMutex.WaitOne();
                m_bitMap = new System.Drawing.Bitmap(m_sFullPath);
                GameGlobals.m_BitmapInitMutex.ReleaseMutex();

                if ((bShouldBeTGA && !Texture.SpecialTexture(m_sFullPath)))
                {
                    System.Diagnostics.Debug.Assert(Path.GetExtension(m_sFullPath) == ".jpg");

                    AddAlphaToImage(shader);

                    if (m_sFullPath.Contains("sfx/beam") || m_sFullPath.Contains("spotlamp/beam"))
                    {
                        BrightUpBitmap(Texture.GetBeamColor(m_sFullPath));
                    }
                    else if(m_sFullPath.Contains("lamps/flare03"))
                    {
                        BrightUpBitmap(System.Drawing.Color.White);
                    }
                }
            }
        }

        private void BrightUpBitmap(System.Drawing.Color c)
        {
            for (int i = 0; i < m_bitMap.Width; i++)
            {
                for (int j = 0; j < m_bitMap.Height; j++)
                {
                    m_bitMap.SetPixel(i, j, System.Drawing.Color.FromArgb(m_bitMap.GetPixel(i, j).A, c.R, c.G, c.B));
                }
            }
        }
    }
}
