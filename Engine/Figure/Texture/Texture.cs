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
using System.Drawing;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace engine
{
   /// <summary>
   /// OpenGL m_pnTextures
   /// </summary>
    public class Texture
    {
        private uint[] m_pnTextures;
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

        public string GetPath() { return m_sInternalZipPath; }

		public void Delete()
		{
            if(m_pnTextures != null)
			    GL.DeleteTextures(1, m_pnTextures);
		}

        private bool FindMissingTexture(ref string sFullPath, string sURL, string sSearchString, string sReplacer)
        {
            if (sFullPath.Contains(sSearchString))
            {
                if (sReplacer.Contains("/"))
                {
                    sURL = Path.GetDirectoryName(sURL);
                    sURL = Path.GetDirectoryName(sURL) + "\\" + sReplacer;
                }
                else
                {
                    sURL = Path.GetDirectoryName(sURL) + "\\" + sReplacer;
                }
                sURL = sURL.Replace("\\", "/");
                sFullPath = m_zipper.ExtractSoundTextureOther(sURL);
                return true;
            }
            return false;
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
                    // try looking somewhere else

                    // original map that needed these fixes : 
                    // Fatal Instinct
                    bool b = FindMissingTexture(ref sFullPath, sURL, "toxicsky", "toxicsky.jpg");
                    if(!b) b = FindMissingTexture(ref sFullPath, sURL, "blacksky", "blacksky.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "killblock_i4", "killblock_i4.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "flame1side", "flame1side.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "ironcrosslt2", "ironcrosslt2.jpg");
                     
                    // Introduction
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "sphere", "spherex.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "patch10shiny", "patch10.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "mirror2", "mirror1.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "border11c", "base_trim/border11c.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "q3tourneyscreen", "q3tourney1.jpg"); // there are five of these q3tourney. they are supposed to cycle on this texture I believe
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "glass01", "glass_frame.jpg"); // not sure about this one. see how it looks.
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "proto_lightred2", "proto_lightred.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "teslacoil3", "tesla1.jpg"); // shot in the dark. wel'll see how it looks 
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "teslacoil", "tesla1b.jpg"); // shot in the dark. wel'll see how it looks. this line needs to be before previous for search purposes
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "gothic_light3_2K", "gothic_light3.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "comp3b_dark", "comp3b.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "bluemetal2_shiny_trans", "bluemetal2_shiny.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "proto_light_2k", "proto_light2.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "baslt4_1_4k", "baslt4_1.jpg");

                    // q3dm17
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "diamond2cjumppad", "bouncepad01_diamond2cTGA.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "light5", "light5.jpg");
                    if (!b) b = FindMissingTexture(ref sFullPath, sURL, "lt2", "light2.jpg");

                    if (!File.Exists(sFullPath))
                    {
                        LOGGER.Warning("Missing texture " + sURL + ". Looked here " + sFullPath);
                        sFullPath = m_zipper.ExtractSoundTextureOther(g_sDefaultTexture);
                    }
				}
			}

			LOGGER.Debug("Set texture to " + sFullPath);

			Bitmap image = new Bitmap(sFullPath);
            image.RotateFlip(RotateFlipType.Rotate180FlipX);

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            System.Drawing.Imaging.BitmapData bitmapdata;
            bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.GenTextures(1, m_pnTextures);
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, image.Width, 
                image.Height, 0, PixelFormat.Bgr, PixelType.UnsignedByte, bitmapdata.Scan0);

            string sErrors = "";
            int nRet = utilities.ShaderHelper.GetOpenGLErrors(ref sErrors);
            if(nRet != 0)
            {
                LOGGER.Error("Texture open gl errors: " + sErrors);
            }
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            image.UnlockBits(bitmapdata);
            image.Dispose();

			if(bExtracted) 
				File.Delete(sFullPath);
        }

        public void bindMeRaw()
        {
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);
        }

		public void Initialize()
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
		}
    }
}