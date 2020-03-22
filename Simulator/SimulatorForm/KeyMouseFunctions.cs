using engine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using utilities;

namespace simulator
{
    public partial class SimulatorForm
    {
        MoveStates m_PreviousTickMoveStates = new MoveStates();

        /// <summary>
        /// Get last key combination hit for display purposes
        /// </summary>
        public Keys GetRecentKey
        {
            get { return m_RecentKey; }
        }

        private bool TestMovementKeys(Keys targetKey, Keys actualKey)
        {
            return (actualKey == targetKey || (Convert.ToInt32(actualKey) == (Convert.ToInt32(Keys.Shift) + Convert.ToInt32(targetKey))) ||
                (Convert.ToInt32(actualKey) == (Convert.ToInt32(Keys.Control) + Convert.ToInt32(targetKey))));
        }

        // Create a bullet
        // Move created bullet along rho
        private void openGLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (m_bRunning)
            {               
                m_dictMouseButtonStates[e.Button] = true;
            }
        }

        private void m_openGLControl_MouseUp(object sender, MouseEventArgs e)
        {
        }

        private void openGLControl_KeyUp(object sender, KeyEventArgs e)
        {            
            m_dictKeyStates[e.KeyCode] = false;
        }

        private void ProcessMouseButtons()
        {
            bool bLeft = false, bMiddle = false, bRight = false;

            m_dictMouseButtonStates.TryGetValue(MouseButtons.Left, out bLeft);
            m_dictMouseButtonStates.TryGetValue(MouseButtons.Middle, out bMiddle);
            m_dictMouseButtonStates.TryGetValue(MouseButtons.Right, out bRight);
            
            if(bLeft)
            {
                m_Engine.LeftMouseDown();
            }
            if(bRight)
            {
                m_Engine.RightMouseDown();
            }
            if(bMiddle)
            {
                
            }

            m_dictMouseButtonStates.Clear();
        }

