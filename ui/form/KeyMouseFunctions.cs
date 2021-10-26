using engine;
using System.Drawing;
using System.Windows.Forms;
using utilities;

namespace sharpq3load_ui
{
    public partial class GameWindow
    {
        /// <summary>
        /// Get last key combination hit for display purposes
        /// </summary>
        public Keys GetRecentKey
        {
            get { return m_RecentKey; }
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
            if(m_bRunning)
            {
                if(e.Button == MouseButtons.Middle)
                {
                    m_bMiddleMouseUp = true;
                }
            }
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
                m_Engine.MiddleMouseDown();
            }
            if(m_bMiddleMouseUp)
            {
                m_Engine.MiddleMouseUp();
            }

            m_bMiddleMouseUp = false;
            m_dictMouseButtonStates.Clear();
        }

        private void SetLastMoveStates()
        {
            bool bForward = false, bBackward = false, bLeft = false, bRight = false, bJump = false;
            m_dictKeyStates.TryGetValue(Keys.W, out bForward);
            m_dictKeyStates.TryGetValue(Keys.S, out bBackward);
            m_dictKeyStates.TryGetValue(Keys.A, out bLeft);
            m_dictKeyStates.TryGetValue(Keys.D, out bRight);
            m_dictKeyStates.TryGetValue(Keys.Space, out bJump);

            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.FORWARD, bForward);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.BACK, bBackward);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.LEFT, bLeft);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.RIGHT, bRight);
            m_lastTickMovestates.SetState(MovableCamera.DIRECTION.UP, bJump);
        }

        private void ProcessKeyStates(MoveStates stoppedMovingStates, MoveStates startedMovingStates)
        {
            bool bForward = false, bBackward = false, bLeft = false, bRight = false, bJump = false;
            m_dictKeyStates.TryGetValue(Keys.W, out bForward);
            m_dictKeyStates.TryGetValue(Keys.S, out bBackward);
            m_dictKeyStates.TryGetValue(Keys.A, out bLeft);
            m_dictKeyStates.TryGetValue(Keys.D, out bRight);
            m_dictKeyStates.TryGetValue(Keys.Space, out bJump);

            stoppedMovingStates.SetState(MovableCamera.DIRECTION.FORWARD, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.FORWARD) && !bForward ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.BACK, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.BACK) && !bBackward ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.LEFT, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.LEFT) && !bLeft ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.RIGHT, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.RIGHT) && !bRight ? true : false);
            stoppedMovingStates.SetState(MovableCamera.DIRECTION.UP, m_lastTickMovestates.GetState(MovableCamera.DIRECTION.UP) && !bJump ? true : false);

            startedMovingStates.SetState(MovableCamera.DIRECTION.FORWARD, !m_lastTickMovestates.GetState(MovableCamera.DIRECTION.FORWARD) && bForward ? true : false);
            startedMovingStates.SetState(MovableCamera.DIRECTION.BACK, !m_lastTickMovestates.GetState(MovableCamera.DIRECTION.BACK) && bBackward ? true : false);
            startedMovingStates.SetState(MovableCamera.DIRECTION.LEFT, !m_lastTickMovestates.GetState(MovableCamera.DIRECTION.LEFT) && bLeft ? true : false);
            startedMovingStates.SetState(MovableCamera.DIRECTION.RIGHT, !m_lastTickMovestates.GetState(MovableCamera.DIRECTION.RIGHT) && bRight ? true : false);
            startedMovingStates.SetState(MovableCamera.DIRECTION.UP, !m_lastTickMovestates.GetState(MovableCamera.DIRECTION.UP) && bJump ? true : false);

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
            if(bJump)
            {
                m_Engine.CacheMove(MovableCamera.DIRECTION.UP);
                m_lastTickMovestates.SetState(MovableCamera.DIRECTION.UP, true);
            }
        }

        private void SwitchToGhost()
        {
            DisableDebuggingMode();
            m_Engine.PreSwitchModes();
            m_Engine = new Ghost(m_Engine);
        }

        private void ShowHelp()
        {
            SetCursor(true, false);
            InfoForm nfo = new InfoForm();
            nfo.ShowDialog(this);
            SetCursor(false, false);
        }

        private void m_openGLControl_ProcessKey(object sender, KeyEventArgs e)
        {
            if (m_bOpeningMap) return;

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
                        m_Observer.Subscribe(p);
                        p.SetSoundManager(m_SoundManager);
                        m_Engine = p;

                    }
                    else if (e.KeyData == Keys.F2 && m_Engine.GetClass() != Engine.EEngineType.GHOST)
                    {
                        SwitchToGhost();
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
                        m_Engine.GraphicsMode = Engine.EGraphicsMode.TEXTURED_SHADED;
                    }                
                }

                if (e.KeyData == Keys.O)
                {
                    OpenMap();
                }             
                else if (e.KeyData == Keys.Q)
                {
                    ExitProgram();
                }
                else if (e.KeyData == Keys.H)
                {
                    ShowHelp();
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