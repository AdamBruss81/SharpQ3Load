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
        
        public enum EFileType { PNG, TGA, JPG };

        // Form the complete path of the m_pnTextures filename and create the m_pnTextures with the filename
        // If the m_pnTextures cannot be found then create a m_pnTextures with a default file
        public Texture( string sInternalZipPath, MapInfo map )
        {
			m_sInternalZipPath = sInternalZipPath;
			m_map = map;
        }

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

        private string GetTexturePathFromShaderScripts()
        {
            List<string> lsShaderFilenames = GetShaderFileName();
            string sNewPath = "";

            for (int i = 0; i < lsShaderFilenames.Count; i++)
            {
                string sShaderFilename = lsShaderFilenames[i];
                string sInternalPathNoExtension = System.IO.Path.ChangeExtension(m_sInternalZipPath, null);

                StreamReader sr = new StreamReader(m_zipper.ExtractShaderFile(sShaderFilename));
                while (!sr.EndOfStream)
                {
                    string sLine = sr.ReadLine();
                    if (sLine.Trim() == sInternalPathNoExtension) // found shader
                    {
                        int nCurlyCounter = 1;
                        sr.ReadLine(); // eat open curly

                        while (true) // read found shader to find what we need
                        {
                            string sInsideTargetShaderLine = sr.ReadLine();

                            if (sInsideTargetShaderLine.Contains("{"))
                            {
                                nCurlyCounter++;

                                string sMapLine = sr.ReadLine();
                                string[] tokens = sMapLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length == 2)
                                {
                                    if (tokens[0].Trim(new char[] { '\t' }) == "map" && (tokens[1].Contains("textures") || tokens[1].Contains("gfx")))
                                    {
                                        sNewPath = tokens[1];
                                        break;
                                    }
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("}"))
                            {
                                nCurlyCounter--;

                                if (nCurlyCounter == 0)
                                    break; // get outta here
                            }
                        }
                        break;
                    }
                }
                sr.Close();

                if (!string.IsNullOrEmpty(sNewPath)) break;
            }

            return sNewPath;
        }

        /// <summary>
        /// Get the shader file that contains the shader based on the texture from the vrml map file
        /// This is sort of a guessing game at the moment
        /// </summary>
        /// <returns></returns>
        private List<string> GetShaderFileName()
        {
            string[] tokens = m_sInternalZipPath.Split('/');

            if (tokens.Length > 0)
            {
                if (tokens[1].Contains("liquid"))
                    return new List<string>() { "liquid" };
                else if (tokens[1].Contains("skies"))
                    return new List<string>() { "sky" };
                else if (tokens[1].Contains("sfx"))
                    return new List<string>() { "sfx" };
                else if (tokens[1].Contains("skin"))
                    return new List<string>() { "skin" };
                else if (tokens[1].Contains("organics"))
                    return new List<string>() { "organics", "skin" };
                else if (tokens[1].Contains("base_wall"))
                    return new List<string>() { "base_wall", "sfx" };
                else if (tokens[1].Contains("base_button"))
                    return new List<string>() { "base_button" };
                else if (tokens[1].Contains("base_floor"))
                    return new List<string>() { "base_floor" };
                else if (tokens[1].Contains("base_light"))
                    return new List<string>() { "base_light" };
                else if (tokens[1].Contains("base_trim"))
                    return new List<string>() { "base_trim" };
                else if (tokens[1].Contains("gothic_floor"))
                    return new List<string>() { "gothic_floor" };
                else if (tokens[1].Contains("gothic_wall"))
                    return new List<string>() { "gothic_wall" };
                else if (tokens[1].Contains("gothic_block"))
                    return new List<string>() { "gothic_block", "sfx", "gothic_trim" };
                else if (tokens[1].Contains("gothic_light"))
                    return new List<string>() { "gothic_light" };
                else if (tokens[1].Contains("gothic_trim"))
                    return new List<string>() { "gothic_trim" };
                else if (tokens[1].Contains("common"))
                    return new List<string>() { "common" };
                else if (tokens[0].Contains("models"))
                    return new List<string>() { "models" };
                else if (tokens[1].Contains("ctf"))
                    return new List<string>() { "ctf" };
                else if (tokens[1].Contains("base_support"))
                    return new List<string>() { "base_support" };
                else
                    return new List<string>();
            }
            else 
                return new List<string>();
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
                        string sTemp = GetTexturePathFromShaderScripts();
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