/* *******************************************************************************************************************
 * Application: PomodoroTimer
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2025
 * Usage and reproduction in any manner whatsoever without the written permission of Daniel Liedke is strictly forbidden.
 *  
 * Purpose: Tooltip form for the timer, including full screen mode
 *           
 * *******************************************************************************************************************/

using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DEMAgentProcess
{
    public partial class ToolTipForm : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        #region Class Variables / Constructor

        private bool _isDragging = false;
        private Point _lastMousePosition;
        private bool _isFullScreen = false;
        private Screen _fullScreenTarget;
        private Point _originalPosition;
        private Size _normalSize;

        /// <summary>
        /// True while this window is covering a full screen.
        /// </summary>
        public bool IsFullScreen
        {
            get { return _isFullScreen; }
        }

        /// <summary>
        /// Screen where the small timer is placed. It is always recalculated from the
        /// small timer position, so it never gets stale after a monitor is added,
        /// removed or has its resolution changed.
        /// </summary>
        public Screen HomeScreen
        {
            get { return Screen.FromPoint(_originalPosition); }
        }

        /// <summary>
        /// Size used by the small timer. It is ignored while in full screen, otherwise
        /// the timer text width would shrink the full screen window back to a small box.
        /// </summary>
        public Size NormalSize
        {
            get { return _normalSize; }
            set
            {
                _normalSize = value;

                if (!_isFullScreen)
                {
                    this.Size = value;
                }
            }
        }

        public ToolTipForm()
        {
            InitializeComponent();
            lblText.MouseDown += lblText_MouseDown;
            lblText.MouseMove += lblText_MouseMove;
            lblText.MouseUp += lblText_MouseUp;
            this.MouseDown += ToolTipForm_MouseDown;
            this.MouseMove += ToolTipForm_MouseMove;
            this.MouseUp += ToolTipForm_MouseUp;

            // Load last location if it is within the screen bounds
            if (Properties.Settings.Default.ToolTipFormLocation != null)
            {
                // Get saved location
                Point savedLocation = Properties.Settings.Default.ToolTipFormLocation;
                Screen savedScreen = Screen.FromPoint(savedLocation);
                Rectangle screenBounds = savedScreen.Bounds;

                // If the location is within the screen bounds, set it and save new position
                if (screenBounds.Contains(savedLocation))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = savedLocation;
                }
                // If the location is not within the screen bounds,
                // set it to the center of the screen and save new position
                else
                {
                    this.Location = new Point(
                        savedScreen.Bounds.Width / 2 - this.Width / 2,
                        savedScreen.Bounds.Height / 2 - this.Height / 2);
                    Properties.Settings.Default.ToolTipFormLocation = this.Location;
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                Screen primaryScreen = Screen.PrimaryScreen;
                this.Location = new Point(
                    primaryScreen.Bounds.Width / 2 - this.Width / 2,
                    primaryScreen.Bounds.Height / 2 - this.Height / 2);
            }

            _originalPosition = this.Location;
            _normalSize = this.Size;
        }

        #endregion

        #region Set Tool Tip Text and Color

        public void SetToolTip(string text)
        {
            // Update the label text
            lblText.Text = text;

            // Ensure the label resizes to fit the text
            lblText.AutoSize = true;

            if (_isFullScreen)
            {
                CenterLabel();

                // Only for primary screen 
                if (_fullScreenTarget != null && _fullScreenTarget.Primary)
                {
                    // Focus window with Win32 API
                    SetForegroundWindow(this.Handle);
                }
            }
        }

        /// <summary>
        /// Shows this window covering the whole target screen, or restores it back to
        /// the small timer. The target screen is always explicit, the window is never
        /// maximized, otherwise Windows would decide in which monitor to show it.
        /// </summary>
        public void SetFullScreen(bool isFullScreen, TimerStatus timerStatus, Screen targetScreen = null)
        {
            _isFullScreen = isFullScreen;

            SetToolTipColor(timerStatus);

            if (isFullScreen)
            {
                _fullScreenTarget = targetScreen ?? HomeScreen;

                // Whole screen bounds and no border, so the break also covers the
                // taskbar. Being top most keeps it above the taskbar in every screen
                this.StartPosition = FormStartPosition.Manual;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = _fullScreenTarget.Bounds;
                this.TopMost = true;
                this.Cursor = Cursors.Default;

                // Increase lblText font size to 110
                lblText.Font = new Font(lblText.Font.Name, 110, lblText.Font.Style, lblText.Font.Unit);

                // Ensure the label resizes to fit the text
                lblText.AutoSize = true;

                // Put the label in the middle of the screen
                CenterLabel();

                // Add exit and +5 buttons, removing the previous ones if this window
                // was already in full screen (for example lunch switched to long break)
                RemoveButtons();
                AddButtons(this.ClientSize.Width, this.ClientSize.Height, timerStatus == TimerStatus.Break);

                // Full screen must always be visible, even when the small timer is hidden
                this.Show();
                this.BringToFront();
            }
            else
            {
                _fullScreenTarget = null;

                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                this.Cursor = Cursors.SizeAll;

                // Decrease lblText font size to 12
                lblText.Font = new Font(lblText.Font.Name, 12, lblText.Font.Style, lblText.Font.Unit);
                using (Graphics graphics = lblText.CreateGraphics())
                {
                    int dpiX = (int)graphics.DpiX;
                    int dpiY = (int)graphics.DpiY;
                    int labelX = (int)(12 * (dpiX / 96.0));
                    int labelY = (int)(3 * (dpiY / 96.0));
                    lblText.Location = new Point(labelX, labelY);
                }

                // Remove exit/+5 buttons if required
                RemoveButtons();

                // Restore to original size and position, visibility is up to the caller
                this.Size = _normalSize;
                this.Location = _originalPosition;
            }
        }

        private void CenterLabel()
        {
            // Measure the text size after updating the text
            lblText.Update();
            SizeF textSize;
            using (Graphics g = lblText.CreateGraphics())
            {
                textSize = g.MeasureString(lblText.Text, lblText.Font);
            }

            // Put label in the middle of the window considering DPI and label size
            lblText.Location = new Point(
                (this.ClientSize.Width - (int)textSize.Width) / 2,
                (this.ClientSize.Height - (int)textSize.Height) / 2);
        }

        private void SetToolTipColor(TimerStatus timerStatus)
        {
            // Red for regular break and long break
            if (timerStatus == TimerStatus.Break ||
                timerStatus == TimerStatus.LongBreak)
            {
                lblText.ForeColor = Color.Red;
            }
            // Green for lunch
            if (timerStatus == TimerStatus.Lunch)
            {
                lblText.ForeColor = Color.LightGreen;
            }
            // Orange for meeting
            if (timerStatus == TimerStatus.Meeting)
            {
                lblText.ForeColor = Color.Orange;
            }
            // Green for task
            if (timerStatus == TimerStatus.Task)
            {
                lblText.ForeColor = Color.LightGreen;
            }
        }

        private void AddButtons(int screenWidth, int screenHeight, bool add5)
        {
            // Add an exit button in the bottom right corner
            Button exitButton = new Button();
            exitButton.Text = "Exit";
            exitButton.Font = new Font(exitButton.Font.Name, 20, exitButton.Font.Style, exitButton.Font.Unit);
            exitButton.Size = new Size(200, 100);
            exitButton.Location = new Point(screenWidth - exitButton.Width - 20, screenHeight - exitButton.Height - 20);
            exitButton.ForeColor = Color.White;
            exitButton.Cursor = Cursors.Hand;
            exitButton.Focus();
            exitButton.Click += (sender, e) =>
            {
                // Remove the exit button and add 5 minutes button
                Button exitButton2 = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Exit");
                if (exitButton2 != null)
                {
                    exitButton2.Visible = false;
                }
                Button addMinutesButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "+5 Minutes");
                if (addMinutesButton != null)
                {
                    addMinutesButton.Visible = false;
                }

                // Switch back to task
                MainForm mainForm = (MainForm)Application.OpenForms["MainForm"];
                mainForm.SwitchToTask();
            };
            this.Controls.Add(exitButton);

            if (add5)
            {
                // Add a "+5 Minutes" button next to the exit button
                if (((MainForm)Application.OpenForms["MainForm"]).CurrentStatus == TimerStatus.Break)
                {
                    Button addMinutesButton = new Button();
                    addMinutesButton.Text = "+5 Minutes";
                    addMinutesButton.Font = new Font(addMinutesButton.Font.Name, 20, addMinutesButton.Font.Style, addMinutesButton.Font.Unit);
                    addMinutesButton.Size = new Size(400, 100);
                    addMinutesButton.Location = new Point(screenWidth - addMinutesButton.Width - exitButton.Width - 40, screenHeight - addMinutesButton.Height - 20);
                    addMinutesButton.ForeColor = Color.White;
                    addMinutesButton.Cursor = Cursors.Hand;
                    addMinutesButton.Click += (sender, e) =>
                    {
                        // Add 5 minutes (300 seconds)
                        MainForm mainForm = (MainForm)Application.OpenForms["MainForm"];
                        mainForm.RemainingTime += 300;
                    };
                    this.Controls.Add(addMinutesButton);
                }
            }
        }

        private void RemoveButtons()
        {
            // Remove the exit button and add 5 minutes button
            Button exitButton2 = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Exit");
            if (exitButton2 != null)
            {
                this.Controls.Remove(exitButton2);
                exitButton2.Dispose();
            }
            Button addMinutesButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "+5 Minutes");
            if (addMinutesButton != null)
            {
                this.Controls.Remove(addMinutesButton);
                addMinutesButton.Dispose();
            }
        }

        #endregion

        #region Form/Label Move with Dragging

        private void lblText_MouseDown(object sender, MouseEventArgs e)
        {
            StartDragging(e);
        }

        private void lblText_MouseMove(object sender, MouseEventArgs e)
        {
            Drag(e);
        }


        private void lblText_MouseUp(object sender, MouseEventArgs e)
        {
            EndDragging();
        }

        private void ToolTipForm_MouseUp(object sender, MouseEventArgs e)
        {
            EndDragging();
        }

        private void ToolTipForm_MouseDown(object sender, MouseEventArgs e)
        {
            StartDragging(e);
        }

        private void ToolTipForm_MouseMove(object sender, MouseEventArgs e)
        {
            Drag(e);
        }

        private void StartDragging(MouseEventArgs e)
        {
            // Full screen windows cannot be dragged, otherwise the break would be
            // moved out of its monitor and the small timer position would be lost
            if (_isFullScreen)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.Location;
            }
        }

        private void Drag(MouseEventArgs e)
        {
            if (_isDragging)
            {
                this.Location = new Point(
                    this.Location.X + e.X - _lastMousePosition.X,
                    this.Location.Y + e.Y - _lastMousePosition.Y);
            }
        }

        private void EndDragging()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            // Save new location, the screen is recalculated from it when required
            _originalPosition = this.Location;
            Properties.Settings.Default.ToolTipFormLocation = _originalPosition;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Form Closing

        private void ToolTipForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Esc key was pressed
            if (e.KeyCode == Keys.Escape)
            {
                // If full screen and esc key, click in the exit button
                if (_isFullScreen)
                {
                    Button exitButton = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Exit");
                    if (exitButton != null)
                    {
                        exitButton.PerformClick();
                    }
                }
            }
        }

        private void ToolTipForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Never save a full screen position, it is not the small timer position
            if (_isFullScreen)
            {
                return;
            }

            // If this.Location is not 0,0
            if (this.Location.X > 0 && this.Location.Y > 0)
            {
                Properties.Settings.Default.ToolTipFormLocation = this.Location;
                Properties.Settings.Default.Save();
            }
        }

        #endregion
    }
}