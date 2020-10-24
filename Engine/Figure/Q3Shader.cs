using System;
using System.Collections.Generic;
using System.IO;

namespace engine
{
    public class DVBulge
    {
        public float m_bulgeWidth = 0f;
        public float m_bulgeHeight = 0f;
        public float m_bulgeSpeed = 0f;
    }

    public class DVMove
    {
        public float m_x = 0f;
        public float m_y = 0f;
        public float m_z = 0f;
    }

    public class DeformVertexes
    {
        public enum EDeformVType { INVALID, WAVE, BULGE, MOVE, AUTOSPRITE, AUTOSPRITE2 };

        public EDeformVType m_eType = EDeformVType.INVALID;

        public float m_div = 0f;

        public WaveForm m_wf = new WaveForm();

        public DVBulge m_Bulge = null;
        public DVMove m_Move = null;
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
                m_sShaderName.Contains("teleporter/energy") || m_sShaderName.Contains("pj_light") ||
                m_sShaderName.Contains("portal_sfx_ring"); // for example see big power reactor in dm0
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
                BitmapWrapper bmw = new BitmapWrapper(m_sLightImageFullPath);
                bmw.SetBitmapFromImageFile(m_bLightImageShouldBeTGA, m_sShaderName);
                float[] fCol = bmw.GetAverageColor255();

