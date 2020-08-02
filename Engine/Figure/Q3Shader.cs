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
        string m_sLightImageFullPath = "";
        List<Texture> m_lStageTextures = new List<Texture>(); // ordered to match stages in q3 shader top to bottom. 
        // this list will change after init for effects like animmap
        Shape m_pParent = null;
        bool m_bTrans = false;
        bool m_bAlphaShadow = false;
        bool m_bLava = false;
        bool m_bSky = false;
        bool m_bNonSolid = false;
        bool m_bWater = false;
        bool m_bWaterGLZERO = true;
        bool m_bFog = false;
        string m_sSort = "";

        // static methods
        public static string GetSampler2DName() { return "sampler2D"; }
        // ***

        public Q3Shader(Shape parent)
        {
            m_pParent = parent;
        }

        public string GetSort()
        {
            return m_sSort;
        }
        public bool GetLava() { return m_bLava; }
        public bool GetFog() { return m_bFog; }
        public bool GetNonSolid() { return m_bNonSolid; }
        public bool GetSky() { return m_bSky; }

        public bool GetTrans() { return m_bTrans; }

        public bool GetAddAlpha()
        {
            bool bAA = false;

            // for these i can't tell by looking at the shader if they need to be see through so hardcoding for now
            bAA = m_sShaderName.Contains("sfx/teslacoil") || m_sShaderName.Contains("console/centercon") ||
                m_sShaderName.Contains("teleporter/energy") || m_sShaderName.Contains("pj_light"); // for example see big power reactor in dm0
            // pj_light is ball light in brimstone abbey

            if (!bAA) bAA = m_bAlphaShadow;
            if (!bAA) bAA = m_bTrans;
            if (!bAA)
            {
                if (m_lStages.Count == 1 && (m_lStageTextures[0].GetShouldBeTGA() || m_lStageTextures[0].IsTGA()) && m_lStages[0].GetBlendFunc() != "")
                {
                    bAA = true;
                }
            }
            // i thought i wouldn't need to do the below check but some flame shaders don't have
            // trans set
            if (!bAA) bAA = m_sShaderName.Contains("sfx") && m_sShaderName.Contains("flame");

            if (!bAA)
            {
                // more hardcoding for shaders that don't make it clear if they should have transparency
                bAA = m_sShaderName.Contains("transparency") || m_sShaderName.Contains("widget");
            }

            if (m_bLava) bAA = false; // not sure why trans is set on lava shaders sometimes

            return bAA;
        }

        public string GetShaderName() { return m_sShaderName; }

        public Texture GetStageTexture(Q3ShaderStage stage)
        {
            if (stage.IsAnimmap()) return stage.GetAnimmapTexture();
            else {
                return m_lStageTextures[m_lStages.IndexOf(stage)];
            }
        }

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
                    return new List<string>() { "sfx", "common" };
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
                else if (tokens[1].Contains("gothic_door")) // for this the only way to know where to look is by looking through all the maps and seeing what doesn't look right
                    return new List<string>() { "gothic_block" }; // i can't think of any programmatic way besides searching the entire set of shader files which I don't want to do
                else if (tokens[1].Contains("common"))
                    return new List<string>() { "common" };
                else if (tokens[0].Contains("models"))
                    return new List<string>() { "models", "sfx" };
                else if (tokens[1].Contains("ctf"))
                    return new List<string>() { "ctf" };
                else if (tokens[1].Contains("base_support"))
                    return new List<string>() { "base_support" };
                else if (tokens[1].Contains("base_door"))
                    return new List<string>() { "base_wall" };
                /*else if (tokens[1].Contains("mapobjects"))
                    return new List<string>() { "sfx" };*/
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
            return s.Contains("map") && (s.Contains("gfx") || s.Contains("textures") || s.Contains("models"));
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

        private void DefineInitialOutputColor(System.Text.StringBuilder sb)
        {
            // define outputColor line
            if (m_sLightImageFullPath != "" && m_lStages[0].GetBlendFunc() != "")
            {
                float[] fCol = Texture.GetAverageColor(m_sLightImageFullPath);
                sb.AppendLine("outputColor = vec4(" + Math.Round(fCol[0], 1) + "/255.0, " + Math.Round(fCol[1], 1) +
                    "/255.0, " + Math.Round(fCol[2], 1) + "/255.0, " + Math.Round(fCol[3], 1) + "/255.0);");
            }
            else if (m_bWater)
            {
                if (m_bWaterGLZERO)
                {
                    // more color and opaqueness because the blend functions are zeroing
                    sb.AppendLine("outputColor = vec4(0.3, 0.6, 0.45, 0.2);");
                }
                else // gl_one
                {
                    // less color and opaqueness because the blend functions are adding
                    sb.AppendLine("outputColor = vec4(0.1, 0.2, 0.15, 0.05);");
                }
            }
            else
            {
                sb.AppendLine("outputColor = vec4(0.0);"); // black out outputColor to start
            }
        }

        /// <summary>
        /// Convert quake3 shader into GLSL code. There is already some content in the glsl shader in the stringbuilder. See who calls this function.
        /// This functions puts in the q3 shader specific content.
        /// </summary>
        /// <param name="sb"></param>
        public void GenerateGLSL(System.Text.StringBuilder sb)
        {
            // add samplers to frag shader based on stage count
            for (int i = 0; i < m_lStages.Count; i++)
            {
                sb.AppendLine("uniform " + GetSampler2DName() + " texture" + Convert.ToString(i) + ";");
            }

            sb.AppendLine("");

            bool bAddTime = false;

            // add uniforms
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                string sIndex = Convert.ToString(i);

                // rgbgen - at some point change rgbgen to a single value i think
                if (!stage.IsRGBGENIdentity() && !stage.IsVertexColor())
                {
                    sb.AppendLine("uniform vec3 rgbgen" + sIndex + ";");
                }

                // tcmod
                if (stage.GetTCMODS().Count > 0) // there are tcmods
                {
                    bAddTime = true;
                    for (int j = 0; j < stage.GetTCMODS().Count; j++) // this order doesn't matter for the uniform declarations
                    {
                        switch (stage.GetTCMODS()[j].GetModType())
                        {
                            case TCMOD.ETYPE.SCALE: sb.AppendLine("uniform vec2 scale" + sIndex + ";"); break;
                            case TCMOD.ETYPE.SCROLL: sb.AppendLine("uniform vec2 scroll" + sIndex + ";"); break;
                            case TCMOD.ETYPE.TURB: sb.AppendLine("uniform vec3 turb" + sIndex + ";"); break;
                            case TCMOD.ETYPE.STRETCH: sb.AppendLine("uniform float stretch" + sIndex + "[6];"); break;
                            case TCMOD.ETYPE.ROTATE: sb.AppendLine("uniform float rotate" + sIndex + "[6];"); break;
                        }
                    }
                }
            }

            sb.AppendLine("");

            // I'm trying to make these auto generated glsl shaders as minimal as possible to make debugging and reading them easier. So
            // only add this time uniform if it's actually used.
            if (bAddTime) sb.AppendLine("uniform float timeS;");

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
                    if (stage.GetTCGEN_CS() == "environment")
                        sb.AppendLine("vec2 " + sTexmod + " = tcgenEnvTexCoord;");
                    else
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
                                    sb.AppendLine(sTexmod + ".x -= scroll" + sIndex + "[0] * timeS * 10;");
                                    sb.AppendLine(sTexmod + ".y -= scroll" + sIndex + "[1] * timeS * 10;");
                                }
                                else
                                {
                                    sb.AppendLine(sTexmod + ".x -= scroll" + sIndex + "[0] * timeS;");
                                    sb.AppendLine(sTexmod + ".y -= scroll" + sIndex + "[1] * timeS;");
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
                        case TCMOD.ETYPE.STRETCH:
                            {
                                // 0 - 3 are the 2x2 transform matrix
                                // 4-5 are the translate vector
                                sb.AppendLine("float " + sTexmod + "_stretch_x = " + sTexmod + ".x;");
                                sb.AppendLine("float " + sTexmod + "_stretch_y = " + sTexmod + ".y;");
                                sb.AppendLine(sTexmod + ".x = " + sTexmod + "_stretch_x * stretch" + sIndex + "[0] + " + sTexmod + "_stretch_y * stretch" + sIndex + "[1] + stretch" + sIndex + "[4];");
                                sb.AppendLine(sTexmod + ".y = " + sTexmod + "_stretch_x * stretch" + sIndex + "[2] + " + sTexmod + "_stretch_y * stretch" + sIndex + "[3] + stretch" + sIndex + "[5];");
                                break;
                            }
                        case TCMOD.ETYPE.ROTATE:
                            {
                                // 0 - 3 are the 2x2 transform matrix
                                // 4-5 are the translate vector
                                sb.AppendLine("float " + sTexmod + "_rotate_x = " + sTexmod + ".x;");
                                sb.AppendLine("float " + sTexmod + "_rotate_y = " + sTexmod + ".y;");
                                sb.AppendLine(sTexmod + ".x = " + sTexmod + "_rotate_x * rotate" + sIndex + "[0] + " + sTexmod + "_rotate_y * rotate" + sIndex + "[1] + rotate" + sIndex + "[4];");
                                sb.AppendLine(sTexmod + ".y = " + sTexmod + "_rotate_x * rotate" + sIndex + "[2] + " + sTexmod + "_rotate_y * rotate" + sIndex + "[3] + rotate" + sIndex + "[5];");
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

                    if (stage.GetTCGEN_CS() == "environment")                    
                        sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", tcgenEnvTexCoord);");                    
                    else 
                        sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", lightmapTexCoord);");
                }
                else if (!string.IsNullOrEmpty(stage.GetTexturePath()))
                {
                    m_lStageTextures.Add(new Texture(stage.GetTexturePath()));
                    bool bTGA = false;
                    m_lStageTextures[m_lStageTextures.Count - 1].SetClamp(stage.GetClampmap());
                    m_lStageTextures[m_lStageTextures.Count - 1].SetTexture(GetPathToTextureNoShaderLookup(false, stage.GetTexturePath(), ref bTGA));
                    m_lStageTextures[m_lStageTextures.Count - 1].SetShouldBeTGA(bTGA);                    

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCGEN_CS() == "environment") sTexCoordName = "tcgenEnvTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex; // this can have started with tcgen environment already

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");

                    if(bTGA && GetAddAlpha())
                    {
                        AppendAddAlphaLine(sIndex, sb);                                               
                    }
                }
                else if (stage.IsAnimmap())
                {
                    m_lStageTextures.Add(stage.GetAnimmapTexture()); // initial texture, will change as time passes

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex;

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");

                    if(m_lStageTextures[m_lStageTextures.Count - 1].GetShouldBeTGA() && GetAddAlpha())
                    {
                        AppendAddAlphaLine(sIndex, sb);
                    }
                }
                else
                {
                    m_lStageTextures.Add(null); // need a placeholder here, will check for null later
                }

                sb.AppendLine("");
            }

            sb.AppendLine("");

            // define rgbgen vec4s to use in outputColor below
            for (int i = 0; i < m_lStages.Count; i++)
            {
                if (!m_lStages[i].IsRGBGENIdentity() && !m_lStages[i].IsVertexColor())
                {
                    sb.AppendLine("vec4 rgbmod" + Convert.ToString(i) + " = vec4(rgbgen" + Convert.ToString(i) + ", 1.0);");
                }
            }

            sb.AppendLine("");

            if (m_bWater)
            {
                for (int i = 0; i < m_lStages.Count; i++)
                {
                    if (m_lStages[i].GetBlendFunc() == "gl_dst_color gl_one")
                    {
                        // I don't know where to get the initial color for water from. I try to determine it from the blending functions
                        // in the water shader. This is not ideal but it gets us something decent.
                        m_bWaterGLZERO = false;
                        break;
                    }
                }
            }

            DefineInitialOutputColor(sb);

            sb.AppendLine("");

            System.Text.StringBuilder sbOutputline = new System.Text.StringBuilder();
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                if (stage.Skip()) continue;
                string sLightmapScale = stage.GetLightmap() ? " * 3.0" : "";

                string sIndex = Convert.ToString(i);
                string sTexel = "texel" + sIndex;
                string sBlendFunc = stage.GetBlendFunc();

                // alphaGen
                if (stage.GetAlphaGenFunc() == "lightingspecular")
                {
                    sb.AppendLine(sTexel + ".w *= alphaGenSpecular;");
                }

                // start forming outputColor
                string sub = sTexel;
                if (!stage.IsRGBGENIdentity() && !stage.IsVertexColor())
                {
                    sub = "(" + sTexel + " * rgbmod" + sIndex + ")";
                }                

                // blend functions in q3
                if (sBlendFunc == "gl_dst_color gl_zero" || sBlendFunc == "filter") // src * dest
                {
                    sb.AppendLine("outputColor *= " + sub + sLightmapScale + ";");
                }
                else if (sBlendFunc == "gl_one gl_one" || sBlendFunc == "add") // src + dest
                {
                    sb.AppendLine("outputColor += " + sub + sLightmapScale + ";");
                }
                else if (sBlendFunc == "gl_src_alpha gl_one_minus_src_alpha" || sBlendFunc == "blend") // mix
                {
                    sb.AppendLine("outputColor = mix(outputColor, " + sub + ", " + sub + ".w);");
                }
                else if (sBlendFunc == "gl_one_minus_src_alpha gl_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sub + " * (1 - " + sub + ".w) + outputColor * " + sub + ".w;");
                }
                else if (sBlendFunc == "gl_dst_color gl_one_minus_dst_alpha")
                {
                    sb.AppendLine("outputColor = (" + sub + " * outputColor" + sLightmapScale + " + outputColor * (1 - outputColor.w));");
                }
                else if (sBlendFunc == "gl_one gl_zero")
                {
                    sb.AppendLine("outputColor += (" + sub + ");");
                }
                else if (sBlendFunc == "gl_dst_color gl_one")
                {
                    sb.AppendLine("outputColor = " + sub + " * outputColor + outputColor;");
                }
                else if (sBlendFunc == "gl_dst_color gl_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sub + " * outputColor + outputColor * " + sub + ".w;");
                }
                else if(sBlendFunc == "gl_one gl_one_minus_src_alpha") 
                {
                    sb.AppendLine("outputColor = " + sub + " + outputColor * (1 - " + sub + ".w);");
                }
                else if(sBlendFunc == "gl_zero gl_one_minus_src_color")
                {
                    sb.AppendLine("outputColor = outputColor * (1 - " + sub + ");");
                } // end blend functions
                else if(!stage.IsVertexColor())
                {
                    sb.AppendLine("outputColor += (" + sub + ")" + sLightmapScale + ";");
                }

                if (stage.IsVertexColor()) // always do this if it is in the shader stage. see horned shader model in dm3
                {
                    if (sBlendFunc == "")
                        sb.AppendLine("outputColor += (" + sTexel + " * color * 3.0);");
                    else
                        sb.AppendLine("outputColor *= (color * 3.0);");
                }
                
                // clamp colors that are over 1.0
                sb.AppendLine("outputColor = clamp(outputColor, 0.0, 1.0);");

                // alpha testing - for example makes skel in fatal instinct look much better
                if (stage.GetAlphaFunc() == "ge128")
                {
                    sb.AppendLine("if(outputColor.w < 0.5) discard;");
                }
                else if (stage.GetAlphaFunc() == "gt0")
                {
                    sb.AppendLine("if(outputColor.w <= 0) discard;");
                }
                else if (stage.GetAlphaFunc() == "lt128")
                {
                    sb.AppendLine("if(outputColor.w >= 0.5) discard;");
                }

                sb.AppendLine("");                
            }

            CustomizeFinalOutputColor(sb);

            // end main
            sbOutputline.AppendLine("}");

            // add helpful comments
            sbOutputline.AppendLine("");
            if (GetAddAlpha()) sbOutputline.AppendLine("// add alpha is enabled");
            if (m_sLightImageFullPath != "") sbOutputline.AppendLine("// light image is " + m_sLightImageFullPath);

            sb.Append(sbOutputline.ToString());
        }

        private void CustomizeFinalOutputColor(System.Text.StringBuilder sb)
        {
            if(m_sShaderName.Contains("pj_light"))
            {
                // the q3 shader for pj_light does not represent what it looks like in q3
                // customizing it here
                sb.AppendLine("outputColor.xyz *= 2.0;");
                sb.AppendLine("outputColor.w = 0.5;");
            }
        }

        private void AppendAddAlphaLine(string sIndex, System.Text.StringBuilder sb)
        {
            string sTexel = "texel" + sIndex;
            // create transparency float using formula
            sb.AppendLine("float texelA" + sIndex + " = sqrt(0.299 * pow(" + sTexel + ".r, 2) + 0.587 * pow(" + sTexel + ".g, 2) + 0.114 * pow(" + sTexel + ".b, 2));");            

            // customize float based on shader
            if(m_sShaderName.Contains("glass"))
            {
                sb.AppendLine("texelA" + sIndex + " *= 0.3;"); // make glass more transparent
            }
            else if(m_sShaderName.Contains("lamp"))
            {
                sb.AppendLine("texelA" + sIndex + " *= 2.5;"); // make things a lot less transparent from original value
                // in dm4 the skull lights have lightbulbs rendered in the lamp glass. i think this is completely in the q3 game
                // there is no indication of this in the shaders so im not going to worry about it for now
            }
            else
            {
                sb.AppendLine("texelA" + sIndex + " *= 1.5;"); // make things a bit less transparent
            }
            // clamp float and set texel to it
            sb.AppendLine("texelA" + sIndex + " = clamp(texelA" + sIndex + ", 0.0, 1.0);");
            sb.AppendLine(sTexel + ".w = texelA" + sIndex + ";");
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

                            // read surface parameters
                            if (sInsideTargetShaderLine.Contains("surfaceparm"))
                            {
                                if (sInsideTargetShaderLine.Contains("metalsteps"))
                                {
                                    m_eStepType = EStepType.METAL;
                                }
                                else if(sInsideTargetShaderLine.Contains("nosteps"))
                                {
                                    m_eStepType = EStepType.NONE;
                                }
                                else if(sInsideTargetShaderLine.Contains("trans"))
                                {
                                    m_bTrans = true;
                                }
                                else if(sInsideTargetShaderLine.Contains("alphashadow"))
                                {
                                    m_bAlphaShadow = true;
                                }
                                else if(sInsideTargetShaderLine.Contains("lava"))
                                {
                                    m_bLava = true;
                                }
                                else if(sInsideTargetShaderLine.Contains("nonsolid"))
                                {
                                    m_bNonSolid = true;
                                }
                                else if(sInsideTargetShaderLine.Contains("water"))
                                {
                                    m_bWater = true;
                                } 
                                else if(sInsideTargetShaderLine.Contains("sky"))
                                {
                                    m_bSky = true;
                                }
                                else if(sInsideTargetShaderLine.Contains("fog"))
                                {
                                    m_bFog = true;
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("sort"))
                            {
                                string[] tokens = sInsideTargetShaderLine.Trim().Split(' ');
                                if (tokens.Length > 1)
                                {
                                    m_sSort = tokens[1];
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("cull"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_sCull = GetTokensAfterFirst(tokens);                                
                            }
                            else if(sInsideTargetShaderLine.Contains("q3map_lightimage"))
                            {
                                // this affects the initial color of outputColor
                                // it sets it to the average color of the image
                                 
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');

                                bool bJunk = false;
                                string sTexPath = GetPathToTextureNoShaderLookup(false, tokens[1], ref bJunk);
                                if(File.Exists(sTexPath))
                                {
                                    m_sLightImageFullPath = sTexPath;
                                } 
                            }

                            // begin stage found
                            // sometimes there is shader content after the open stage curly on the same line
                            if (sInsideTargetShaderLine.Contains("{"))
                            {
                                m_lStages.Add(new Q3ShaderStage(this));
                                nCurlyCounter++;

                                // remove open curly from line
                                int nCurIndex = sInsideTargetShaderLine.IndexOf("{");
                                sInsideTargetShaderLine = sInsideTargetShaderLine.Substring(nCurIndex + 1);
                            }

                            if (sInsideTargetShaderLine.Contains("animmap")) // this needs to be before the map texture one
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
                                    if (tokens[0].Trim(new char[] { '\t' }) == "map" || tokens[0].Trim(new char[] { '\t' }) == "clampmap")
                                    {
                                        if (string.IsNullOrEmpty(sNewPath)) sNewPath = tokens[1];
                                        m_lStages[m_lStages.Count - 1].SetTexturePath(tokens[1]);
                                        if(sInsideTargetShaderLine.Contains("clampmap"))
                                        {
                                            m_lStages[m_lStages.Count - 1].SetClampmap(true);
                                        }
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
                            else if(sInsideTargetShaderLine.Contains("alphagen"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetAlphaGen(GetTokensAfterFirst(tokens));
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
                            else if (sInsideTargetShaderLine.Contains("tcmod rotate"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');

                                string sRotate = GetTokensAfterSecond(tokens);
                                float fRotate;
                                if(Single.TryParse(sRotate, out fRotate))
                                {
                                    // the portal in dm0 has a rotate value of .1 .1. i think this means to rotate back and forth a tiny bit.saw
                                    // for now just ignore this. maybe implement later. funny that the shader manual doesn't mention this.
                                    // i find lots of little things the shader manual doesn't mention but are in the built in q3 maps.
                                    // it's amusing because it gives you insight into the development process of q3 and id
                                    m_lStages[m_lStages.Count - 1].SetTCMODRotate(fRotate);
                                }                                
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod stretch"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetTCMODStretch(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod scale"))
                            {
                                string sTrimmed = sInsideTargetShaderLine.Trim();
                                string[] tokens = sTrimmed.Split(' ');
                                m_lStages[m_lStages.Count - 1].SetTCModeScale(GetTokensAfterSecond(tokens));
                            }
                            else if(sInsideTargetShaderLine.Contains("tcgen environment"))
                            {
                                m_lStages[m_lStages.Count - 1].SetTCGEN_CS("environment");
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
                                
                                // this is a good spot to exit out of the shader reading process to debug shaders
                                // exit out after stages one by one to test stages one by one
                                if(m_sShaderName.Contains("launchpad"))
                                {
                                    if(m_lStages.Count == 3)
                                    {
                                        //m_lStages[0].SetSkip(true);
                                        //break;

                                        // you can break out after reading some of the stages and test
                                        // or you can set certain stages to skip rendering

                                        //break;
                                        //m_lStages[2].SetSkip(true);
                                    }
                                }

                                m_lStages[m_lStages.Count - 1].SetCustomRenderRules();
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
