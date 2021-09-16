﻿using System;
using System.Collections.Generic;
using System.IO;

namespace engine
{
    public abstract class TCMOD
    {
        public enum ETYPE { SCROLL, SCALE, TURB, ROTATE, STRETCH, TRANSFORM };

        public abstract ETYPE GetModType();
    }

    class TCTURB : TCMOD
    {
        public float amplitude = 0f;
        public float phase = 0f;
        public float frequency = 0f;

        public override ETYPE GetModType() { return ETYPE.TURB; }
    }

    class TCSCROLL : TCMOD
    {
        public float s = 0f;
        public float t = 0f;

        public override ETYPE GetModType() { return ETYPE.SCROLL; }
    }

    class TCTRANSFORM : TCMOD
    {
        public float m00, m01, m10, m11, t0, t1;

        public override ETYPE GetModType() { return ETYPE.TRANSFORM; }
    }

    class TCSCALE : TCMOD
    {
        public float s = 1f;
        public float t = 1f;

        public override ETYPE GetModType() { return ETYPE.SCALE; }
    }

    class TCROTATE : TCMOD
    {
        public float degPerSecond;
        public override ETYPE GetModType() { return ETYPE.ROTATE; }
    }

    class TCSTRETCH : TCMOD
    {
        public WaveForm wf = new WaveForm();
        public override ETYPE GetModType() { return ETYPE.STRETCH; }
    }

    public class GEN
    {
        public enum ETYPE { IDENTITY, VERTEX, CONSTANT, WAVEFORM, LIGHTING_SPECULAR };

        public ETYPE m_eType = ETYPE.IDENTITY;
        public WaveForm wf = new WaveForm();
        public float[] m_fConst = new float[3];
    }

    public class WaveForm
    {
        public string func;
        public float fbase = 0f;
        public float amp = 0f;
        public float phase = 0f;
        public float freq = 0f;
    }

    public class Q3ShaderStage
    {
        // read in members
        string m_sTexturePath = "";
        string m_sBlendFunc = "";
        string m_sAlphaFunc = "";
        string m_sTCGEN_CS = "";
        string m_sAnimmapIntervalInput = "";
        bool m_bLightmap = false;
        bool m_bSkip = false; // for debugging
        bool m_bClampmap = false;
        TCTURB m_turb = new TCTURB();
        TCSCALE m_scale = new TCSCALE();
        TCSCROLL m_scroll = new TCSCROLL();
        TCROTATE m_rotate = new TCROTATE();
        TCSTRETCH m_stretch = new TCSTRETCH();
        TCTRANSFORM m_transform = new TCTRANSFORM();
        GEN m_rgbgen = new GEN();
        GEN m_alphagen = new GEN();
        Q3Shader m_ParentShader = null;
        List<TCMOD> m_TCMODS = new List<TCMOD>();
        List<Texture> m_lAnimmapTextures = new List<Texture>();
        int m_nCurAnimmapTextureIndex = 0;
        float m_fSecondsPerAnimmapTexture = 0f;
        float m_fLastAnimmapTextureChangeTime = 0f;

        bool m_bSyncRGBGENandAnimmap = false;
        bool m_bSyncRGBGENandTCMOD = false;
        bool m_bSyncTCMODandAnimmap = false;

        bool m_bSquareOnOff = true;
        float m_fPrevRGBGENWaveformVal = 0f;
        float m_fPrevRGBGENandTCMODVal = 0f;

        public Q3ShaderStage(Q3Shader container) { m_ParentShader = container; }

        public bool Skip() { return m_bSkip; }
        public string GetTCGEN_CS() { return m_sTCGEN_CS; }
        public void SetTCGEN_CS(string s) { m_sTCGEN_CS = s; }
        public void SetBlendFunc(string s) { m_sBlendFunc = s; }
        public void SetClampmap(bool b) { m_bClampmap = b; }
        public bool GetClampmap() { return m_bClampmap; }
        public void SetAlphaFunc(string s) { m_sAlphaFunc = s; }
        
