using System;
using System.Collections.Generic;
using System.IO;

namespace engine
{
    public class Q3Shader
    {
        public enum EStepType { DEFAULT, METAL };

        List<Q3ShaderStage> m_lStages = new List<Q3ShaderStage>();
        EStepType m_eStepType = EStepType.DEFAULT;
        string m_sMainTextureFullPath = "";
        private Zipper m_zipper = new Zipper();

        public Q3Shader()
        {

        }

        public string GetShaderBasedMainTextureFullPath()
        {
            return m_sMainTextureFullPath;
        }

        public EStepType GetStepType() { return m_eStepType; }

        /// <summary>
        /// Get the shader file that contains the shader based on the texture from the vrml map file
        /// This is sort of a guessing game at the moment
        /// </summary>
        /// <returns></returns>
        private List<string> GetShaderFileName(string sPathFromVRML)
        {
            string[] tokens = sPathFromVRML.Split('/');

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

        /// <summary>
        /// Reads the shader files and finds the right texture
        /// </summary>
        /// <returns></returns>
        public void ReadShader(string sPathFromVRML)
        {
            List<string> lsShaderFilenames = GetShaderFileName(sPathFromVRML);
            string sNewPath = "";

            for (int i = 0; i < lsShaderFilenames.Count; i++)
            {
                string sShaderFilename = lsShaderFilenames[i];
                string sInternalPathNoExtension = System.IO.Path.ChangeExtension(sPathFromVRML, null);

                StreamReader sr = new StreamReader(m_zipper.ExtractShaderFile(sShaderFilename));
                while (!sr.EndOfStream)
                {
                    int nCurlyCounter = 0;
                    string sLine = sr.ReadLine();
                    if (sLine.Trim() == sInternalPathNoExtension) // found shader
                    {
                        // read until we eat open curly
                        sLine = sr.ReadLine();
                        while(!sLine.Contains("{"))
                        {
                            sLine = sr.ReadLine();
                        }

                        nCurlyCounter++;

                        while (true) // read found shader to find what we need
                        {
                            string sInsideTargetShaderLine = sr.ReadLine();

                            if (sInsideTargetShaderLine.Contains("{"))
                            {
                                nCurlyCounter++;

                                if (string.IsNullOrEmpty(sNewPath))
                                {
                                    string sMapLine = sr.ReadLine();
                                    string[] tokens = sMapLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (tokens.Length == 2)
                                    {
                                        if (tokens[0].Trim(new char[] { '\t' }) == "map" && (tokens[1].Contains("textures") || tokens[1].Contains("gfx")))
                                        {
                                            sNewPath = tokens[1];
                                        }
                                    }
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("surfaceparm"))
                            {
                                if (sInsideTargetShaderLine.Contains("metalsteps"))
                                {
                                    m_eStepType = EStepType.METAL;
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

            if (!string.IsNullOrEmpty(sNewPath))
            {
                string sTemp = m_zipper.ExtractSoundTextureOther(sNewPath);
                if (!File.Exists(sTemp))
                {
                    sTemp = System.IO.Path.ChangeExtension(sNewPath, "jpg");
                    sTemp = m_zipper.ExtractSoundTextureOther(sTemp);
                }

                m_sMainTextureFullPath = sTemp;
            }
        }
    }
}
