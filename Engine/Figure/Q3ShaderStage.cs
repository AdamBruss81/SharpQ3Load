using utilities;
using System;

namespace engine
{
    public class Q3ShaderStage
    {
        string m_sTexturePath = "";
        string m_sBlendFunc = "";
        string m_sRGBGEN = "";
        string m_sAnimmap = "";
        bool m_bLightmap = false;

        long m_CycleStartMS = 0;

        public Q3ShaderStage()
        {

        }

        public void SetBlendFunc(string s) { m_sBlendFunc = s; }
        public void SetRGBGEN(string s) { m_sRGBGEN = s; }
        public void SetTexturePath(string s) { m_sTexturePath = s; }
        public void SetAnimmap(string s) { m_sAnimmap = s; }
        public void SetLightmap(bool b) { m_bLightmap = b; }

        public string GetTexturePath() { return m_sTexturePath; }
        public string GetBlendFunc() { return m_sBlendFunc; }
        public string GetRGBGen() { return m_sRGBGEN; }
        public string GetAnimmap() { return m_sAnimmap; }

        public D3Vect GetRGBGenValue()
        {
            D3Vect dRGB = new D3Vect(1.0, 1.0, 1.0);
            if(!string.IsNullOrEmpty(m_sRGBGEN))
            {
                if(m_sRGBGEN != "identity")
                {
                    string[] tokens = m_sRGBGEN.Split(' ');
                    if(tokens[0] == "wave")
                    {
                        double dInitial = System.Convert.ToDouble(tokens[2]);
                        double dAmp = System.Convert.ToDouble(tokens[3]);

                        // will use later. i think this is used to offset.
                        double dPhase = System.Convert.ToDouble(tokens[4]); 

                        double dPeaksPerSecond = System.Convert.ToDouble(tokens[5]);
                        double dCycleTimeMS = 1.0 / dPeaksPerSecond * 1000.0;

                        long elapsedForCycleMS;
                        if (m_CycleStartMS == 0) {
                            m_CycleStartMS = GameClock.GetElapsedMS();
                            elapsedForCycleMS = 0;
                        }
                        else
                            elapsedForCycleMS = GameClock.GetElapsedMS() - m_CycleStartMS;

                        if((double)(elapsedForCycleMS) >= dCycleTimeMS) // if need to start a new cycle
                        {
                            m_CycleStartMS = 0;
                            elapsedForCycleMS = 0;
                        }

                        string sType = tokens[1];                        

                        if(sType == "sin")
                        {
                            // the point of this calculation is to convert from one range to another
                            // the first range is from 0 to the full cycle time
                            // the second range is from 0 to PI
                            // we want to convert the first range to the second so we can plug into sin
                            double dIntoSin = elapsedForCycleMS / dCycleTimeMS * Math.PI; 

                            // after plugging into sin you just have to scale by the amplitude and then add the initial value
                            dRGB.SetAll(Math.Sin(dIntoSin) * dAmp + dInitial);
                        }
                    }
                }
            }
            return dRGB;
        }
    }
}
