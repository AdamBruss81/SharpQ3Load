using engine;
using System;
using System.Drawing;
using System.Windows.Forms;
using utilities;

namespace simulator
{
    public partial class SimulatorForm
    {
        private Engine.MOVES m_lastmoveFB = Engine.MOVES.NONE, m_lastmoveLR = Engine.MOVES.NONE;

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

            if (!bForward && !bBackward || bForward && bBackward)
            {
                m_lastmoveFB = Engine.MOVES.NONE;
            }
            if(!bLeft && !bRight || bLeft && bRight)
            {
                m_lastmoveLR = Engine.MOVES.NONE;
            }         
        }

        private void ProcessKeyStates(ref bool bStoppedMoving)
        {
            bool bForward = false, bBackward = false, bLeft = false, bRight = false;
            m_dictKeyStates.TryGetValue(Keys.W, out bForward);
            m_dictKeyStates.TryGetValue(Keys.S, out bBackward);
            m_dictKeyStates.TryGetValue(Keys.A, out bLeft);
            m_dictKeyStates.TryGetValue(Keys.D, out bRight);

            bool bMovedThisTick = false;
            if (bForward && !bBackward)
            {
                bMovedThisTick = true;
                m_Engine.MoveForward();
                m_lastmoveFB = Engine.MOVES.FORWARD;
            }
            if(bBackward && !bForward)
            {
                bMovedThisTick = true;
                m_Engine.MoveBackward();
                m_lastmoveFB = Engine.MOVES.BACK;
            }
            if(bLeft && !bRight)
            {
                bMovedThisTick = true;
                m_Engine.MoveLeft();
                m_lastmoveLR = Engine.MOVES.LEFT;
            }
            if(bRight && !bLeft)
            {
                bMovedThisTick = true;
                m_Engine.MoveRight();
                m_lastmoveLR = Engine.MOVES.RIGHT;
            }

            if(m_bMovedLastTick && !bMovedThisTick)
            {
                bStoppedMoving = true;
            }

            m_bMovedLastTick = bMovedThisTick;

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