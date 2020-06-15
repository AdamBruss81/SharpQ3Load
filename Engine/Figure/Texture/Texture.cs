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
//using System.Drawing;
using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System.Collections.Generic;

namespace engine
{
   /// <summary>
   /// OpenGL m_pnTextures
   /// </summary>
    public class Texture
    {
        private uint[] m_pnTextures;
		private string m_sInternalZipPath;

		private const string g_sDefaultTexture = "textures/base_floor/clang_floor.jpg";

        private Zipper m_zipper = new Zipper();
		MapInfo m_map = null;
        EFileType m_eFileType;
        Shape m_pParent = null;
        
        public enum EFileType { PNG, TGA, JPG };

        // Form the complete path of the m_pnTextures filename and create the m_pnTextures with the filename
        // If the m_pnTextures cannot be found then create a m_pnTextures with a default file
        public Texture( string sInternalZipPath, MapInfo map )
        {
			m_sInternalZipPath = sInternalZipPath;
			m_map = map;
        }

        public void SetParent(Shape sh) { m_pParent = sh; }

        public string GetPath() { return m_sInternalZipPath; }

        public EFileType GetFileType()
        {
            return m_eFileType;
        }

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

        public void SetTexture(string sURL, bool bLightmap)
        {
			bool bExtracted = true;

            string sFullPath;
            if (!bLightmap)
            {
                sFullPath = m_zipper.ExtractSoundTextureOther(sURL);
                m_eFileType = EFileType.JPG;
            }
            else
            {
                sFullPath = m_zipper.ExtractLightmap(sURL);
                m_eFileType = EFileType.PNG;
            }

            if (!File.Exists(sFullPath)) {
                if (!m_map.ExtractedFromZip)
                {
                    // I think I can remove this entire block. Check to be sure.
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
                    // try to find texture as tga
                    sFullPath = m_zipper.ExtractSoundTextureOther(Path.ChangeExtension(m_sInternalZipPath, "tga"));

                    // try to find the texture from the shader scripts
                    if (!File.Exists(sFullPath))
                    {
                        string sTemp = m_pParent.GetTexturePathFromShaderScripts(m_sInternalZipPath);
                        if (!string.IsNullOrEmpty(sTemp))
                        {
                            sFullPath = m_zipper.ExtractSoundTextureOther(sTemp);
                            if (!File.Exists(sFullPath))
                            {
                                sTemp = System.IO.Path.ChangeExtension(sTemp, "jpg");
                                sFullPath = m_zipper.ExtractSoundTextureOther(sTemp);
                            }
                        }
                    }

                    // custom finds
                    if (!File.Exists(sFullPath))
                    {
                        if (sFullPath.Contains("nightsky_xian_dm1"))
                            sFullPath = m_zipper.ExtractSoundTextureOther("env/xnight2_up.jpg");
                    }                      

                    if (!File.Exists(sFullPath))
                    {
                        LOGGER.Warning("Missing texture " + sURL + ". Looked here " + sFullPath);                       
                        sFullPath = m_zipper.ExtractSoundTextureOther(g_sDefaultTexture);
                    }
                    else m_eFileType = EFileType.TGA;
				}
			}

			LOGGER.Debug("Set texture to " + sFullPath);

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

            image.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);

            System.Drawing.Imaging.BitmapData bitmapdata;

            bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.GenTextures(1, m_pnTextures);
            GL.BindTexture(TextureTarget.Texture2D, m_pnTextures[0]);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
       
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
			string[] tokens = m_sInternalZipPath.Split(new char[] { '.' }); // separate filename around period
			string filetype = tokens[tokens.Length - 1]; // get the file extension

			// Try for creating lightmap m_pnTextures
			if (filetype == "png")
			{
				SetTexture(m_sInternalZipPath, true);
			}
			else
			{
				SetTexture(m_sInternalZipPath, false);
			}
		}
    }
}