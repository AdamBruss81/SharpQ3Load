using System;
using System.Collections.Generic;
using System.IO;

namespace engine
{
    public class DeformVertexes
    {
        public enum EDeformVType { INVALID, WAVE };

        public EDeformVType m_eType = EDeformVType.INVALID;

        public float m_div = 0f;
        public WaveForm m_wf = new WaveForm();
    }

    public class Q3Shader
    {
        public enum EStepType { DEFAULT, METAL, NONE };

        List<Q3ShaderStage> m_lStages = new List<Q3ShaderStage>();
        List<DeformVertexes> m_lDeformVertexes = new List<DeformVertexes>();
        EStepType m_eStepType = EStepType.DEFAULT;
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
        bool m_bSlime = false;
        bool m_bSky = false;
        bool m_bNonSolid = false;
        bool m_bWater = false;
        bool m_bWaterGLZERO = true;
        bool m_bFog = false;
        bool m_bLightImageShouldBeTGA = false;
        string m_sSort = "";

        // static methods
        public static string GetSampler2DName() { return "sampler2D"; }
        // ***

        public Q3Shader(Shape parent)
        {
            m_pParent = parent;
        }

        public List<DeformVertexes> GetDeformVertexes() { return m_lDeformVertexes; }

        public string GetSort()
        {
            return m_sSort;
        }

