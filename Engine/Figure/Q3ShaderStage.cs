﻿using utilities;
using System;

namespace engine
{
    class TCTURB
    {
        public float amplitude = 0f;
        public float phase = 0f;
        public float frequency = 0f;
    }

    class TCSCROLL
    {
        public float s = 0f;
        public float t = 0f;
    }

    class TCSCALE
    {
        public float s = 1f;
        public float t = 1f;
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
        string m_sAnimmap = "";
        bool m_bLightmap = false;
        TCTURB m_turb = new TCTURB();
        TCSCALE m_scale = new TCSCALE();
        TCSCROLL m_scroll = new TCSCROLL();
        RGBGEN m_rgbgen = new RGBGEN();
        Q3Shader m_ParentShader = null;

        public Q3ShaderStage(Q3Shader container) { m_ParentShader = container; }

        public void SetBlendFunc(string s) { m_sBlendFunc = s; }
        public void SetRGBGEN(string s)
        {
            string[] tokens = s.Split(' ');

            if (tokens.Length == 1) m_rgbgen.type = tokens[0];
            else
            {
                m_rgbgen.type = tokens[0];
                m_rgbgen.func = tokens[1];

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

        public void SetTexturePath(string s) { m_sTexturePath = s; }
        public void SetAnimmap(string s) { m_sAnimmap = s; }
        public void SetLightmap(bool b) { m_bLightmap = b; }
        public void SetTCModScroll(string s) 
        {
            string[] tokens = s.Split(' ');
            if (tokens[0] != "0")
                m_scroll.s = Convert.ToSingle(tokens[0]);
            if (tokens[1] != "0")
                m_scroll.t = Convert.ToSingle(tokens[1]);
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
        }
        public void SetTCModeScale(string s) 
        {
            string[] tokens = s.Split(' ');
            m_scale.s = Convert.ToSingle(tokens[0]);
            m_scale.t = Convert.ToSingle(tokens[1]);
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

        // pre calculate rgbgen double inputs once instead of doing it every time in here
        public void GetRGBGenValue(ref float[] rgb)
        {
            if (m_rgbgen.type == "wave")
            {
                float dCycleTimeMS = 1.0f / m_rgbgen.freq * 1000.0f;

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
            }
        }
    }
}
