using engine;
using System;
using System.Drawing;
using System.Windows.Forms;
using utilities;

namespace simulator
{
    public partial class SimulatorForm
    {
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

        private void openGLControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (TestMovementKeys(Keys.W, e.KeyData))
            {
                timerMoveForward.Stop();
            }
            if (TestMovementKeys(Keys.S, e.KeyData))
            {
                timerMoveBackward.Stop();
            }
            if (TestMovementKeys(Keys.A, e.KeyData) || TestMovementKeys(Keys.Left, e.KeyData))
            {
                timerStrafeLeft.Stop();
            }
            if (TestMovementKeys(Keys.D, e.KeyData) || TestMovementKeys(Keys.Right, e.KeyData))
            {
                timerStrafeRight.Stop();
            }
            if (TestMovementKeys(Keys.Up, e.KeyData))
            {
                timerMoveUp.Stop();
            }
            if (TestMovementKeys(Keys.Down, e.KeyData) || TestMovementKeys(Keys.Space, e.KeyData))
            {
                timerMoveDown.Stop();
            }
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
                if (m_bRunning && TestMovementKeys(Keys.W, e.KeyData))
                {
                    timerMoveBackward.Stop();
                    timerMoveForward.Start();
                }
                else if (m_bRunning && TestMovementKeys(Keys.S, e.KeyData))
                {
                    timerMoveForward.Stop();
                    timerMoveBackward.Start();
                }
                else if (m_bRunning && (TestMovementKeys(Keys.A, e.KeyData) || TestMovementKeys(Keys.Left, e.KeyData)))
                {
                    timerStrafeRight.Stop();
                    timerStrafeLeft.Start();
                }
                else if (m_bRunning && (TestMovementKeys(Keys.D, e.KeyData) || TestMovementKeys(Keys.Right, e.KeyData)))
                {
                    timerStrafeLeft.Stop();
                    timerStrafeRight.Start();
                }
                else if (m_bRunning && (TestMovementKeys(Keys.Up, e.KeyData)))
                {
                    timerMoveDown.Stop();
                    timerMoveUp.Start();
                }
                else if (m_bRunning && (TestMovementKeys(Keys.Down, e.KeyData) || TestMovementKeys(Keys.Space, e.KeyData)))
                {
                    timerMoveUp.Stop();
                    timerMoveDown.Start();
                }
                else if (m_bRunning && e.KeyData == Keys.F1 && m_Engine.GetClass() != Engine.EEngineType.PLAYER)
                {
                    m_Engine.PreSwitchModes();
                    Player p = new Player(m_Engine);
                    p.SetSoundManager(m_SoundManager);
                    m_Engine = p;

                }
                else if (m_bRunning && e.KeyData == Keys.F2 && m_Engine.GetClass() != Engine.EEngineType.GHOST)
                {
                    DisableDebuggingMode();
                    m_Engine.PreSwitchModes();
                    m_Engine = new Ghost(m_Engine);
                }
                else if (m_bRunning && e.KeyData == Keys.F3 && m_Engine.GetClass() != Engine.EEngineType.SPECTATOR)
                {
                    if (m_Engine.GetStaticFigList[0].GetNumViewPoints > 0)
                    {
                        DisableDebuggingMode();
                        m_Engine.PreSwitchModes();
                        m_Engine = new Spectator(m_Engine);
                    }
                }
                else if (m_bRunning && e.KeyData == Keys.F5)
                {
                    m_Engine.GraphicsMode = Engine.EGraphicsMode.WIREFRAME;
                }
                else if (m_bRunning && e.KeyData == Keys.F6)
                {
                    m_Engine.GraphicsMode = Engine.EGraphicsMode.SINGLE_WHITE;
                }
                else if (m_bRunning && e.KeyData == Keys.F7)
                {
                    m_Engine.GraphicsMode = Engine.EGraphicsMode.SINGLE_TEXTURE_VERTICE_COLOR;
                }
                else if (m_bRunning && e.KeyData == Keys.F8)
                {
                    m_Engine.GraphicsMode = Engine.EGraphicsMode.MULTI_TEXTURE_WHITE;
                }
                else if (e.KeyData == Keys.O)
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
                else if (m_bRunning)
                {
                    m_Engine.KeyDown(e);
                }
            }
        }
    }
}