        private void SetLastMoveStates()
        {
            bool bForward = false, bBackward = false, bLeft = false, bRight = false;
            m_dictKeyStates.TryGetValue(Keys.W, out bForward);
            m_dictKeyStates.TryGetValue(Keys.S, out bBackward);
            m_dictKeyStates.TryGetValue(Keys.A, out bLeft);
            m_dictKeyStates.TryGetValue(Keys.D, out bRight);

            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.FORWARD, bForward);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.BACK, bBackward);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.LEFT, bLeft);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.RIGHT, bRight);       
        }

        private void ProcessKeyStates(MoveStates stoppedMovingStates)
        {
            bool bForward = false, bBackward = false, bLeft = false, bRight = false;
            m_dictKeyStates.TryGetValue(Keys.W, out bForward);
            m_dictKeyStates.TryGetValue(Keys.S, out bBackward);
            m_dictKeyStates.TryGetValue(Keys.A, out bLeft);
            m_dictKeyStates.TryGetValue(Keys.D, out bRight);

            stoppedMovingStates.SetState(MovableCamera.DIRECTION.FORWARD, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.FORWARD) && !bForward ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.BACK, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.BACK) && !bBackward ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.LEFT, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.LEFT) && !bLeft ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.RIGHT, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.RIGHT) && !bRight ? true : false);

            stoppedMovingStates.SetState(MovableCamera.DIRECTION.FORWARD_LEFT, (m_lastTickMovestates.GetState(MovableCamera.DIRECTION.FORWARD_LEFT) && !bForward && !bLeft) ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.FORWARD_RIGHT, (m_lastTickMovestates.GetState(MovableCamera.DIRECTION.FORWARD_RIGHT) && !bForward && !bRight) ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.BACK_LEFT, (m_lastTickMovestates.GetState(MovableCamera.DIRECTION.BACK_LEFT) && !bBackward && !bLeft) ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.BACK_RIGHT, (m_lastTickMovestates.GetState(MovableCamera.DIRECTION.BACK_RIGHT) && !bBackward && !bRight) ? true : false);

            m_lastTickMovestates.Clear();

            // determine which way to move 
            if (bForward && !bBackward)
            {
                m_Engine.CacheMove(MovableCamera.DIRECTION.FORWARD);
                m_Engine.MoveForward();
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.FORWARD, true);
            }
            if(bBackward && !bForward)
            {
                m_Engine.CacheMove(MovableCamera.DIRECTION.BACK);
                m_Engine.MoveBackward();
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.BACK, true);
            }
            if(bLeft && !bRight)
            {
                m_Engine.CacheMove(MovableCamera.DIRECTION.LEFT);
                m_Engine.MoveLeft();
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.LEFT, true);
            }
            if(bRight && !bLeft)
            {
                m_Engine.CacheMove(MovableCamera.DIRECTION.RIGHT);
                m_Engine.MoveRight();
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.RIGHT, true);
            }    
            if(bForward && bLeft)
            {
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.FORWARD_LEFT, true);
            }
            if (bForward && bRight)
            {
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.FORWARD_RIGHT, true);
            }
            if (bBackward && bLeft)
            {
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.BACK_LEFT, true);
            }
            if (bBackward && bRight)
            {
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.BACK_RIGHT, true);
            }

            m_Engine.Fall();
        }

        private void m_openGLControl_ProcessKey(object sender, KeyEventArgs e)
        {
            m_RecentKey = e.KeyData;

            if (Control.ModifierKeys == Keys.Shift)
            {
                if (e.KeyCode == Keys.P)
                {
                    Bitmap screencap = ImageHelper.GetImage(m_openGLControl.Size);
                    Clipboard.SetImage(screencap);
                    screencap.Dispose();
                }
            }
            else
            {
                if (m_bRunning)
                {
                    m_dictKeyStates[e.KeyCode] = true;

                    if (e.KeyData == Keys.F1 && m_Engine.GetClass() != Engine.EEngineType.PLAYER)
                    {
                        m_Engine.PreSwitchModes();
                        Player p = new Player(m_Engine);
                        p.SetSoundManager(m_SoundManager);
                        m_Engine = p;

                    }
                    else if (e.KeyData == Keys.F2 && m_Engine.GetClass() != Engine.EEngineType.GHOST)
                    {
                        DisableDebuggingMode();
                        m_Engine.PreSwitchModes();
                        m_Engine = new Ghost(m_Engine);
                    }
                    else if (e.KeyData == Keys.F3 && m_Engine.GetClass() != Engine.EEngineType.SPECTATOR)
                    {
                        if (m_Engine.GetStaticFigList[0].GetNumViewPoints > 0)
                        {
                            DisableDebuggingMode();
                            m_Engine.PreSwitchModes();
                            m_Engine = new Spectator(m_Engine);
                        }
                    }
                    else if (e.KeyData == Keys.F5)
                    {
                        m_Engine.GraphicsMode = Engine.EGraphicsMode.WIREFRAME;
                    }
                    else if (e.KeyData == Keys.F6)
                    {
                        m_Engine.GraphicsMode = Engine.EGraphicsMode.SINGLE_WHITE;
                    }
                    else if (e.KeyData == Keys.F7)
                    {
                        m_Engine.GraphicsMode = Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR;
                    }
                    else if (e.KeyData == Keys.F8)
                    {
                        m_Engine.GraphicsMode = Engine.EGraphicsMode.MULTI_TEXTURE_WHITE;
                    }
                }

                if (e.KeyData == Keys.O)
                {
                    OpenMap();
                }
                else if (e.KeyData == Keys.L)
                {
                    OpenMapFromFile();
                }
                else if (e.KeyData == Keys.C)
                {
                    CloseMap();
                }
                else if (e.KeyData == Keys.Q)
                {
                    static_theEngine = null;
                    static_theMap = null;

                    if (m_Engine != null) m_Engine.Delete();
                    if (m_fonter != null) m_fonter.Delete();

                    m_SoundManager.Dispose();

                    while(!m_SoundManager.GetPlaybackStopped())
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    Close();
                }
                else if (e.KeyData == Keys.H)
                {
                    SetCursor(true, false);
                    InfoForm nfo = new InfoForm();
                    nfo.ShowDialog(this);
                    SetCursor(false, false);
                }
                else if (e.KeyData == Keys.P)
                {
                    STATE.AllowPrinting = !STATE.AllowPrinting;
                }
                
                if (m_bRunning)
                {
                    m_Engine.KeyDown(e);
                }
            }
        }
    }
}