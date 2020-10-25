using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using utilities;

namespace engine
{
    class StopWatchManager
    {
        Stopwatch m_swFallTimer = new Stopwatch();
        Stopwatch m_swJumpTimer = new Stopwatch();
        Stopwatch m_swStepTimer = new Stopwatch();

        Stopwatch m_swPostMoveForwardDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveForwardAccelTimer = new Stopwatch();        

        Stopwatch m_swPostMoveLeftDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveLeftAccelTimer = new Stopwatch();

        Stopwatch m_swPostMoveRightDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveRightAccelTimer = new Stopwatch();

        Stopwatch m_swPostMoveBackDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveBackAccelTimer = new Stopwatch();

        D3Vect m_currentJumpVector = null;

        MovableCamera m_cam = null;
        Player m_Player = null;

        const double mc_dDecelAccelTimeMS = 250.0;
        const double mc_dDecelJumpMS = 350;

        Dictionary<MovableCamera.DIRECTION, double> m_dictMaxDecelMS = new Dictionary<MovableCamera.DIRECTION, double>();
        Dictionary<MovableCamera.DIRECTION, double> m_dictMaxAccelMS = new Dictionary<MovableCamera.DIRECTION, double>();

        public enum AccelModes { ACCEL, DECEL, ACCEL_AND_DECEL, FALL, ALL };
        public enum SWCommand { START, RESET };

        public StopWatchManager(MovableCamera cam, Player player) 
        {
            m_cam = cam;
            m_Player = player;
        }

        public long GetElapsed(MovableCamera.DIRECTION dir, bool bAccel)
        {
            Stopwatch sw = GetStopWatch(dir, bAccel);
            if (sw.IsRunning)
            {
                return sw.ElapsedMilliseconds;
            }
            else return 0;
        }

        public Stopwatch GetStepper() { return m_swStepTimer; }

        public double GetMaxMS(MovableCamera.DIRECTION dir, bool bAccel)
        {
            if(bAccel)
            {
                if (m_dictMaxAccelMS.ContainsKey(dir))
                    return m_dictMaxAccelMS[dir];
                else return 0;
            }
            else
            {
                if (m_dictMaxDecelMS.ContainsKey(dir))
                    return m_dictMaxDecelMS[dir];
                else return 0;
            }
        }

        public void Command(MovableCamera.DIRECTION dir, bool bAccel, SWCommand command)
        {
            Stopwatch sw = GetStopWatch(dir, bAccel);
            if (command == SWCommand.RESET)
            {
                sw.Reset();
            }
            else
                sw.Start();
        }

        public D3Vect GetCurrentJumpVector() { return m_currentJumpVector; }

        public bool IsRunning(MovableCamera.DIRECTION dir, bool bAccel)
        {
            return GetStopWatch(dir, bAccel).IsRunning;         
        }

        private Stopwatch GetStopWatch(MovableCamera.DIRECTION dir, bool bAccel)
        {
            switch (dir)
            {
                case MovableCamera.DIRECTION.FORWARD:
                    if (bAccel) return m_swStartMoveForwardAccelTimer;
                    else return m_swPostMoveForwardDecelTimer;
                case MovableCamera.DIRECTION.BACK:
                    if (bAccel) return m_swStartMoveBackAccelTimer;
                    else return m_swPostMoveBackDecelTimer;
                case MovableCamera.DIRECTION.LEFT:
                    if(bAccel) return m_swStartMoveLeftAccelTimer;
                    else return m_swPostMoveLeftDecelTimer;
                case MovableCamera.DIRECTION.RIGHT:
                    if (bAccel) return m_swStartMoveRightAccelTimer;
                    else return m_swPostMoveRightDecelTimer;
                case MovableCamera.DIRECTION.DOWN:
                    return m_swFallTimer;
                case MovableCamera.DIRECTION.UP:
                    return m_swJumpTimer;                    
            }

            throw new Exception("No stopwatch found");
        }

        public bool GetAnyRunning(AccelModes eMode)
        {
            bool bAccel = m_swStartMoveBackAccelTimer.IsRunning ||
                m_swStartMoveForwardAccelTimer.IsRunning || m_swStartMoveLeftAccelTimer.IsRunning || m_swStartMoveRightAccelTimer.IsRunning;

            bool bDecel = m_swPostMoveBackDecelTimer.IsRunning || m_swPostMoveForwardDecelTimer.IsRunning ||
            m_swPostMoveLeftDecelTimer.IsRunning || m_swPostMoveRightDecelTimer.IsRunning;

            if (eMode == AccelModes.ACCEL) return bAccel;
            else if (eMode == AccelModes.DECEL) return bDecel;
            else if (eMode == AccelModes.ACCEL_AND_DECEL) return bAccel || bDecel;
            else if (eMode == AccelModes.FALL) return m_swFallTimer.IsRunning;
            else if (eMode == AccelModes.ALL) return bAccel || bDecel || m_swFallTimer.IsRunning;

            return false;
        }

