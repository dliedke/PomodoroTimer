﻿/* *******************************************************************************************************************
 * Application: PomodoroTimer
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2024
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

namespace PomodoroTimer
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
                Rectangle screenBounds = Screen.FromPoint(savedLocation).Bounds;

                // If the location is within the screen bounds, set it and save new postion
                if (screenBounds.Contains(savedLocation))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = savedLocation;
                }
                // If the location is not within the screen bounds,
                // set it to the center of the screen and save new postion
                else
                {
                    this.Location = new Point(
                        Screen.PrimaryScreen.Bounds.Width / 2 - this.Width / 2,
                        Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2);
                    Properties.Settings.Default.ToolTipFormLocation = this.Location;
                    Properties.Settings.Default.Save();
                }
            }
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
                // Measure the text size after updating the text
                lblText.Update();
                SizeF textSize;
                using (Graphics g = lblText.CreateGraphics())
                {
                    textSize = g.MeasureString(lblText.Text, lblText.Font);
                }

                // Determine the screen where the tooltip form is displayed
                Screen targetScreen = Screen.FromControl(this);

                // Put label in the middle of the screen considering DPI and label size
                int screenWidth = targetScreen.WorkingArea.Width;
                int screenHeight = targetScreen.WorkingArea.Height;
                int labelWidth = (int)textSize.Width;
                int labelHeight = (int)textSize.Height;
                int labelX = (screenWidth - labelWidth) / 2;
                int labelY = (screenHeight - labelHeight) / 2;
                lblText.Location = new Point(labelX, labelY);

                // Only for primary screen 
                if (targetScreen.Primary)
                {
                    // Focus window with Win32 API
                    SetForegroundWindow(this.Handle);
                }
            }
        }

        public void SetFullScreen(bool isFullScreen, TimerStatus timerStatus, bool secondMonitor = false)
        {
            _isFullScreen = isFullScreen;

            SetToolTipColor(timerStatus);

            if (isFullScreen)
            {
                // If secondMonitor is true, move this window to the other monitor
                if (secondMonitor && Screen.AllScreens.Length > 1)
                {
                    // Get the screen that is secondary
                    Screen secondScreen = Screen.AllScreens.FirstOrDefault(s => !s.Primary);
                    if (secondScreen != null)
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(
                            secondScreen.Bounds.Left + secondScreen.Bounds.Width / 2 - this.Width / 2,
                            secondScreen.Bounds.Top + secondScreen.Bounds.Height / 2 - this.Height / 2);
                        this.WindowState = FormWindowState.Normal; // Important to reset the state before maximizing
                        this.WindowState = FormWindowState.Maximized;
                    }
                }
                else
                {
                    this.StartPosition = FormStartPosition.CenterScreen;
                    this.WindowState = FormWindowState.Normal; // Important to reset the state before maximizing
                    this.WindowState = FormWindowState.Maximized;
                }

                // Allow the taskbar to be displayed
                this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                this.Cursor = Cursors.Default;

                // Increase lblText font size to 110
                lblText.Font = new Font(lblText.Font.Name, 110, lblText.Font.Style, lblText.Font.Unit);

                // Ensure the label resizes to fit the text
                lblText.AutoSize = true;

                // Update the label size and position after setting the new font
                lblText.Update();
                SizeF textSize;
                using (Graphics g = lblText.CreateGraphics())
                {
                    textSize = g.MeasureString(lblText.Text, lblText.Font);
                }

                // Put label in the middle of the screen considering DPI and label size
                Screen targetScreen = secondMonitor && Screen.AllScreens.Length > 1 ? Screen.AllScreens.FirstOrDefault(s => !s.Primary) : Screen.PrimaryScreen;
                if (targetScreen != null)
                {
                    int screenWidth = targetScreen.WorkingArea.Width;
                    int screenHeight = targetScreen.WorkingArea.Height;
                    int labelWidth = (int)textSize.Width;
                    int labelHeight = (int)textSize.Height;
                    int labelX = (screenWidth - labelWidth) / 2;
                    int labelY = (screenHeight - labelHeight) / 2;
                    lblText.Location = new Point(labelX, labelY);

                    // Add exit and +5 buttons if required
                    AddButtons(screenWidth, screenHeight, timerStatus == TimerStatus.Break);
                }
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
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

                // Load last location
                if (Properties.Settings.Default.ToolTipFormLocation != null &&
                    Properties.Settings.Default.ToolTipFormLocation.X > 0 &&
                    Properties.Settings.Default.ToolTipFormLocation.Y > 0)
                {
                    this.Location = Properties.Settings.Default.ToolTipFormLocation;
                }
            }

            // Ensure the form is brought to the front
            this.BringToFront();
            this.Show();
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
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.Location;
            }
        }

        private void lblText_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                this.Location = new Point(
                    this.Location.X + e.X - _lastMousePosition.X,
                    this.Location.Y + e.Y - _lastMousePosition.Y);
            }
        }

        private void lblText_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;

            // Save new location
            Properties.Settings.Default.ToolTipFormLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        private void ToolTipForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.Location;
            }
        }

        private void ToolTipForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                this.Location = new Point(
                    this.Location.X + e.X - _lastMousePosition.X,
                    this.Location.Y + e.Y - _lastMousePosition.Y);
            }
        }

        private void ToolTipForm_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;

            // Save new location
            Properties.Settings.Default.ToolTipFormLocation = this.Location;
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
            Properties.Settings.Default.ToolTipFormLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        #endregion
    }
}