        public void SetSkip(bool b) { m_bSkip = b; }
        public bool IsRGBGENIdentity()  
        {
            return (m_rgbgen.m_eType == GEN.ETYPE.IDENTITY);
        }
        public void SetCustomRenderRules()
        {
            // for sync of animmap and rgbgen waveforms ...
            if (IsAnimmap() && (m_rgbgen.wf.func == "inversesawtooth" || m_rgbgen.wf.func == "sawtooth")) // expand this to sawtooth as well
            {
                if(Convert.ToSingle(m_sAnimmapIntervalInput) == m_rgbgen.wf.freq)
                {
                    m_bSyncRGBGENandAnimmap = true;
                }
                // this was originally for the quake3 letter flashing sign but it helps with flames too
            }

            // for sync of rgb and tcmod
            if(m_stretch.wf.func == "sin" && m_rgbgen.wf.func == "square")
            {
                // originally for jump pads
                if(m_stretch.wf.freq == m_rgbgen.wf.freq)
                {
                    m_bSyncRGBGENandTCMOD = true; // this and its use are really a special case hack
                    // i should make this more generic. q3 has tables
                }
            }

            bool bScroll = false;
            for(int i = 0; i < m_TCMODS.Count; i++)
            {
                if(m_TCMODS[i].GetModType() == TCMOD.ETYPE.SCROLL)
                {
                    bScroll = true;
                    break;
                }
            }         

            if(IsAnimmap() && bScroll)
            {
                // assume these should be synced
                //m_bSyncTCMODandAnimmap = true; // couldn't get this to work. revisit later. so launchpads won't look right for now.
                // change animmap texture based on scroll status
            }

            // some hardcoding above for now. if this kind of thing keeps happening while loading maps I'll make it more
            // generic
        }

        public bool IsAnimmap() { return m_lAnimmapTextures.Count > 0; }

        public Texture GetAnimmapTexture()
        {
            if (!m_bSyncRGBGENandAnimmap && !m_bSyncTCMODandAnimmap)
            {
                if (m_fLastAnimmapTextureChangeTime == 0f) m_fLastAnimmapTextureChangeTime = GameGlobals.GetElapsedS();
                else if (GameGlobals.GetElapsedS() - m_fLastAnimmapTextureChangeTime >= m_fSecondsPerAnimmapTexture)
                {
                    m_nCurAnimmapTextureIndex++;
                    if (m_nCurAnimmapTextureIndex > m_lAnimmapTextures.Count - 1) m_nCurAnimmapTextureIndex = 0;

                    m_fLastAnimmapTextureChangeTime = GameGlobals.GetElapsedS();
                }
            }

            return m_lAnimmapTextures[m_nCurAnimmapTextureIndex];
        }

        public List<TCMOD> GetTCMODS() { return m_TCMODS; }

        public void SetAlphaGen(string s)
        {
            SetGEN(m_alphagen, s);
        }

        public void SetRGBGEN(string s)
        {
            SetGEN(m_rgbgen, s);
        }

        public static void SetWaveForm(WaveForm wf, string[] tokens, int nFirst)
        {
            wf.func = tokens[nFirst].ToLower();

            // initial value
            wf.fbase = System.Convert.ToSingle(tokens[nFirst+1]);

            // amplitude
            wf.amp = System.Convert.ToSingle(tokens[nFirst+2]);

            // phase
            wf.phase = System.Convert.ToSingle(tokens[nFirst+3]);

            // peaks per second
            wf.freq = System.Convert.ToSingle(tokens[nFirst+4]);
        }

        public static void SetGEN(GEN gen, string sInput)
        {
            string[] tokens = sInput.Split(' ');

            if(tokens.Length == 1)
            {
                if(tokens[0].ToLower() == "vertex")
                {
                    gen.m_eType = GEN.ETYPE.VERTEX;
                } 
                else if(tokens[0].ToLower() == "lightingspecular")
                {
                    gen.m_eType = GEN.ETYPE.LIGHTING_SPECULAR;
                }
                else if(tokens[0].ToLower() != "identity" && tokens[0].ToLower() != "identitylighting" && tokens[0].ToLower() != "exactvertex") // these are basically ones that don't need any special handling(i think)
                {
                    throw new Exception("Encountered unknown gen type : " + tokens[0]);
                }
            }
            else if (tokens.Length > 1) 
            {
                // known kinds at this point : waveform and constant
                if (tokens[0].ToLower() == "wave")
                {
                    gen.m_eType = GEN.ETYPE.WAVEFORM;
                    SetWaveForm(gen.wf, tokens, 1);
                }
                else if(tokens[0].ToLower() == "const")
                {                   
                    gen.m_eType = GEN.ETYPE.CONSTANT;
                    if (tokens.Length == 2)
                    {
                        gen.m_fConst[0] = Convert.ToSingle(tokens[1]);
                        gen.m_fConst[1] = Convert.ToSingle(tokens[1]);
                        gen.m_fConst[2] = Convert.ToSingle(tokens[1]);
                    }
                    else
                    {
                        gen.m_fConst[0] = Convert.ToSingle(tokens[2]);
                        gen.m_fConst[1] = Convert.ToSingle(tokens[3]);
                        gen.m_fConst[2] = Convert.ToSingle(tokens[4]);
                    }
                }
                else
                {
                    throw new Exception("Encountered unknown gen type : " + tokens[0]);
                }
            }
        }