        public double GetFallScale()
        {
            double dScale = 1.0;

            // slower than 9.8 to account for fast timer tick
            dScale = 5.0 * m_swFallTimer.ElapsedMilliseconds / 1000;

            return dScale;
        }

        public double GetSlowDownScale(MovableCamera.DIRECTION dir)
        {
            double dScale = 1.0;

            Stopwatch sw = GetStopWatch(dir, false);

            double dElapsedAdjusted = sw.ElapsedMilliseconds + (mc_dDecelAccelTimeMS - m_dictMaxDecelMS[dir]);

            double dRatio = dElapsedAdjusted / mc_dDecelAccelTimeMS;

            dScale = (1.0 - dRatio) * m_cam.GetStandardMovementScale();

            //LOGGER.Debug("Decel scale is " + dScale + " for elapsed of " + sw.ElapsedMilliseconds + " for direction " + dir);

            return dScale;
        }

        public double GetSpeedUpScale(MovableCamera.DIRECTION dir)
        {
            double dScale = 1.0;

            Stopwatch sw = GetStopWatch(dir, true);

            double dElapsedAdjusted = sw.ElapsedMilliseconds + (mc_dDecelAccelTimeMS - m_dictMaxAccelMS[dir]);

            dScale = dElapsedAdjusted / mc_dDecelAccelTimeMS * m_cam.GetStandardMovementScale();

            //LOGGER.Debug("Accel scale is " + dScale + " for elapsed of " + sw.ElapsedMilliseconds + " for direction " + dir);

            return dScale;
        }

        /// <summary>
        /// Perform an normal spacebar jump, jumppad or launch pad
        /// </summary>
        /// <param name="dDecelJumpMS">how long to jump for</param>
        /// <param name="customLookAt">optional vector to jump along</param>
        public void Jump(double dDecelJumpMS, D3Vect customLookAt = null)
        {
            //LOGGER.Debug("JUMP!");
            m_currentJumpVector = customLookAt;
            m_swJumpTimer.Reset();
            m_dictMaxDecelMS[MovableCamera.DIRECTION.UP] = dDecelJumpMS;
            m_swJumpTimer.Start(); 
        }

