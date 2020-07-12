using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace engine
{
    public class Q3Shader
    {
        public enum EStepType { DEFAULT, METAL, NONE };

        List<Q3ShaderStage> m_lStages = new List<Q3ShaderStage>();
        EStepType m_eStepType = EStepType.DEFAULT;
        string m_sMainTextureFullPath = "";
        private Zipper m_zipper = new Zipper();
        string m_sShaderName = "";
        string m_sCull = "";
        List<Texture> m_lStageTextures = new List<Texture>(); // ordered to match stages in q3 shader top to bottom. 
        // this list will change after init for effects like animmap
        Shape m_pParent = null;

        // static methods
        public static string GetSampler2DName() { return "sampler2D"; }
        // ***

        public Q3Shader(Shape parent)
        {
            m_pParent = parent;
        }        

        public string GetShaderName() { return m_sShaderName; }

        public Texture GetStageTexture(int iStage)
        {
            if(m_lStages[iStage].IsAnimmap())
            {
                return m_lStages[iStage].GetAnimmapTexture();
            }
            else { return m_lStageTextures[iStage]; }
        }

        public string GetShaderBasedMainTextureFullPath()
        {
            return m_sMainTextureFullPath;
        }

        public string GetCull() { return m_sCull; }

        public List<Q3ShaderStage> GetStages() { return m_lStages; }

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

        private string GetTokensAfterFirst(string[] tokens)
        {
            string s = "";
            for(int i = 1; i < tokens.Length; i++)
            {
                s += tokens[i];
                if (i < tokens.Length - 1) s += " ";
            }
            return s;
        }

        private string GetTokensAfterSecond(string[] tokens)
        {
            string s = "";
            for (int i = 2; i < tokens.Length; i++)
            {
                s += tokens[i];
                if (i < tokens.Length - 1) s += " ";
            }
            return s;
        }

        private bool IsMapTexture(string s)
        {
            return s.Contains("map") && (s.Contains("gfx") || s.Contains("textures") || s.Contains("models")) && !s.Contains("clampmap");
        }

        public string GetPathToTextureNoShaderLookup(bool bLightmap, string sURL, ref bool bShouldBeTGA)
        {
            string sFullPath;

            if (bLightmap)
            {
                sFullPath = m_zipper.ExtractLightmap(sURL);
            }
            else
            {
                sFullPath = m_zipper.ExtractSoundTextureOther(sURL);

                if (!File.Exists(sFullPath))
                {
                    // try to find texture as tga or jpg
                    // when quake 3 was near shipping, id had to convert some tgas to jpg to reduce pak0 size					
                    if (Path.GetExtension(sFullPath) == ".jpg")
                        sFullPath = m_zipper.ExtractSoundTextureOther(Path.ChangeExtension(sURL, "tga"));
                    else
                    {
                        bShouldBeTGA = true;
                        sFullPath = m_zipper.ExtractSoundTextureOther(Path.ChangeExtension(sURL, "jpg"));
                    }

                    if (!File.Exists(sFullPath))
                    {
                        // the only texture in the game of this nature
                        if (sFullPath.Contains("nightsky_xian_dm1"))
                            sFullPath = m_zipper.ExtractSoundTextureOther("env/xnight2_up.jpg");
                    }
                }
            }

            return sFullPath;
        }

        /// <summary>
        /// Convert quake3 shader into GLSL code. There is already some content in the glsl shader in the stringbuilder. See who calls this function.
        /// This functions puts in the q3 shader specific content.
        /// </summary>
        /// <param name="sb"></param>
        public void GenerateGLSL(System.Text.StringBuilder sb)
        {
            // add samplers to frag shader based on stage count
            for(int i = 0; i < m_lStages.Count; i++)
            {
                sb.AppendLine("uniform " + GetSampler2DName() + " texture" + Convert.ToString(i) + ";");
            }

            sb.AppendLine("");

            bool bAddTime = false;

            // add uniforms
            for(int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                string sIndex = Convert.ToString(i);

                // rgbgen
                if(!stage.IsRGBGENIdentity() && !stage.IsVertexColor())
                {
                    sb.AppendLine("uniform vec3 rgbgen" + sIndex + ";");
                }

                // tcmod
                if(stage.GetTCMODS().Count > 0) // there are tcmods
                {
                    bAddTime = true;
                    for(int j = 0; j < stage.GetTCMODS().Count; j++) // this order doesn't matter for the uniform declarations
                    {
                        switch(stage.GetTCMODS()[j].GetModType())
                        {
                            case TCMOD.ETYPE.SCALE: sb.AppendLine("uniform vec2 scale" + sIndex + ";"); break;
                            case TCMOD.ETYPE.SCROLL: sb.AppendLine("uniform vec2 scroll" + sIndex + ";"); break;
                            case TCMOD.ETYPE.TURB: sb.AppendLine("uniform vec3 turb" + sIndex + ";"); break;
                        }
                    }                    
                }
            }

            sb.AppendLine("");

            // I'm trying to make these auto generated glsl shaders as minimal as possible to make debugging and reading them easier. So
            // only add this time uniform if it's actually used.
            if(bAddTime) sb.AppendLine("uniform float timeS;");

            // create main function
            sb.AppendLine("");
            sb.AppendLine("void main()");
            sb.AppendLine("{");

            // define tcmods
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                string sIndex = Convert.ToString(i);
                string sTexmod = "texmod" + sIndex;

                if (stage.GetTCMODS().Count > 0)
                {
                    sb.AppendLine("vec2 " + sTexmod + " = mainTexCoord;");
                }

                for (int j = 0; j < stage.GetTCMODS().Count; j++)
                {
                    switch (stage.GetTCMODS()[j].GetModType())
                    {
                        case TCMOD.ETYPE.SCROLL:
                            {
                                if (GetShaderName().Contains("skies"))
                                {
                                    sb.AppendLine(sTexmod + ".x += scroll" + sIndex + "[0] * timeS * 10;");
                                    sb.AppendLine(sTexmod + ".y += scroll" + sIndex + "[1] * timeS * 10;");
                                }
                                else
                                {
                                    sb.AppendLine(sTexmod + ".x += scroll" + sIndex + "[0] * timeS;");
                                    sb.AppendLine(sTexmod + ".y += scroll" + sIndex + "[1] * timeS;");
                                }
                                break;
                            }
                        case TCMOD.ETYPE.SCALE:
                            {
                                if (GetShaderName().Contains("skies"))
                                {
                                    sb.AppendLine(sTexmod + ".x /= scale" + sIndex + "[0];");
                                    sb.AppendLine(sTexmod + ".y /= scale" + sIndex + "[1];");
                                }
                                else
                                {
                                    sb.AppendLine(sTexmod + ".x *= scale" + sIndex + "[0];");
                                    sb.AppendLine(sTexmod + ".y *= scale" + sIndex + "[1];");
                                }
                                break;
                            }
                        case TCMOD.ETYPE.TURB:
                            {
                                sb.AppendLine("float turbVal" + sIndex + " = turb" + sIndex + "[1] + timeS * turb" + sIndex + "[2];");
                                sb.AppendLine(sTexmod + ".x += sin( ( (vertice.x + vertice.z) * 1.0/128.0 * 0.125 + turbVal" + sIndex + " ) * 6.238) * turb" + sIndex + "[0];");
                                sb.AppendLine(sTexmod + ".y += sin( (vertice.y * 1.0/128.0 * 0.125 + turbVal" + sIndex + " ) * 6.238) * turb" + sIndex + "[0];");
                                break;
                            }
                    }
                }
            }

            sb.AppendLine("");

            // define texture texels and create stage textures
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                string sIndex = Convert.ToString(i);

                if (stage.GetLightmap())
                {
                    m_lStageTextures.Add(m_pParent.GetLightmapTexture());

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", lightmapTexCoord);");
                }
                else if (!string.IsNullOrEmpty(stage.GetTexturePath())) 
                {
                    m_lStageTextures.Add(new Texture(stage.GetTexturePath()));
                    bool bTGA = false;
                    m_lStageTextures[m_lStageTextures.Count - 1].SetTexture(GetPathToTextureNoShaderLookup(false, stage.GetTexturePath(), ref bTGA));
                    m_lStageTextures[m_lStageTextures.Count - 1].SetShouldBeTGA(bTGA);

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex;

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");
                }
                else if(stage.IsAnimmap())
                {
                    m_lStageTextures.Add(stage.GetAnimmapTexture()); // initial texture, will change as time passes

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex;
                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");
                }
            }

            sb.AppendLine("");

            // define rgbgen vec4s to use in outputColor below
            for(int i = 0; i < m_lStages.Count; i++)
            {
                if(!m_lStages[i].IsRGBGENIdentity() && !m_lStages[i].IsVertexColor())
                {
                    sb.AppendLine("vec4 rgbmod" + Convert.ToString(i) + " = vec4(rgbgen" + Convert.ToString(i) + ", 1.0);");
                }
            }

            sb.AppendLine("");

            // define outputColor line
            sb.AppendLine("outputColor = vec4(0.0);"); // black out outputColor to start

            sb.AppendLine("");

            bool bAddAlpha = true; // this is for tgas that got converted to jpg by id before shipping
            // if they were tgas, the blending would just work for free for the most part
            // instead i have to add alpha myself. not sure how q3 actually does this
            // deciding when to do it is specific for now. see below

            System.Text.StringBuilder sbOutputline = new System.Text.StringBuilder();           
            for(int i = 0; i < m_lStages.Count; i++)
            {                
                Q3ShaderStage stage = m_lStages[i];
                string sLightmapScale = stage.GetLightmap() ? " * 3.0" : "";
                 
                string sIndex = Convert.ToString(i);
                string sTexel = "texel" + sIndex;

                string sub = sTexel;
                if (!stage.IsRGBGENIdentity() && !stage.IsVertexColor()) sub = "(" + sTexel + " * rgbmod" + sIndex + ")";
                else if (stage.IsVertexColor()) sub = "(" + sTexel + " * color * 2.0)";                

                if (stage.GetBlendFunc() == "gl_dst_color gl_zero") // src * dest
                {
                    sb.Append("outputColor *= " + sub + sLightmapScale);
                }
                else if (stage.GetBlendFunc() == "gl_one gl_one") // src + dest
                {
                    sb.Append("outputColor += " + sub + sLightmapScale);
                }
                else if (stage.GetBlendFunc() == "gl_src_alpha gl_one_minus_src_alpha") // mix
                {
                    sb.Append("outputColor = " + sTexel + " * " + sTexel + ".w + texel" + Convert.ToString(i - 1) + " * (1 - " + sTexel + ".w)");
                }
                else if (stage.GetBlendFunc() == "gl_dst_color gl_one_minus_dst_alpha")
                {
                    sb.Append("outputColor = (" + sub + " * outputColor" + sLightmapScale + " + outputColor * (1 - outputColor.w))");
                }
                else if (stage.IsVertexColor())
                {
                    sb.Append("outputColor += (" + sTexel + " * color * 2.0)");
                }
                else
                {
                    sb.Append("outputColor += (" + sub + ")" + sLightmapScale);
                }

                sb.Append(";\r\n");

                // alpha testing - for example makes skel in fatal instinct look much better
                if(stage.GetAlphaFunc() == "ge128")
                {
                    sb.AppendLine("if(outputColor.w < 0.5) discard;");
                }
                else if(stage.GetAlphaFunc() == "gt0")
                {
                    sb.AppendLine("if(outputColor.w <= 0) discard;");
                }
                else if(stage.GetAlphaFunc() == "lt128")
                {
                    sb.AppendLine("if(outputColor.w >= 0.5) discard;");
                }

                /*if (m_lStageTextures[i] != null && m_lStageTextures[i].GetShouldBeTGA() && !string.IsNullOrEmpty(stage.GetBlendFunc()))
                {
                }
                else bAddAlpha = false;*/
            }

            // don't do this for now. There are two problems. It's a hack I think. #2 I can't tell when to do it and when not
            // to. 
            if (bAddAlpha)
            {
                //sb.AppendLine("");

                //sb.AppendLine("float na = 0.2126 * outputColor.r + 0.7152 * outputColor.g + 0.0722 * outputColor.b;");
                //sb.AppendLine("float na = 0.299 * outputColor.r + 0.587 * outputColor.g + 0.114 * outputColor.b;");

                // best
                //sb.AppendLine("float na = sqrt(0.299 * pow(outputColor.r, 2) + 0.587 * pow(outputColor.g, 2) + 0.114 * pow(outputColor.b, 2));");
                //sb.AppendLine("outputColor.w = na;");

                //sqrt(0.299 * R ^ 2 + 0.587 * G ^ 2 + 0.114 * B ^ 2)
                // (0.299*R + 0.587*G + 0.114*B
            }

            // end main
            sbOutputline.AppendLine("}");

            sb.Append(sbOutputline.ToString());
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
                    string sLine = sr.ReadLine().ToLower();

                    if (sLine.Trim() == sInternalPathNoExtension.ToLower()) // found shader
                    {
                        m_sShaderName = sPathFromVRML;

                        // read until we eat open curly
                        sLine = sr.ReadLine();
                        while(!sLine.Contains("{"))
                        {
                            sLine = sr.ReadLine();
                        }

                        nCurlyCounter++;

                        while (true) // read found shader surface params and stages
                        {
                            string sInsideTargetShaderLine = sr.ReadLine().ToLower();

                            if(sInsideTargetShaderLine.Contains("//"))
                            {
                                continue;
                                // for now just avoid lines with comments in them
                                // may need to make this smarter later for lines with comments
                                // at end and valid stuff at beginning of line
                            }

                            // read shader properties
                            if (sInsideTargetShaderLine.Contains("surfaceparm"))
                            {
                                if (sInsideTargetShaderLine.Contains("metalsteps"))
                                {
                                    m_eStepType = EStepType.METAL;
                                }
                                if(sInsideTargetShaderLine.Contains("nosteps"))
                                {
                                    m_eStepType = EStepType.NONE;
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("cull"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_sCull = GetTokensAfterFirst(tokens);
                            }
                            // begin stage found
                            else if (sInsideTargetShaderLine.Contains("{"))
                            {
                                m_lStages.Add(new Q3ShaderStage(this));
                                nCurlyCounter++;
                            }
                            else if (sInsideTargetShaderLine.Contains("animmap")) // this needs to be before the map texture one
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetAnimmap(GetTokensAfterFirst(tokens));                                
                            }
                            // read stage items
                            else if(IsMapTexture(sInsideTargetShaderLine))  
                            {
                                string[] tokens = sInsideTargetShaderLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length == 2)
                                {
                                    if (tokens[0].Trim(new char[] { '\t' }) == "map")
                                    {
                                        if (string.IsNullOrEmpty(sNewPath)) sNewPath = tokens[1];
                                        m_lStages[m_lStages.Count - 1].SetTexturePath(tokens[1]);
                                    }
                                }
                            }
                            else if(sInsideTargetShaderLine.Contains("rgbgen"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetRGBGEN(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("blendfunc"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetBlendFunc(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("alphafunc"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetAlphaFunc(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod scroll"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetTCModScroll(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod turb"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetTCModTurb(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod scale"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetTCModeScale(GetTokensAfterSecond(tokens));
                            }
                            else if(sInsideTargetShaderLine.Contains("$lightmap"))
                            {
                                m_lStages[m_lStages.Count - 1].SetLightmap(true);
                            }
                            // end stage reading                            
                            else if (sInsideTargetShaderLine.Contains("}")) // end of stage
                            {
                                nCurlyCounter--;

                                if (nCurlyCounter == 0)
                                    break; // end of shader
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