        public bool IsVertexColor() { return m_rgbgen.m_eType == GEN.ETYPE.VERTEX; }

        public string GetBlendFunc() { return m_sBlendFunc; }
        public string GetAlphaFunc() { return m_sAlphaFunc; }
        public GEN.ETYPE GetAlphaGenFunc() { return m_alphagen.m_eType; }

        public void SetTexturePath(string s) { m_sTexturePath = s; }
        public void SetAnimmap(string s)
        {
            string[] tokens = s.Split(' ');
            if (tokens.Length > 0)
            {
                for (int i = 1; i < tokens.Length; i++)
                {
                    string sToken = tokens[i].Trim();
                    if (!string.IsNullOrEmpty(sToken))
                    {
                        bool bShouldBeTGA = false;
                        bool bFoundTexture = false;
                        string sNonShaderTexture = m_ParentShader.GetPathToTextureNoShaderLookup(false, sToken, ref bShouldBeTGA);
                        if (!File.Exists(sNonShaderTexture))
                        {
                            string sPK3 = Path.ChangeExtension(m_ParentShader.GetParent().GetMap().GetMapPathOnDisk, "pk3");
                            if (File.Exists(sPK3))
                            {
                                sNonShaderTexture = m_ParentShader.GetPathToTextureNoShaderLookup(false, sToken, ref bShouldBeTGA, sPK3);
                                if(File.Exists(sNonShaderTexture))
                                {
                                    bFoundTexture = true;
                                }
                            }
                        }
                        else
                        {
                            bFoundTexture = true;
                        }

                        if (bFoundTexture)
                        {
                            m_lAnimmapTextures.Add(new Texture(sToken, sNonShaderTexture));

                            m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetShouldBeTGA(bShouldBeTGA);
                            m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetTexture(m_ParentShader);
                        }
                        else
                        {
                            LOGGER.Info("Could not locate texture for animmap " + sNonShaderTexture);
                            m_ParentShader.GetParent().SetDontRender(true);
                        }
                    }
                }
                m_fSecondsPerAnimmapTexture = 1f / (float)Convert.ToSingle(tokens[0]);
                m_sAnimmapIntervalInput = tokens[0];
            }
        }

        public void Delete()
        {
            foreach (Texture t in m_lAnimmapTextures)
            {
                t.Delete();
            }
        }

        public void GLDefineTextures()
        {
            foreach(Texture t in m_lAnimmapTextures)
            {
                t.GLDefineTexture();
            }
        }

        public void SetLightmap(bool b) { m_bLightmap = b; }
        public bool GetLightmap() { return m_bLightmap; }
        public void SetTCModScroll(string s)
        {
            string[] tokens = s.Split(' ');
            if (tokens[0] != "0")
                m_scroll.s = Convert.ToSingle(tokens[0]);
            if (tokens[1] != "0")
                m_scroll.t = Convert.ToSingle(tokens[1]);

            m_TCMODS.Add(m_scroll);
        }
        public void SetTCMODStretch(string s)
        {
            string[] tokens = s.Split(' ');
            m_stretch.wf.func = tokens[0];
            m_stretch.wf.fbase = Convert.ToSingle(tokens[1]);
            m_stretch.wf.amp = Convert.ToSingle(tokens[2]);
            m_stretch.wf.phase = Convert.ToSingle(tokens[3]);
            m_stretch.wf.freq = Convert.ToSingle(tokens[4]);
            m_TCMODS.Add(m_stretch);
        }
        public void SetTCMODRotate(float f)
        {
            m_rotate.degPerSecond = f;
            m_TCMODS.Add(m_rotate);
        }
        public void SetTCModTurb(string s)
        {
            string[] tokens = s.Split(' ');

            if (tokens.Length > 0)
            {
                m_turb.amplitude = Convert.ToSingle(tokens[1]); // amp
                m_turb.phase = Convert.ToSingle(tokens[2]); // phase
                m_turb.frequency = Convert.ToSingle(tokens[3]); // freq
            }

            m_TCMODS.Add(m_turb);
        }
        public void SetTCModScale(string s)
        {
            string[] tokens = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            m_scale.s = Convert.ToSingle(tokens[0]);
            m_scale.t = Convert.ToSingle(tokens[1]);

            m_TCMODS.Add(m_scale);
        }

