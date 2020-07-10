using System;
using System.Collections.Generic;

namespace engine
{
    public abstract class TCMOD
    {
        public enum ETYPE { SCROLL, SCALE, TURB };

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

    class RGBGEN
    {
        public string type = "";
        public string func = "";

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
        bool m_bLightmap = false;
        TCTURB m_turb = new TCTURB();
        TCSCALE m_scale = new TCSCALE();
        TCSCROLL m_scroll = new TCSCROLL();
        RGBGEN m_rgbgen = new RGBGEN();
        Q3Shader m_ParentShader = null;
        List<TCMOD> m_TCMODS = new List<TCMOD>();
        List<Texture> m_lAnimmapTextures = new List<Texture>();
        int m_nCurAnimmapTextureIndex = 0;
        float m_fSecondsPerAnimmapTexture = 0f;
        float m_fLastAnimmapTextureChangeTime = 0f;

        public Q3ShaderStage(Q3Shader container) { m_ParentShader = container; }

        public void SetBlendFunc(string s) { m_sBlendFunc = s; }
        public bool IsRGBGENIdentity() 
        {
            return (m_rgbgen.type.ToLower() == "identity" || string.IsNullOrEmpty(m_rgbgen.type)); 
        }

        public bool IsAnimmap() { return m_lAnimmapTextures.Count > 0; }

        public Texture GetAnimmapTexture()
        {
            if (m_fLastAnimmapTextureChangeTime == 0f) m_fLastAnimmapTextureChangeTime = GameClock.GetElapsedS();
            else if(GameClock.GetElapsedS() - m_fLastAnimmapTextureChangeTime >= m_fSecondsPerAnimmapTexture)
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
                m_rgbgen.func = tokens[1].ToLower();

                // initial value
                m_rgbgen.fbase = System.Convert.ToSingle(tokens[2]);

                // amplitude
                m_rgbgen.amp = System.Convert.ToSingle(tokens[3]);

                // phase. will use later. i think this is used to offset.
                m_rgbgen.phase = System.Convert.ToSingle(tokens[4]);

                // peaks per second
                m_rgbgen.freq = System.Convert.ToSingle(tokens[5]);
            }
        }

        public bool IsVertexColor() { return m_rgbgen.type.ToLower() == "vertex"; }

        public string GetBlendFunc() { return m_sBlendFunc; }

        public void SetTexturePath(string s) { m_sTexturePath = s; }
        public void SetAnimmap(string s) 
        {
            string[] tokens = s.Split(' ');
            if(tokens.Length > 0)
            {
                for (int i = 1; i < tokens.Length; i++)
                {
                    m_lAnimmapTextures.Add(new Texture(tokens[i]));
                    bool bShouldBeTGA = false;
                    string sNonShaderTexture = m_ParentShader.GetPathToTextureNoShaderLookup(false, m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].GetPath(), ref bShouldBeTGA);
                    m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetTexture(sNonShaderTexture);
                    if (bShouldBeTGA) m_lAnimmapTextures[m_lAnimmapTextures.Count - 1].SetShouldBeTGA(true);
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
        public void SetTCModTurb(string s) 
        {
            string[] tokens = s.Split(' ');

            if(tokens.Length > 0)
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

        public void GetRGBGenValue(ref float[] rgb)
        {
            rgb[0] = 1f; 
            rgb[1] = 1f; 
            rgb[2] = 1f;

            float dCycleTimeMS = 1.0f / m_rgbgen.freq * 1000.0f;

            if (m_rgbgen.type == "wave")
            {               
                if (m_rgbgen.func == "sin")
                {
                    // the point of this calculation is to convert from one range to another
                    // the first range is from 0 to the full cycle time
                    // the second range is from 0 to PI
                    // we want to convert the first range to the second so we can plug into sin
                    double dIntoSin = GameClock.GetElapsedMS() / dCycleTimeMS * Math.PI;

                    // after plugging into sin you just have to scale by the amplitude and then add the initial value
                    rgb[0] = Math.Abs(Convert.ToSingle(Math.Sin(dIntoSin) * m_rgbgen.amp + m_rgbgen.fbase));
                    rgb[1] = rgb[0];
                    rgb[2] = rgb[0];
                }
                else if(m_rgbgen.func == "sawtooth")
                {
                    rgb[0] = ((GameClock.GetElapsedS() * m_rgbgen.freq) % 1f) * m_rgbgen.amp;
                    rgb[1] = rgb[0];
                    rgb[2] = rgb[0];
                }
                else if (m_rgbgen.func == "inversesawtooth")
                {
                    rgb[0] = (((1f - (GameClock.GetElapsedS() % 1f)) * (m_rgbgen.freq)) % 1f) * m_rgbgen.amp;
                    rgb[1] = rgb[0];
                    rgb[2] = rgb[0];
                }
            }
        }
    }
}
