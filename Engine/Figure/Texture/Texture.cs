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
using Tao.OpenGl;
using Tao.Platform.Windows;
using System.Drawing;
using System.IO;

namespace engine
{
   /// <summary>
   /// OpenGL m_pnTextures
   /// </summary>
    public class Texture
    {
        private uint[] m_pnTextures;
		private int m_nBindTexture;
		private string m_sInternalZipPath;

		private const string g_sTexturePrefix = "Texture";
		private const string g_sLightmapPrefix = "Texture/maps";
		private const string g_sDefaultTexture = g_sTexturePrefix + "/textures/base_floor/clang_floor.jpg";

		private Zipper m_zipper = new Zipper();
		MapInfo m_map = null;

        // Form the complete path of the m_pnTextures filename and create the m_pnTextures with the filename
        // If the m_pnTextures cannot be found then create a m_pnTextures with a default file
        public Texture( string sInternalZipPath, MapInfo map )
        {
			m_sInternalZipPath = sInternalZipPath;
			m_map = map;
        }

		public void Delete()
		{
			Gl.glDeleteTextures(1, m_pnTextures);
			Gl.glDeleteLists(m_nBindTexture, 1);
		}

        public void SetTexture(string sURL)
        {
			bool bExtracted = true;

			string sFullPath = m_zipper.ExtractSoundTextureOther(sURL);
			if(!File.Exists(sFullPath)) {
				if (!m_map.ExtractedFromZip)
				{
					string sMapDir = Path.GetDirectoryName(m_map.GetMapPathOnDisk);
					string sFileName = Path.GetFileName(sURL);
					string sTexturePath = Path.Combine(sMapDir, sFileName);
					string sFullPathLocal = sTexturePath;
					if (!File.Exists(sFullPathLocal))
					{
						LOGGER.Warning("Missing texture " + sURL + ".\n   Looked here " + sFullPath + " and\n   here " + sFullPathLocal);
						sFullPath = m_zipper.ExtractSoundTextureOther(g_sDefaultTexture);
					}
					else
					{
						bExtracted = false;
						sFullPath = sFullPathLocal;
					}
				}
				else
				{
					LOGGER.Warning("Missing texture " + sURL + ". Looked here " + sFullPath);
					sFullPath = m_zipper.ExtractSoundTextureOther(g_sDefaultTexture);
				}
			}

			LOGGER.Debug("Set texture to " + sFullPath);

			Bitmap image = new Bitmap(sFullPath);
            image.RotateFlip(RotateFlipType.Rotate180FlipX);

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            System.Drawing.Imaging.BitmapData bitmapdata;
            bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Gl.glGenTextures(1, m_pnTextures);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, m_pnTextures[0]);

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            
            Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, 3, image.Width,
                image.Height, Gl.GL_BGR_EXT, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

            image.UnlockBits(bitmapdata);
            image.Dispose();

			if(bExtracted) 
				File.Delete(sFullPath);
        }

        public void bindMe()
        {
			Gl.glCallList(m_nBindTexture);
        }

		public void InitializeLists()
		{
			m_pnTextures = new uint[1];
			string[] tokens = m_sInternalZipPath.Split(new char[] { '.' }); // seperate filename around period
			string filetype = tokens[tokens.Length - 1]; // get the file extension
			string sFullTexturePath;

			// Try for creating lightmap m_pnTextures
			if (filetype == "png")
			{
				sFullTexturePath = g_sLightmapPrefix + "/" + m_sInternalZipPath;
				SetTexture(sFullTexturePath);
			}
			else
			{
				if (filetype == "tga")
				{
					m_sInternalZipPath = tokens[0] + ".jpg";
				}

				sFullTexturePath = g_sTexturePrefix + "/" + m_sInternalZipPath;
				SetTexture(sFullTexturePath);
			}

			m_nBindTexture = Gl.glGenLists(1);
			Gl.glNewList(m_nBindTexture, Gl.GL_COMPILE);
				Gl.glBindTexture(Gl.GL_TEXTURE_2D, m_pnTextures[0]);
			Gl.glEndList();
		}
    }
}