        public void HandleStartedMoving(MoveStates startedMovingStates, Dictionary<MovableCamera.DIRECTION, double> dictLastMoveScales)
        {        
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.FORWARD))
            {
                m_swPostMoveForwardDecelTimer.Reset();

                if (dictLastMoveScales.ContainsKey(MovableCamera.DIRECTION.FORWARD))
                    m_dictMaxAccelMS[MovableCamera.DIRECTION.FORWARD] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.FORWARD] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveForwardAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.BACK))
            {
                m_swPostMoveBackDecelTimer.Reset();

                if (dictLastMoveScales.ContainsKey(MovableCamera.DIRECTION.BACK))
                    m_dictMaxAccelMS[MovableCamera.DIRECTION.BACK] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.BACK] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveBackAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.LEFT))
            {
                m_swPostMoveLeftDecelTimer.Reset();

                // there's a bug here sometimes that causes a crash. a dictionary entry is not present and we assume it is.
                // fix at some point
                if(dictLastMoveScales.ContainsKey(MovableCamera.DIRECTION.LEFT))
                    m_dictMaxAccelMS[MovableCamera.DIRECTION.LEFT] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.LEFT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveLeftAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.RIGHT))
            {
                m_swPostMoveRightDecelTimer.Reset();

                if(dictLastMoveScales.ContainsKey(MovableCamera.DIRECTION.RIGHT))
                    m_dictMaxAccelMS[MovableCamera.DIRECTION.RIGHT] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.RIGHT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveRightAccelTimer.Start();
            }
            
            if(startedMovingStates.GetState(MovableCamera.DIRECTION.UP))
            {
                if (!m_swJumpTimer.IsRunning && !m_swFallTimer.IsRunning) {
                    m_Player.GetSoundManager().PlayEffect(SoundManager.EEffects.JUMP);
                    Jump(mc_dDecelJumpMS);
                }
            }
        }

        public void HandleStoppedMoving(MoveStates stoppedMovingStates, Dictionary<MovableCamera.DIRECTION, double> dictLastMoveScales)
        {
            if(stoppedMovingStates.GetState(MovableCamera.DIRECTION.FORWARD))
            {
                //LOGGER.Debug("Stopped moving forward");

                m_swStartMoveForwardAccelTimer.Reset(); // if you stop moving, reset the accel stopwatch

                m_dictMaxDecelMS[MovableCamera.DIRECTION.FORWARD] = dictLastMoveScales[MovableCamera.DIRECTION.FORWARD] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS;

                m_swPostMoveForwardDecelTimer.Start();
            }
            if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.BACK))
            {
                m_swStartMoveBackAccelTimer.Reset(); // if you stop moving, reset the accel stopwatch

                m_dictMaxDecelMS[MovableCamera.DIRECTION.BACK] = dictLastMoveScales[MovableCamera.DIRECTION.BACK] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS;

                m_swPostMoveBackDecelTimer.Start();
            }
            if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.LEFT))
            {
                m_swStartMoveLeftAccelTimer.Reset(); // if you stop moving, reset the accel stopwatch

                m_dictMaxDecelMS[MovableCamera.DIRECTION.LEFT] = dictLastMoveScales[MovableCamera.DIRECTION.LEFT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS;

                m_swPostMoveLeftDecelTimer.Start();
            }
            if (stoppedMovingStates.GetState(MovableCamera.DIRECTION.RIGHT))
            {
                m_swStartMoveRightAccelTimer.Reset(); // if you stop moving, reset the accel stopwatch

                m_dictMaxDecelMS[MovableCamera.DIRECTION.RIGHT] = dictLastMoveScales[MovableCamera.DIRECTION.RIGHT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS;

                m_swPostMoveRightDecelTimer.Start();
            }
        }

        public void StopAccelTimers()
        {
            if (IsRunning(MovableCamera.DIRECTION.FORWARD, true))
            {
                if (GetElapsed(MovableCamera.DIRECTION.FORWARD, true) >= GetMaxMS(MovableCamera.DIRECTION.FORWARD, true))
                {
                    Command(MovableCamera.DIRECTION.FORWARD, true, StopWatchManager.SWCommand.RESET);
                }
            }

            if (IsRunning(MovableCamera.DIRECTION.BACK, true))
            {
                if (GetElapsed(MovableCamera.DIRECTION.BACK, true) >= GetMaxMS(MovableCamera.DIRECTION.BACK, true))
                {
                    Command(MovableCamera.DIRECTION.BACK, true, StopWatchManager.SWCommand.RESET);
                }
            }

            if (IsRunning(MovableCamera.DIRECTION.LEFT, true))
            {
                if (GetElapsed(MovableCamera.DIRECTION.LEFT, true) >= GetMaxMS(MovableCamera.DIRECTION.LEFT, true))
                {
                    Command(MovableCamera.DIRECTION.LEFT, true, StopWatchManager.SWCommand.RESET);
                }
            }

            if (IsRunning(MovableCamera.DIRECTION.RIGHT, true))
            {
                if (GetElapsed(MovableCamera.DIRECTION.RIGHT, true) >= GetMaxMS(MovableCamera.DIRECTION.RIGHT, true))
                {
                    Command(MovableCamera.DIRECTION.RIGHT, true, StopWatchManager.SWCommand.RESET);
                }
            }
        }

        public double GetAccelDecelScale(MovableCamera.DIRECTION eSourceMovement)
        {
            double dAccelDecelScale = 1.0;

            if (eSourceMovement == MovableCamera.DIRECTION.FORWARD)
            {
                if (IsRunning(MovableCamera.DIRECTION.FORWARD, false))
                {
                    dAccelDecelScale = GetSlowDownScale(MovableCamera.DIRECTION.FORWARD);
                }
                else if (IsRunning(MovableCamera.DIRECTION.FORWARD, true))
                {
                    dAccelDecelScale = GetSpeedUpScale(MovableCamera.DIRECTION.FORWARD);
                }
            }
            else if (eSourceMovement == MovableCamera.DIRECTION.BACK)
            {
                if (IsRunning(MovableCamera.DIRECTION.BACK, false))
                {
                    dAccelDecelScale = GetSlowDownScale(MovableCamera.DIRECTION.BACK);
                }
                else if (IsRunning(MovableCamera.DIRECTION.BACK, true))
                {
                    dAccelDecelScale = GetSpeedUpScale(MovableCamera.DIRECTION.BACK);
                }
            }
            else if (eSourceMovement == MovableCamera.DIRECTION.LEFT)
            {
                if (IsRunning(MovableCamera.DIRECTION.LEFT, false))
                {
                    dAccelDecelScale = GetSlowDownScale(MovableCamera.DIRECTION.LEFT);
                }
                else if (IsRunning(MovableCamera.DIRECTION.LEFT, true))
                {
                    dAccelDecelScale = GetSpeedUpScale(MovableCamera.DIRECTION.LEFT);
                }
            }
            else if (eSourceMovement == MovableCamera.DIRECTION.RIGHT)
            {
                if (IsRunning(MovableCamera.DIRECTION.RIGHT, false))
                {
                    dAccelDecelScale = GetSlowDownScale(MovableCamera.DIRECTION.RIGHT);
                }
                else if (IsRunning(MovableCamera.DIRECTION.RIGHT, true))
                {
                    dAccelDecelScale = GetSpeedUpScale(MovableCamera.DIRECTION.RIGHT);
                }
            }
            else if(eSourceMovement == MovableCamera.DIRECTION.UP)
            {
                Debug.Assert(m_swJumpTimer.IsRunning);
                dAccelDecelScale = GetSlowDownScale(MovableCamera.DIRECTION.UP);
                //LOGGER.Debug("Up slowdown scale is: " + dAccelDecelScale);
            }
            else
            {
                throw new Exception("Invalid direction: " + eSourceMovement);
            }

            return dAccelDecelScale;
        }
    }
}