        public bool GetAddAlpha()
        {
            /* i still need this function to know when to enable opengl pipeline blending for a shape's rendering */
            // these checks are based on shader name and if there is only one stage

            bool bAA = false;            

            // for these i can't tell by looking at the shader if they need to be see through so hardcoding for now
            bAA = m_sShaderName.Contains("sfx/teslacoil") || m_sShaderName.Contains("console/centercon") ||
                m_sShaderName.Contains("teleporter/energy") || m_sShaderName.Contains("pj_light"); // for example see big power reactor in dm0
            // pj_light is ball light in brimstone abbey

            if (!bAA)
            {
                for (int i = 0; i < m_lStages.Count; i++)
                {
                    if (m_lStages[i].GetAlphaFunc() != "")
                    {
                        bAA = true;
                        break;
                    }
                }
            }

            if (!bAA) bAA = m_bAlphaShadow;
            if (!bAA) bAA = m_bTrans;
            if (!bAA)
            {                
                if ((m_lStages.Count == 1 && m_lStageTextures[0] != null) && (m_lStageTextures[0].GetShouldBeTGA() || m_lStageTextures[0].IsTGA()) && m_lStages[0].GetBlendFunc() != "")
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

            if (m_bLava || m_bSlime) bAA = false; // not sure why trans is set on lava shaders sometimes

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

        public bool UsesTcgen()
        {
            for(int i = 0; i < m_lStages.Count; i++)
            {
                if (m_lStages[i].GetTCGEN_CS() == "environment") return true;
            }
            return false;
        }

        public bool UsesAlphaGenspec()
        {
            for (int i = 0; i < m_lStages.Count; i++)
            {
                if (m_lStages[i].GetAlphaGenFunc() == "lightingspecular") return true;
            }
            return false;
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
                        else if (sFullPath.Contains("stars"))
                        {
                            // this is the lightimage for the dm10 sky. i think the sky is somehow broken up into multiple
                            // images in env. see other images named space*. im just taking one for now
                            // no indication in shaders or manual for how this works
                            // i think for this particular case i need to add the lightimage as a first stage
                            // and then blend into it with the clouds. but usually with lightimages you 
                            // use them as the initial color for outputColor by taking average color.
                            // im not going to hardcode something for this case at this time.
                            sFullPath = m_zipper.ExtractSoundTextureOther("env/space1_bk.jpg");
                        }
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
                float[] fCol = Texture.GetAverageColor255(m_sLightImageFullPath, m_bLightImageShouldBeTGA);

                sb.AppendLine("outputColor = vec4(" + Math.Round(fCol[0], 5) + "/255.0, " + Math.Round(fCol[1], 5) + "/255.0, " + Math.Round(fCol[2], 5) + "/255.0, 0.0);");
            }
            else if (m_bWater)
            {
                if (m_bWaterGLZERO)
                {
                    // more color and opaqueness because the blend functions are zeroing
                    sb.AppendLine("outputColor = vec4(1.0, 1.0, 1.0, .3);");
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
        public void ConvertQ3ShaderToGLSL(System.Text.StringBuilder sb)
        {
            // add samplers to frag shader based on stage count
            for (int i = 0; i < m_lStages.Count; i++)
            {
                sb.AppendLine("uniform " + GetSampler2DName() + " texture" + Convert.ToString(i) + ";");
            }

            sb.AppendLine("");

            bool bAddTime = false;
            bool bSendSinTable = false;
            bool bMultipleAlphaFuncs = false;
            int nNumAlphaFuncs = 0;

            // add uniforms
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                string sIndex = Convert.ToString(i);

                if (stage.GetAlphaFunc() != "") nNumAlphaFuncs++;

                // rgbgen - at some point change rgbgen to a single value i think
                if (!stage.IsRGBGENIdentity() && !stage.IsVertexColor())
                {
                    sb.AppendLine("uniform vec4 rgbgen" + sIndex + ";");
                }
                if(stage.GetAlphaGenFunc() == "wave")
                {
                    sb.AppendLine("uniform float alphagen" + sIndex + ";");
                }                                 
            }

            bMultipleAlphaFuncs = nNumAlphaFuncs > 1;

            sb.AppendLine("");

            // create main function
            sb.AppendLine("");
            sb.AppendLine("void main()");
            sb.AppendLine("{");            

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
                    string sTemp = GetPathToTextureNoShaderLookup(false, stage.GetTexturePath(), ref bTGA);
                    m_lStageTextures[m_lStageTextures.Count - 1].SetTexture(sTemp, bTGA, m_sShaderName);
                    m_lStageTextures[m_lStageTextures.Count - 1].SetShouldBeTGA(bTGA);                    

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCGEN_CS() == "environment") sTexCoordName = "tcgenEnvTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex; // this can have started with tcgen environment already

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");
                }
                else if (stage.IsAnimmap())
                {
                    m_lStageTextures.Add(stage.GetAnimmapTexture()); // initial texture, will change as time passes

                    string sTexCoordName = "mainTexCoord";
                    if (stage.GetTCMODS().Count > 0) sTexCoordName = "texmod" + sIndex;

                    sb.AppendLine("vec4 texel" + sIndex + " = texture(texture" + sIndex + ", " + sTexCoordName + ");");
                }
                else
                {
                    m_lStageTextures.Add(null); // need a placeholder here, will check for null later
                }

                sb.AppendLine("");
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

            sb.AppendLine("// ### " + m_lStages.Count + " STAGES ###");
            sb.AppendLine("");

            DefineInitialOutputColor(sb);

            sb.AppendLine("");

            System.Text.StringBuilder sbOutputline = new System.Text.StringBuilder();
            for (int i = 0; i < m_lStages.Count; i++)
            {
                Q3ShaderStage stage = m_lStages[i];
                if (stage.Skip()) continue;
                
                string sLightmapScale = stage.GetLightmap() ? " * " + GetLightmapScale() : "";
                string sVertexColScale = GetVertexColScale();

                string sIndex = Convert.ToString(i);
                string sTexel = "texel" + sIndex;
                string sBlendFunc = stage.GetBlendFunc();

                // alphaGen
                if (stage.GetAlphaGenFunc() == "lightingspecular")
                {
                    sb.AppendLine(sTexel + ".w *= alphaGenSpecular;"); // this doesn't work exactly right but it has 
                    // a dramatic positive effect on some maps like the bouncy map(floor)
                }
                else if(stage.GetAlphaGenFunc() == "wave")
                {
                    sb.AppendLine(sTexel + ".w *= alphagen" + sIndex + ";");
                }

                // start forming outputColor
                string sSource = sTexel;
                if (!stage.IsRGBGENIdentity() && !stage.IsVertexColor())
                {
                    sSource = "(" + sTexel + " * rgbgen" + sIndex + ")";
                }

                if (bMultipleAlphaFuncs) {
                    sb.AppendLine("bool bPassAlphaTest_" + i + " = true;");

                    // perform alpha func on texel and skip outputColor potentially
                    if (stage.GetAlphaFunc() == "ge128")
                    {
                        sb.AppendLine("bPassAlphaTest_" + i + " = " + sSource + ".w >= 0.5;");
                    }
                    else if (stage.GetAlphaFunc() == "gt0")
                    {
                        sb.AppendLine("bPassAlphaTest_" + i + " = " + sSource + ".w > 0.0;");
                    }
                    else if (stage.GetAlphaFunc() == "lt128")
                    {
                        sb.AppendLine("bPassAlphaTest_" + i + " = " + sSource + ".w < 0.5;");
                    }
                }
               
                if(bMultipleAlphaFuncs)
                {
                    sb.AppendLine("if(bPassAlphaTest_" + i + ") {");
                }
                
                // blend functions in q3
                if (sBlendFunc == "gl_dst_color gl_zero" || sBlendFunc == "filter") // src * dest
                {
                    sb.AppendLine("outputColor *= " + sSource + sLightmapScale + ";");
                }
                else if (sBlendFunc == "gl_one gl_one" || sBlendFunc == "add") // src + dest
                {
                    sb.AppendLine("outputColor += (" + sSource + ")" + sLightmapScale + ";");
                }
                else if (sBlendFunc == "gl_src_alpha gl_one_minus_src_alpha" || sBlendFunc == "blend") // mix
                {
                    sb.AppendLine("outputColor = mix(outputColor, " + sSource + ", " + sSource + ".w);");
                }
                else if (sBlendFunc == "gl_one_minus_src_alpha gl_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sSource + " * (1 - " + sSource + ".w) + outputColor * " + sSource + ".w;");
                }
                else if (sBlendFunc == "gl_dst_color gl_one_minus_dst_alpha")
                {
                    sb.AppendLine("outputColor = (" + sSource + " * outputColor" + sLightmapScale + " + outputColor * (1 - outputColor.w));");
                }
                else if (sBlendFunc == "gl_one gl_zero")
                {
                    sb.AppendLine("outputColor += (" + sSource + ");");
                }
                else if (sBlendFunc == "gl_dst_color gl_one")
                {
                    sb.AppendLine("outputColor = " + sSource + " * outputColor + outputColor;");
                }
                else if (sBlendFunc == "gl_dst_color gl_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sSource + " * outputColor + outputColor * " + sSource + ".w;");
                }
                else if (sBlendFunc == "gl_one gl_one_minus_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sSource + " + outputColor * (1 - " + sSource + ".w);");
                }
                else if (sBlendFunc == "gl_zero gl_one_minus_src_color")
                {
                    sb.AppendLine("outputColor = outputColor * (1 - " + sSource + ");");
                } 
                else if(sBlendFunc == "gl_zero gl_src_color")
                {
                    sb.AppendLine("outputColor = outputColor * (" + sSource + ");");
                }
                else if(sBlendFunc == "gl_zero gl_src_alpha")
                {
                    sb.AppendLine("outputColor = outputColor * (" + sSource + ".w);");
                }
                else if (sBlendFunc == "gl_one gl_src_alpha")
                {
                    sb.AppendLine("outputColor = " + sSource + " + outputColor * (" + sSource + ".w);");
                }
                else if(sBlendFunc.Contains("gl_"))
                {
                    throw new Exception("Unknown blend function encountered. Provide an implementation for " + sBlendFunc);
                }                
                else if (!stage.IsVertexColor())
                {
                    // default blend function is add
                    sb.AppendLine("outputColor += (" + sSource + ")" + sLightmapScale + ";");
                }
                // end blend functions

                if (stage.IsVertexColor()) // always do this if it is in the shader stage. see horned shader model in dm3
                {
                    if (sBlendFunc == "")
                        sb.AppendLine("outputColor += (" + sTexel + " * color * " + sVertexColScale + ");");
                    else
                        sb.AppendLine("outputColor *= (color * " + sVertexColScale + ");");
                }

                // clamp colors that are over 1.0
                if (stage.IsRGBGENIdentity() && stage.GetLightmap()) { }
                else if(i < m_lStages.Count - 1) sb.AppendLine("outputColor = clamp(outputColor, 0.0, 1.0);");

                if(bMultipleAlphaFuncs)
                {
                    sb.AppendLine("}");
                }
                else
                {
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
                }

                sb.AppendLine("");                
            }

            CustomizeFinalOutputColor(sb);

            // end main
            sbOutputline.AppendLine("}");

            // add helpful comments
            sbOutputline.AppendLine("");
            if (GetAddAlpha()) sbOutputline.AppendLine("// open gl blending enabled");
            if (m_sLightImageFullPath != "") sbOutputline.AppendLine("// light image is " + m_sLightImageFullPath);

            sb.Append(sbOutputline.ToString());
        }

        private string GetVertexColScale()
        {
            if (m_sShaderName.Contains("base_wall/protobanner"))
            {
                return "2.0";
            }
            else return "3.0";
        }

        /// <summary>
        /// This is experimental. Lighting does not work exactly like it does in q3. I scale lightmaps by 3.0. But it's too bright in some
        /// cases. Try lowering it in those cases.
        /// </summary>
        /// <returns></returns>
        private string GetLightmapScale()
        {
            if (m_sShaderName.Contains("base_wall/protobanner"))
            {
                return "2.0";
            }
            else return "3.0";
        }

        private void CustomizeFinalOutputColor(System.Text.StringBuilder sb)
        {
            if(m_sShaderName.Contains("pj_light"))
            {
                // the q3 shader for pj_light does not represent what it looks like in q3
                // customizing it here
                sb.AppendLine("outputColor.xyz *= 3.0;");
                sb.AppendLine("outputColor.w = 0.5;");
            }
            else if(m_sShaderName.Contains("jesuswall"))
            {
                sb.AppendLine("outputColor *= 4.0;"); // jesus is too dark so brighten him up
            }

            // final clamp
            sb.AppendLine("outputColor = clamp(outputColor, 0.0, 1.0);");
        }     

        /// <summary>
        /// Reads the shader files and finds the right texture
        /// </summary>
        /// <returns></returns>
        public void ReadQ3Shader(string sPathFromVRML)
        {            
            List<string> lsShaderFilenames = GetShaderFileName(sPathFromVRML);

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
                                else if (sInsideTargetShaderLine.Contains("slime"))
                                {
                                    m_bSlime = true;
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
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_sCull = GetTokensAfterFirst(tokens);                                
                            }
                            else if(sInsideTargetShaderLine.Contains("q3map_lightimage"))
                            {
                                // this affects the initial color of outputColor
                                // it sets it to the average color of the image

                                string[] tokens = GetTokens(sInsideTargetShaderLine);

                                bool bShouldBeTGA = false;
                                string sTexPath = GetPathToTextureNoShaderLookup(false, tokens[1], ref bShouldBeTGA);
                                if(File.Exists(sTexPath))
                                {
                                    m_bLightImageShouldBeTGA = bShouldBeTGA;
                                    m_sLightImageFullPath = sTexPath;
                                } 
                            }
                            else if(sInsideTargetShaderLine.Contains("deformvertexes"))
                            {
                                string[] tokens = sInsideTargetShaderLine.Trim().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                DeformVertexes dv = new DeformVertexes();
                                if(tokens.Length > 1)
                                {
                                    if (tokens[1] == "wave") // only handle wave for now
                                    {
                                        dv.m_div = Convert.ToSingle(tokens[2]);
                                        dv.m_eType = DeformVertexes.EDeformVType.WAVE;
                                        Q3ShaderStage.SetWaveForm(dv.m_wf, tokens, 3);
                                        m_lDeformVertexes.Add(dv);
                                    }
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
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetAnimmap(GetTokensAfterFirst(tokens));
                            }
                            // read stage items
                            else if (IsMapTexture(sInsideTargetShaderLine))
                            {
                                string[] tokens = sInsideTargetShaderLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length == 2)
                                {
                                    if (tokens[0].Trim(new char[] { '\t' }) == "map" || tokens[0].Trim(new char[] { '\t' }) == "clampmap")
                                    {
                                        m_lStages[m_lStages.Count - 1].SetTexturePath(tokens[1]);
                                        if (sInsideTargetShaderLine.Contains("clampmap"))
                                        {
                                            m_lStages[m_lStages.Count - 1].SetClampmap(true);
                                        }
                                    }
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("rgbgen"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetRGBGEN(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("blendfunc"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetBlendFunc(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("alphagen"))
                            {
                                if (!sInsideTargetShaderLine.Contains("portal")) // don't handle portal
                                {
                                    string[] tokens = GetTokens(sInsideTargetShaderLine);
                                    m_lStages[m_lStages.Count - 1].SetAlphaGen(GetTokensAfterFirst(tokens));
                                }
                            }
                            else if (sInsideTargetShaderLine.Contains("alphafunc"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetAlphaFunc(GetTokensAfterFirst(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod scroll"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetTCModScroll(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod turb"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetTCModTurb(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod rotate"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);

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
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
                                m_lStages[m_lStages.Count - 1].SetTCMODStretch(GetTokensAfterSecond(tokens));
                            }
                            else if (sInsideTargetShaderLine.Contains("tcmod scale"))
                            {
                                string[] tokens = GetTokens(sInsideTargetShaderLine);
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
                                if(m_sShaderName.Contains("comp3"))
                                {
                                    if(m_lStages.Count == 2)
                                    {
                                        //m_lStages[1].SetSkip(true);
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

                if (!string.IsNullOrEmpty(m_sShaderName)) break;
            }
        }

        private string[] GetTokens(string sInsideTargetShaderLine)
        {
            string sTrimmed = sInsideTargetShaderLine.Trim();
            return sTrimmed.Split(new Char[] { ' ', '\t' });
        }
    }
}
