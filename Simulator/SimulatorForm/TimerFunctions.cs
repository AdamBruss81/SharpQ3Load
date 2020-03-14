using System;

namespace simulator
{
    public partial class SimulatorForm
    {
        private void timerLeftMouse_Tick(object sender, EventArgs e)
        {
            m_Engine.LeftMouseDown();
        }

        private void timerMoveForward_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveForward();
        }

        private void timerMoveBackward_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveBackward();
        }

        private void timerStrafeLeft_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveLeft();
        }

        private void timerStrafeRight_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveRight();
        }

        private void timerMoveUp_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveUp();
        }

        private void timerMoveDown_Tick(object sender, EventArgs e)
        {
            m_Engine.MoveDown();
        }

        private void timerFallChecker_Tick(object sender, EventArgs e)
        {
            m_Engine.Fall();
        }

        private void timerRedrawer_Tick(object sender, EventArgs e)
        {
            resetMouseCursor();

            m_nFrameCounter++;

            m_swFramerate.Start();

            m_Engine.showScene(GetRecentKey);

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
                timerFallChecker.Start();
            }
            else
            {
                timerRedrawer.Stop();
                timerFallChecker.Stop();
            }
        }   

        private void StopAllTimers()
        {
            StartStopRedrawer(false);
            timerMoveForward.Stop();
            timerMoveBackward.Stop();
            timerStrafeLeft.Stop();
            timerStrafeRight.Stop();
            timerMoveUp.Stop();
            timerMoveDown.Stop();
            timerFallChecker.Stop();
        }
    }
}