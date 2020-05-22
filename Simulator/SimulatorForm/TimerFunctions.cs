using System;
using utilities;

namespace simulator
{
    public partial class SimulatorForm
    {
        long m_nLastFrameTimeMilli = 15 ;
        System.Diagnostics.Stopwatch m_swFrameTimer = new System.Diagnostics.Stopwatch();

        private void timerRedrawer_Tick(object sender, EventArgs e)
        {
            resetMouseCursor();

            m_nFrameCounter++;

            m_swFrameTimer.Start();

            // key game functions
            ProcessMouseButtons();
            MoveStates stoppedMovingStates = new MoveStates();
            MoveStates startedMovingStates = new MoveStates();
            ProcessKeyStates(stoppedMovingStates, startedMovingStates);
            m_Engine.GameTick(stoppedMovingStates, startedMovingStates, m_nLastFrameTimeMilli);
            SetLastMoveStates();
            m_Engine.showScene(GetRecentKey);
            // ###            

            m_fonter.PrintLowerRight(Math.Round(m_dVelocity, 2).ToString(), m_openGLControl.Width, 2);
            m_fonter.PrintLowerRight(m_dFPS.ToString(), m_openGLControl.Width, 1);
            m_fonter.PrintLowerRight(GetRecentKey.ToString(), m_openGLControl.Width, 0);

            m_openGLControl.SwapBuffers();

            m_swFrameTimer.Stop();
            m_nLastFrameTimeMilli = m_swFrameTimer.ElapsedMilliseconds;
            m_swFrameTimer.Reset();

            if (m_swDelayMusicStart.IsRunning && m_swDelayMusicStart.ElapsedMilliseconds >= 5000)
            {
                m_swDelayMusicStart.Reset();
                //m_SoundManager.PlayRandomSong();
            }
        }

        private void timerShowFPS_Tick(object sender, EventArgs e)
        {
            m_dVelocity = m_Engine.GetVelocity();
            m_dFPS = System.Convert.ToDouble(m_nFrameCounter * (1000 / timerShowFPS.Interval));
            m_nFrameCounter = 0;
        }

        private void StartStopRedrawer(bool b)
        {
            if (b)
            {
                timerShowFPS.Start();
                timerRedrawer.Start();
            }
            else
            {
                timerShowFPS.Stop();
                timerRedrawer.Stop();
            }
        }   

        private void StopAllTimers()
        {
            StartStopRedrawer(false);          
        }
    }
}