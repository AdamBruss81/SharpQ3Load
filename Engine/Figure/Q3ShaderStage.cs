using System;
using System.Collections.Generic;

namespace engine
{
    public abstract class TCMOD
    {
        public enum ETYPE { SCROLL, SCALE, TURB, ROTATE, STRETCH };

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

    class RGBGEN
    {
        public string type = "";
        public WaveForm wf = new WaveForm();
    }

    class WaveForm
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
        bool m_bLightmap = false;
        TCTURB m_turb = new TCTURB();
        TCSCALE m_scale = new TCSCALE();
        TCSCROLL m_scroll = new TCSCROLL();
        TCROTATE m_rotate = new TCROTATE();
        TCSTRETCH m_stretch = new TCSTRETCH();
        RGBGEN m_rgbgen = new RGBGEN();
        Q3Shader m_ParentShader = null;
        List<TCMOD> m_TCMODS = new List<TCMOD>();
        List<Texture> m_lAnimmapTextures = new List<Texture>();
        int m_nCurAnimmapTextureIndex = 0;
        float m_fSecondsPerAnimmapTexture = 0f;
        float m_fLastAnimmapTextureChangeTime = 0f;

        public Q3ShaderStage(Q3Shader container) { m_ParentShader = container; }

        public void SetBlendFunc(string s) { m_sBlendFunc = s; }
        public void SetAlphaFunc(string s) { m_sAlphaFunc = s; }
        public bool IsRGBGENIdentity()
        {
            return (m_rgbgen.type.ToLower() == "identity" || string.IsNullOrEmpty(m_rgbgen.type));
        }

        public bool IsAnimmap() { return m_lAnimmapTextures.Count > 0; }

        public Texture GetAnimmapTexture()
        {
            if (m_fLastAnimmapTextureChangeTime == 0f) m_fLastAnimmapTextureChangeTime = GameClock.GetElapsedS();
            else if (GameClock.GetElapsedS() - m_fLastAnimmapTextureChangeTime >= m_fSecondsPerAnimmapTexture)
            {
                m_nCurAnimmapTextureIndex++;
                if (m_nCurAnimmapTextureIndex > m_lAnimmapTextures.Count - 1) m_nCurAnimmapTextureIndex = 0;
                m_fLastAnimmapTextureChangeTime = GameClock.GetElapsedS();
            }

            return m_lAnimmapTextures[m_nCurAnimmapTextureIndex];
        }

        public List<TCMOD> GetTCMODS() { return m_TCMODS; }

        public void SetRGBGEN(string s)
        {
            string[] tokens = s.Split(' ');

            if (tokens.Length == 1) m_rgbgen.type = tokens[0];
            else
            {
                m_rgbgen.type = tokens[0].ToLower();
                m_rgbgen.wf.func = tokens[1].ToLower();

                // initial value
                m_rgbgen.wf.fbase = System.Convert.ToSingle(tokens[2]);

                // amplitude
                m_rgbgen.wf.amp = System.Convert.ToSingle(tokens[3]);

                // phase. will use later. i think this is used to offset.
                m_rgbgen.wf.phase = System.Convert.ToSingle(tokens[4]);

                // peaks per second
                m_rgbgen.wf.freq = System.Convert.ToSingle(tokens[5]);
            }
        }

        public bool IsVertexColor() { return m_rgbgen.type.ToLower() == "vertex"; }

        public string GetBlendFunc() { return m_sBlendFunc; }
        public string GetAlphaFunc() { return m_sAlphaFunc; }

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
                        m_lAnimmapTextures.Add(new Texture(sToken));
                        bool bShouldBeTGA = false;
                        string sNonShaderTexture = m_ParentShader.GetPathToTextureNoShaderLookup(false, m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].GetPath(), ref bShouldBeTGA);
                        m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetTexture(sNonShaderTexture);
                        if (bShouldBeTGA) m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetShouldBeTGA(true);
                    }
                }
                m_fSecondsPerAnimmapTexture = 1f / (float)Convert.ToInt32(tokens[0]);
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
        public void SetTCModeScale(string s)
        {
            string[] tokens = s.Split(' ');
            m_scale.s = Convert.ToSingle(tokens[0]);
            m_scale.t = Convert.ToSingle(tokens[1]);

            m_TCMODS.Add(m_scale);
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
            vals[0] = m_scroll.s;
            vals[1] = m_scroll.t;
        }

        // first four values are 2 x 2 transform matrix, last two are a translation vector
        public void GetStretchValues(ref float[] vals)
        {
            float p = CalculateWaveForm(m_stretch.wf);

            p = p * 1.5f; // don't know why i have to do this but it makes the flaming turners look right in dm1 so leaving it for now
            // without it the stretching doesn't go far enough in towards the center

            vals[0] = p;
            vals[1] = 0;
            vals[4] = (.5f - .5f * p);
            vals[2] = 0;
            vals[3] = p;
            vals[5] = (.5f - .5f * p);
        }

        // first four values are 2 x 2 transform matrix, last two are a translation vector
        public void GetRotateValues(ref float[] vals)
        {
            float degs;
            float sinValue, cosValue;

            degs = m_rotate.degPerSecond * -1f * GameClock.GetElapsedS();

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
            float fVal = 0f;

            if (wf.func == "sin")
            {
                float dCycleTimeMS = 1.0f / wf.freq * 1000.0f;
                // the point of this calculation is to convert from one range to another
                // the first range is from 0 to the full cycle time
                // the second range is from 0 to PI
                // we want to convert the first range to the second so we can plug into sin
                double dIntoSin = GameClock.GetElapsedMS() / dCycleTimeMS * Math.PI;

                // after plugging into sin you just have to scale by the amplitude and then add the initial value
                fVal = Math.Abs(Convert.ToSingle(Math.Sin(dIntoSin) * wf.amp + wf.fbase));
            }
            else if (wf.func == "sawtooth")
            {
                fVal = ((GameClock.GetElapsedS() * wf.freq) % 1f) * wf.amp;
            }
            else if (wf.func == "inversesawtooth")
            {
                fVal = (((1f - (GameClock.GetElapsedS() % 1f)) * (wf.freq)) % 1f) * wf.amp;
            }

            return fVal;
        }

        public void GetRGBGenValue(ref float[] rgb)
        {
            rgb[0] = 1f; 
            rgb[1] = 1f; 
            rgb[2] = 1f;          

            if (m_rgbgen.type == "wave")
            {
                rgb[0] = CalculateWaveForm(m_rgbgen.wf);
                rgb[1] = rgb[0];
                rgb[2] = rgb[0];                
            }
        }
    }
}