                sb.AppendLine("outputColor = vec4(" + Math.Round(fCol[0], 5) + "/255.0, " + Math.Round(fCol[1], 5) + "/255.0, " + Math.Round(fCol[2], 5) + "/255.0, 0.0);");
            }
            else if (m_bWater)
            {
                // q3 must be doing something special with water to make it look better.

                if (m_bWaterGLZERO)
                {
                    // more color and opaqueness because the blend functions are zeroing
                    sb.AppendLine("outputColor = vec4(1.0, 1.0, 1.0, .5);");
                }
                else // gl_one
                {
                    // less color and opaqueness because the blend functions are adding
                    sb.AppendLine("outputColor = vec4(0.1, 0.2, 0.15, 0.1);");
                }
            }
            else if(m_sShaderName.Contains("jesuswall"))
            {
                // special case for now. i think i should actually be doing this for all models which use rgbgen vertex
                // then i probably wouldn't have to scale their colors up later
                sb.AppendLine("outputColor = vec4(1.0);"); // black out outputColor to start
            }
            else
            {
                sb.AppendLine("outputColor = vec4(0.0);"); // black out outputColor to start
            }
        }

        public void GLDefineTextures()
        {
            foreach(Texture t in m_lStageTextures)
            {
                if(t != null)
                    t.GLDefineTexture();
            }
            foreach(Q3ShaderStage stage in m_lStages)
            {
                stage.GLDefineTextures();
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
                    Texture t = new Texture(stage.GetTexturePath());
                    m_lStageTextures.Add(t);
                    bool bShouldBeTGA = false;
                    t.SetClamp(stage.GetClampmap());
                    string sFullTexPath = GetPathToTextureNoShaderLookup(false, stage.GetTexturePath(), ref bShouldBeTGA);
                    t.SetFullPath(sFullTexPath);
                    t.SetShouldBeTGA(bShouldBeTGA);
                    t.SetTexture(m_sShaderName);

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

                ScaleTexel(stage, sIndex, sb);

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

                sb.AppendLine("// ## STAGE " + i + " ##");

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
            else return GameGlobals.GetBaseVertexColorScale();
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
            else return GameGlobals.GetBaseLightmapScale();
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

            // final clamp
            sb.AppendLine("outputColor = clamp(outputColor, 0.0, 1.0);");
        }

        public static void ReadQ3ShaderContentOnceAtStartup(Dictionary<string, List<string>> dictShaderNameToShaderContent, Zipper zip)
        {
            zip.ExtractAllShaderFiles();

            string[] shaders = Directory.GetFiles(Path.Combine(PATHS.GetTempDir, "scripts"), "*.shader");

            try
            {
                foreach (string sShaderFile in shaders)
                {
                    StreamReader sr = new StreamReader(sShaderFile);
                    string sCurrentShaderName = "";
                    int nCurlyCounter = 0;

                    while (!sr.EndOfStream)
                    {
                        string sLine = sr.ReadLine();

                        if(sLine.Contains("//"))
                        {
                            sLine = sLine.Substring(0, sLine.IndexOf("//"));
                        }

                        string sLineTrimmed = sLine.Trim().ToLower();

                        if (sLine.Contains("//") || string.IsNullOrEmpty(sLineTrimmed)) continue;

                        if (sLine.Contains("{"))
                        {
                            nCurlyCounter++;

                            if (nCurlyCounter == 1)
                            {
                                // new q3 shader

                                // HANDLE DUPLICATE SHADERS SOMEHOW. REPLACE. I ran into three and then decided to just overwrite anymore i find. will test later.
                                if(dictShaderNameToShaderContent.ContainsKey(sCurrentShaderName))
                                {
                                    dictShaderNameToShaderContent.Remove(sCurrentShaderName);
                                }

                                System.Diagnostics.Debug.Assert(dictShaderNameToShaderContent.ContainsKey(sCurrentShaderName) == false);
                                dictShaderNameToShaderContent[sCurrentShaderName] = new List<string>();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(dictShaderNameToShaderContent.ContainsKey(sCurrentShaderName) == true);                                
                            }

                            dictShaderNameToShaderContent[sCurrentShaderName].Add(sLineTrimmed);
                        }
                        else if (sLine.Contains("}"))
                        {
                            nCurlyCounter--;

                            if (nCurlyCounter == 0)
                            {
                                // end of current q3 shader
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(dictShaderNameToShaderContent.ContainsKey(sCurrentShaderName) == true);
                            }

                            dictShaderNameToShaderContent[sCurrentShaderName].Add(sLineTrimmed);
                        }
                        else if (nCurlyCounter == 0) // new q3 shader name
                        {
                            sCurrentShaderName = sLineTrimmed;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(dictShaderNameToShaderContent.ContainsKey(sCurrentShaderName) == true);
                            dictShaderNameToShaderContent[sCurrentShaderName].Add(sLineTrimmed);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "\n\n" + e.StackTrace);
            }
        }

        public void Delete()
        {
            foreach (Texture t in m_lStageTextures)
            {
                if (t != null)
                {
                    if(!t.Deleted()) // it could be deleted if it's a lightmap
                        t.Delete();
                    else
                    {
                        // assure lightmap
                        if (!t.GetPath().Contains(".png")) throw new Exception("Only okay to try to delete something twice if it's a lightmap. This is not.");
                        // also ok to delete something twice in case of animmap textures because they are located in two lists
                        // the stage list has one and the animmap list has all of them. this is handled by setting wrapper to null now upon
                        // first delete
                    }
                }
            }
            foreach (Q3ShaderStage s in m_lStages) s.Delete();
        }

        /// <summary>
        /// Reads the shader files and finds the right texture
        /// </summary>
        /// <returns></returns>
        public void ReadQ3Shader(string sPathFromVRML)
        {          
            string sPathNoExt = sPathFromVRML.Substring(0, sPathFromVRML.IndexOf(".")).ToLower();
            List<string> lShaderLines;
            bool bFound = GameGlobals.m_dictQ3ShaderContent.TryGetValue(sPathNoExt, out lShaderLines);

            if (bFound)
            {
                m_sShaderName = sPathNoExt;
                int nCurlyCounter = 0;
                for (int i = 0; i < lShaderLines.Count; i++)
                {
                    string sInsideTargetShaderLine = lShaderLines[i];

                    // read surface parameters
                    if (sInsideTargetShaderLine.Contains("surfaceparm"))
                    {
                        if (sInsideTargetShaderLine.Contains("metalsteps"))
                        {
                            m_eStepType = EStepType.METAL;
                        }
                        else if (sInsideTargetShaderLine.Contains("nosteps"))
                        {
                            m_eStepType = EStepType.NONE;
                        }
                        else if (sInsideTargetShaderLine.Contains("trans"))
                        {
                            m_bTrans = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("alphashadow"))
                        {
                            m_bAlphaShadow = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("lava"))
                        {
                            m_bLava = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("slime"))
                        {
                            m_bSlime = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("nonsolid"))
                        {
                            m_bNonSolid = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("water"))
                        {
                            m_bWater = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("sky"))
                        {
                            m_bSky = true;
                        }
                        else if (sInsideTargetShaderLine.Contains("fog"))
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
                    else if (sInsideTargetShaderLine.Contains("q3map_lightimage"))
                    {
                        // this affects the initial color of outputColor
                        // it sets it to the average color of the image

                        string[] tokens = GetTokens(sInsideTargetShaderLine);

                        bool bShouldBeTGA = false;
                        string sTexPath = GetPathToTextureNoShaderLookup(false, tokens[1], ref bShouldBeTGA);
                        if (File.Exists(sTexPath))
                        {
                            m_bLightImageShouldBeTGA = bShouldBeTGA;
                            m_sLightImageFullPath = sTexPath;
                        }
                    }
                    else if (sInsideTargetShaderLine.Contains("deformvertexes"))
                    {
                        string[] tokens = sInsideTargetShaderLine.Trim().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        DeformVertexes dv = new DeformVertexes();
                        if (tokens.Length > 1)
                        {
                            if (tokens[1] == "wave") // only handle wave for now
                            {
                                dv.m_div = Convert.ToSingle(tokens[2]);
                                dv.m_eType = DeformVertexes.EDeformVType.WAVE;
                                Q3ShaderStage.SetWaveForm(dv.m_wf, tokens, 3);
                                m_lDeformVertexes.Add(dv);
                            }
                            else if (tokens[1] == "bulge")
                            {
                                // deformvertexes bulge 3 10 1
                                dv.m_Bulge = new DVBulge();
                                dv.m_Bulge.m_bulgeWidth = Convert.ToSingle(tokens[2]);
                                dv.m_Bulge.m_bulgeHeight = Convert.ToSingle(tokens[3]);
                                dv.m_Bulge.m_bulgeSpeed = Convert.ToSingle(tokens[4]);
                                dv.m_eType = DeformVertexes.EDeformVType.BULGE;
                                m_lDeformVertexes.Add(dv);
                            }
                            else if (tokens[1] == "move")
                            {
                                // deformVertexes move 0 0 3   sin 0 5 0 0.1
                                dv.m_Move = new DVMove();
                                dv.m_Move.m_x = Convert.ToSingle(tokens[2]);
                                dv.m_Move.m_y = Convert.ToSingle(tokens[3]);
                                dv.m_Move.m_z = Convert.ToSingle(tokens[4]);
                                dv.m_eType = DeformVertexes.EDeformVType.MOVE;
                                Q3ShaderStage.SetWaveForm(dv.m_wf, tokens, 5);
                                m_lDeformVertexes.Add(dv);
                            }
                            else if(tokens[1] == "autosprite") 
                            {
                                dv.m_eType = DeformVertexes.EDeformVType.AUTOSPRITE;
                                m_lDeformVertexes.Add(dv);
                            }
                            else if (tokens[1] == "autosprite2") 
                            {
                                /*dv.m_eType = DeformVertexes.EDeformVType.AUTOSPRITE2;
                                m_lDeformVertexes.Add(dv);*/
                            }
                            else
                            {
                                if (tokens[1] != "normal") // not doing autosprite2 or normal right now  
                                    throw new Exception("Found deformvertexes with type " + tokens[1]);
                            }
                        }
                    }

                    // begin stage found
                    // sometimes there is shader content after the open stage curly on the same line
                    if (sInsideTargetShaderLine.Contains("{")) 
                    {
                        nCurlyCounter++;

                        if (nCurlyCounter > 1)
                        {
                            m_lStages.Add(new Q3ShaderStage(this));
                        }

                        // remove open curly from line
                        int nCurIndex = sInsideTargetShaderLine.IndexOf("{");
                        sInsideTargetShaderLine = sInsideTargetShaderLine.Substring(nCurIndex + 1);
                    }

                    if (nCurlyCounter > 1)
                    {
                        if (sInsideTargetShaderLine.Contains("animmap")) // this needs to be before the map texture one
                        {
                            string[] tokens = GetTokens(sInsideTargetShaderLine);
                            m_lStages[m_lStages.Count - 1].SetAnimmap(GetTokensAfterFirst(tokens));
                        }
                        // read stage items
                        else if (IsMapTexture(sInsideTargetShaderLine))
                        {
                            string[] tokens = GetTokens(sInsideTargetShaderLine); 
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
                            if (Single.TryParse(sRotate, out fRotate))
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
                            m_lStages[m_lStages.Count - 1].SetTCModScale(GetTokensAfterSecond(tokens));
                        }
                        else if(sInsideTargetShaderLine.Contains("tcmod transform"))
                        {
                            string[] tokens = GetTokens(sInsideTargetShaderLine);
                            m_lStages[m_lStages.Count - 1].SetTCModTransform(GetTokensAfterSecond(tokens));
                        }
                        else if (sInsideTargetShaderLine.Contains("tcgen environment"))
                        {
                            m_lStages[m_lStages.Count - 1].SetTCGEN_CS("environment");
                        }
                        else if (sInsideTargetShaderLine.Contains("$lightmap"))
                        {
                            // for example the jesus wall has a lightmap stage but there is no lightmap
                            // try ignoring this then
                            if (m_pParent.GetLightmapTexture() != null)
                                m_lStages[m_lStages.Count - 1].SetLightmap(true);
                            else
                                m_lStages[m_lStages.Count - 1].SetSkip(true);
                        }

                        // end stage reading                            
                        if (sInsideTargetShaderLine.Contains("}")) // end of stage
                        {
                            nCurlyCounter--;

                            if (nCurlyCounter == 0)
                                break; // end of shader

                            // this is a good spot to exit out of the shader reading process to debug shaders
                            // exit out after stages one by one to test stages one by one
                            if (m_sShaderName.Contains("largerblock3b_ow"))
                            {
                                if (m_lStages.Count == 1)
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
                }
            }
        }

        public static string[] GetTokens(string sInsideTargetShaderLine)
        {
            string sTrimmed = sInsideTargetShaderLine.Trim();
            return sTrimmed.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool AutoSpriteEnabled()
        {
            for(int i = 0; i < m_lDeformVertexes.Count; i++)
            {
                if (m_lDeformVertexes[i].m_eType == DeformVertexes.EDeformVType.AUTOSPRITE || 
                    m_lDeformVertexes[i].m_eType == DeformVertexes.EDeformVType.AUTOSPRITE2) 
                    return true;
            }
            return false;
        }

        private void ScaleTexel(Q3ShaderStage stage, string sIndex, System.Text.StringBuilder sb)
        {
            // only for darkening comp3 at the moment. it's too bright because the env texture is just too bright i think.
            // not sure why

            if(stage.GetTexturePath().Contains("base_wall/comp3env"))
            {
                //sb.AppendLine("texel" + sIndex + " *= 0.1;");
            }
        }

    }
}