        public void SetTCModTransform(string s)
        {
            string[] tokens = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            m_transform.m00 = Convert.ToSingle(tokens[0]);
            m_transform.m01 = Convert.ToSingle(tokens[1]);
            m_transform.m10 = Convert.ToSingle(tokens[2]);
            m_transform.m11 = Convert.ToSingle(tokens[3]);
            m_transform.t0 = Convert.ToSingle(tokens[4]);
            m_transform.t1 = Convert.ToSingle(tokens[5]);

            m_TCMODS.Add(m_transform);
        }

        public string GetTexturePath() { return m_sTexturePath; }

        public void GetTurbValues(ref float[] vals)
        {
            vals[0] = m_turb.amplitude;
            vals[1] = m_turb.phase;
            vals[2] = m_turb.frequency;
        }

        public void GetScaleValues(ref float[] vals)
        {
            vals[0] = m_scale.s;
            vals[1] = m_scale.t;
        }

        public void GetScrollValues(ref float[] vals)
        {
            if (m_ParentShader.GetStageTexture(this).GetWide())
            {
                vals[0] = -1 * m_scroll.s; // to get scrolling of scrolllight and techborder to work
                vals[1] = -1 * m_scroll.t;
            }
            else
            {
                if (m_bSyncTCMODandAnimmap) 
                {
                    // launch pad animation doesn't sync

                    /*long f = (long)m_scroll.t * GameGlobals.GetElapsedMS();

                    if (m_fPrevScrollAmountAtAnimmapChange == System.Single.MaxValue)
                    {
                        m_fPrevScrollAmountAtAnimmapChange = f;
                    }
                    else if ((f - m_fPrevScrollAmountAtAnimmapChange) >= (1000f / m_scroll.t))
                    {
                        m_nCurAnimmapTextureIndex++;
                        if (m_nCurAnimmapTextureIndex > m_lAnimmapTextures.Count - 1) m_nCurAnimmapTextureIndex = 0;

                        m_fPrevScrollAmountAtAnimmapChange = f;
                    }*/
                }
                vals[0] = m_scroll.s;
                vals[1] = m_scroll.t;
            }
        }

        // first four values are 2 x 2 transform matrix, last two are a translation vector
        public void GetStretchValues(ref float[] vals)
        {
            float p = CalculateWaveForm(m_stretch.wf);

            if (GameGlobals.IsJumpPad(m_ParentShader.GetShaderName()))
                p = p * 1.2f; // not sure why needed but if fixes all bounce pads it seems
            else 
                p = p * 1.6f; // don't know why i have to do this but it makes the flaming turners look right in dm1 so leaving it for now
            // without it the stretching doesn't go far enough in towards the center   
            
            // these adjustments are probably because the vertices are scaled down significantly in the vrml?

            vals[0] = p;
            vals[1] = 0;
            vals[4] = (.5f - .5f * p);
            vals[2] = 0;
            vals[3] = p;
            vals[5] = (.5f - .5f * p);
        }

        public void GetTransformValues(ref float[] vals)
        {
            vals[0] = m_transform.m00;
            vals[1] = m_transform.m01;
            vals[2] = m_transform.m10;
            vals[3] = m_transform.m11;
            vals[4] = m_transform.t0;
            vals[5] = m_transform.t1;
        }

        // first four values are 2 x 2 transform matrix, last two are a translation vector
        public void GetRotateValues(ref float[] vals)
        {
            float degs;
            float sinValue, cosValue;

            degs = m_rotate.degPerSecond * GameGlobals.GetElapsedS();

            sinValue = Convert.ToSingle(Math.Sin(Convert.ToDouble(degs) * utilities.GLB.DegToRad)); 
            cosValue = Convert.ToSingle(Math.Cos(Convert.ToDouble(degs) * utilities.GLB.DegToRad)); 

            vals[0] = cosValue; // 0, 0
            vals[1] = -sinValue; // 1, 0
            vals[4] = 0.5f - 0.5f * cosValue + 0.5f * sinValue;
            vals[2] = sinValue; // 0, 1
            vals[3] = cosValue; // 1, 1
            vals[5] = 0.5f - 0.5f * sinValue - 0.5f * cosValue;
        }

