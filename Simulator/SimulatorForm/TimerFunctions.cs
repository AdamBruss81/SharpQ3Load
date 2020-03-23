using System;
using System.Collections.Generic;
using utilities;

namespace simulator
{
    public partial class SimulatorForm
    {  
        private void timerRedrawer_Tick(object sender, EventArgs e)
        {
            resetMouseCursor();

            m_nFrameCounter++;

            m_swFramerate.Start();

            // key game functions
            ProcessMouseButtons();
            MoveStates stoppedMovingStates = new MoveStates();
            ProcessKeyStates(stoppedMovingStates);
            m_Engine.GameTick(stoppedMovingStates);
            SetLastMoveStates();
            m_Engine.showScene(GetRecentKey);
            // ###            

            m_swFramerate.Stop();

            if (m_nFrameCounter % 10 == 0)
            {
                m_dVelocity = m_Engine.GetVelocity();
                m_dElapsedSecondsShowScene = Math.Round(1.0 / m_swFramerate.Elapsed.TotalSeconds);
                m_nFrameCounter = 0;
            }

            m_fonter.PrintLowerRight(Math.Round(m_dVelocity, 2).ToString(), m_openGLControl.Width, 2);
            m_fonter.PrintLowerRight(m_dElapsedSecondsShowScene.ToString(), m_openGLControl.Width, 1);

            m_swFramerate.Reset();

            m_fonter.PrintLowerRight(GetRecentKey.ToString(), m_openGLControl.Width, 0);

            if (m_swDelayMusicStart.IsRunning && m_swDelayMusicStart.ElapsedMilliseconds >= 5000)
            {
                m_swDelayMusicStart.Reset();
                m_SoundManager.PlayRandomSong();
            }
        }

        private void StartStopRedrawer(bool b)
        {
            if (b)
            {
                timerRedrawer.Start();
            }
            else
            {
                timerRedrawer.Stop();
            }
        }   

        private void StopAllTimers()
        {
            StartStopRedrawer(false);          
        }
    }
}