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

        Stopwatch m_swPostMoveForwardDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveForwardAccelTimer = new Stopwatch();        

        Stopwatch m_swPostMoveLeftDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveLeftAccelTimer = new Stopwatch();

        Stopwatch m_swPostMoveRightDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveRightAccelTimer = new Stopwatch();

        Stopwatch m_swPostMoveBackDecelTimer = new Stopwatch();
        Stopwatch m_swStartMoveBackAccelTimer = new Stopwatch();

        MovableCamera m_cam = null;

        const double mc_dDecelAccelTimeMS = 150.0;

        Dictionary<MovableCamera.DIRECTION, double> m_dictMaxDecelMS = new Dictionary<MovableCamera.DIRECTION, double>();
        Dictionary<MovableCamera.DIRECTION, double> m_dictMaxAccelMS = new Dictionary<MovableCamera.DIRECTION, double>();

        public enum AccelModes { ACCEL, DECEL, ACCEL_AND_DECEL, FALL, ALL };
        public enum SWCommand { START, RESET };

        public StopWatchManager(MovableCamera cam) 
        {
            m_cam = cam;
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

        public double GetMaxMS(MovableCamera.DIRECTION dir, bool bAccel)
        {
            if(bAccel)
            {
                return m_dictMaxAccelMS[dir];
            }
            else
            {
                return m_dictMaxDecelMS[dir];
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

            LOGGER.Debug("Decel scale is " + dScale + " for elapsed of " + sw.ElapsedMilliseconds + " for direction " + dir);

            return dScale;
        }

        public double GetSpeedUpScale(MovableCamera.DIRECTION dir)
        {
            double dScale = 1.0;

            Stopwatch sw = GetStopWatch(dir, true);

            double dElapsedAdjusted = sw.ElapsedMilliseconds + (mc_dDecelAccelTimeMS - m_dictMaxAccelMS[dir]);

            dScale = dElapsedAdjusted / mc_dDecelAccelTimeMS * m_cam.GetStandardMovementScale();

            LOGGER.Debug("Accel scale is " + dScale + " for elapsed of " + sw.ElapsedMilliseconds + " for direction " + dir);

            return dScale;
        }

        public void HandleStartedMoving(MoveStates startedMovingStates, Dictionary<MovableCamera.DIRECTION, double> dictLastMoveScales)
        {
            /*if (startedMovingStates.AnyTrue())
			{
				m_swPostMoveDecelTimer.Reset(); // if start moving, stop decel timer for now

				m_eAccelingDirection = startedMovingStates.GetRelevant();
				m_dAccelMaxMS = (mc_dDecelAccelTimeMS - (m_dLastGameTickMoveScale / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));
				m_swStartMoveAccelTimer.Start();
				LOGGER.Debug("Started moving with accel move scale " + m_dLastGameTickMoveScale);
			}*/

            if (startedMovingStates.GetState(MovableCamera.DIRECTION.FORWARD))
            {
                m_swPostMoveForwardDecelTimer.Reset();

                m_dictMaxAccelMS[MovableCamera.DIRECTION.FORWARD] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.FORWARD] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveForwardAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.BACK))
            {
                m_swPostMoveBackDecelTimer.Reset();

                m_dictMaxAccelMS[MovableCamera.DIRECTION.BACK] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.BACK] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveBackAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.LEFT))
            {
                m_swPostMoveLeftDecelTimer.Reset();

                m_dictMaxAccelMS[MovableCamera.DIRECTION.LEFT] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.LEFT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveLeftAccelTimer.Start();
            }
            if (startedMovingStates.GetState(MovableCamera.DIRECTION.RIGHT))
            {
                m_swPostMoveRightDecelTimer.Reset();

                m_dictMaxAccelMS[MovableCamera.DIRECTION.RIGHT] = (mc_dDecelAccelTimeMS - (dictLastMoveScales[MovableCamera.DIRECTION.RIGHT] / m_cam.GetStandardMovementScale() * mc_dDecelAccelTimeMS));

                m_swStartMoveRightAccelTimer.Start();
            }
        }

        public void HandleStoppedMoving(MoveStates stoppedMovingStates, Dictionary<MovableCamera.DIRECTION, double> dictLastMoveScales)
        {
            if(stoppedMovingStates.GetState(MovableCamera.DIRECTION.FORWARD))
            {
                LOGGER.Debug("Stopped moving forward");

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

            return dAccelDecelScale;
        }
    }
}