        // this needs to factor in phase at some point
        private float CalculateWaveForm(WaveForm wf)
        {            
            float fVal = 1f;
            float fElapsedS = GameGlobals.GetElapsedS();

            if (wf.func == "sin")
            {
                if (wf.amp == 0) // teleporters in ctf 4 
                {
                    fVal = wf.fbase * 1.3f;
                }
                else
                {
                    float dCycleTimeMS = 1.0f / (wf.freq * 2.0f) * 1000.0f;
                    // the point of this calculation is to convert from one range to another
                    // the first range is from 0 to the full cycle time
                    // the second range is from 0 to PI
                    // we want to convert the first range to the second so we can plug into sin
                    double dIntoSin = (fElapsedS * 1000f + wf.phase * wf.freq) / dCycleTimeMS * Math.PI;

                    // after plugging into sin you just have to scale by the amplitude and then add the initial value
                    float fSinValue = Convert.ToSingle(Math.Sin(dIntoSin));

                    fVal = Convert.ToSingle(fSinValue * wf.amp + wf.fbase);

                    if (m_bSyncRGBGENandTCMOD)
                    {
                        if (m_fPrevRGBGENandTCMODVal < fVal && fVal > 0) m_bSquareOnOff = false;
                        else m_bSquareOnOff = true;

                        m_fPrevRGBGENandTCMODVal = fVal;
                    }

                    if (fVal <= 0) fVal = 0;
                }
            }
            else if (wf.func == "sawtooth")
            {
                fVal = (fElapsedS + wf.phase * wf.freq) % (1f / wf.freq) * wf.freq * wf.amp + wf.fbase;

                if (m_bSyncRGBGENandAnimmap && (Math.Abs(m_fPrevRGBGENWaveformVal - fVal) > (m_rgbgen.wf.amp / 2.0f)))
                {
                    // if the change is greater than half the amplitude assume the function cycled
                    m_nCurAnimmapTextureIndex++;
                    if (m_nCurAnimmapTextureIndex > m_lAnimmapTextures.Count - 1) m_nCurAnimmapTextureIndex = 0;
                }

                m_fPrevRGBGENWaveformVal = fVal;
            }
            else if (wf.func == "inversesawtooth")
            {
                fVal = wf.amp - (fElapsedS + wf.phase * wf.freq) % (1f / wf.freq) * wf.freq * wf.amp + wf.fbase;

                if (m_bSyncRGBGENandAnimmap && (Math.Abs(m_fPrevRGBGENWaveformVal - fVal) > (m_rgbgen.wf.amp / 2.0f)))
                {
                    // if the change is greater than half the amplitude assume the function cycled
                    m_nCurAnimmapTextureIndex++;
                    if (m_nCurAnimmapTextureIndex > m_lAnimmapTextures.Count - 1) m_nCurAnimmapTextureIndex = 0;
                }

                m_fPrevRGBGENWaveformVal = fVal;
            }
            else if(wf.func == "square")
            {
                if (m_bSyncRGBGENandTCMOD)
                {
                    if (m_bSquareOnOff) fVal = wf.fbase + wf.amp;
                    else fVal = 0;
                }
                else
                {
                    int n = Math.Sign(Math.Sin(Math.PI * 2 * (fElapsedS + wf.phase * wf.freq) * wf.freq));
                    if (n >= 0) fVal = wf.fbase + wf.amp;
                    else fVal = 0; // this seems to be right. see blinking red tower lights in dm0
                }
            }
            else if(wf.func == "triangle")
            {
                float fHalfPeriod = 1 / wf.freq / 2;
                fVal = wf.amp / fHalfPeriod * (fHalfPeriod - Math.Abs((fElapsedS + wf.phase * fHalfPeriod) % (2 * fHalfPeriod) - fHalfPeriod)) + wf.fbase;
            }   

            return fVal;
        }

        public float GetAlphaGenValue()
        {
            if (m_alphagen.m_eType == GEN.ETYPE.WAVEFORM)
            {
                float f = CalculateWaveForm(m_alphagen.wf);
                return f;
            }
            else if(m_alphagen.m_eType == GEN.ETYPE.CONSTANT)
            {
                return m_alphagen.m_fConst[0];
            }
            throw new Exception("invalid alpha gen type: " + m_alphagen.m_eType);
        }

        public void GetRGBGenValue(ref float[] rgb)
        {
            rgb[0] = 1f; 
            rgb[1] = 1f; 
            rgb[2] = 1f;

            if (m_rgbgen.m_eType == GEN.ETYPE.WAVEFORM)
            {
                rgb[0] = CalculateWaveForm(m_rgbgen.wf);
                rgb[1] = rgb[0];
                rgb[2] = rgb[0];                
            }
            else if(m_rgbgen.m_eType == GEN.ETYPE.CONSTANT)
            {
                rgb[0] = m_rgbgen.m_fConst[0];
                rgb[1] = m_rgbgen.m_fConst[1];
                rgb[2] = m_rgbgen.m_fConst[2];
            }
        }
    }
}
