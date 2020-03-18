using System;

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
            bool bStoppedMovingForwardBackward = false, bStoppedMovingLeftRight = false;
            ProcessKeyStates(ref bStoppedMovingForwardBackward, ref bStoppedMovingLeftRight);
            m_Engine.GameTick(m_lastmoveFB, m_lastmoveLR, bStoppedMovingForwardBackward, bStoppedMovingLeftRight);
            SetLastMoveStates();
            m_Engine.showScene(GetRecentKey);
            // ###

            m_swFramerate.Stop();

            if (m_nFrameCounter % 10 == 0)
            {
                m_dElapsedSecondsShowScene = Math.Round(1.0 / m_swFramerate.Elapsed.TotalSeconds);
                m_nFrameCounter = 0;
            }

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