/* *******************************************************************************************************************
 * Application: PomodoroTimer
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2024
 * Usage and reproduction in any manner whatsoever without the written permission of Daniel Liedke is strictly forbidden.
 *  
 * Purpose: Main form to control the pomodoro timer
 *           
 * *******************************************************************************************************************/

using System;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace PomodoroTimer
{
    public enum TimerStatus
    {
        Task,
        Meeting,
        Break,
        LongBreak,
        Lunch
    }

    public partial class MainForm : Form
    {
        #region Class Variables / Constructor

        private System.ComponentModel.IContainer components;

        private int _taskDuration;
        private int _breakDuration;
        private bool _fullScreenBreak;
        private NotifyIcon _notifyIcon;
        private System.Windows.Forms.Timer _timer;

        private new ContextMenuStrip ContextMenu;

        private ToolStripMenuItem pauseToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator1;

        private ToolStripMenuItem goToTaskToolStripMenuItem;
        private ToolStripMenuItem goToBreakToolStripMenuItem;
        private ToolStripMenuItem goToMeetingToolStripMenuItem;
        private ToolStripMenuItem goToLunchToolStripMenuItem;
        private ToolStripMenuItem goToLongBreakToolStripMenuItem;

        private ToolStripSeparator toolStripMenuItemSeparator3;

        private ToolStripMenuItem configureToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator4;

        private ToolStripMenuItem totalBreaksTimeToolStripMenuItem;
        private ToolStripMenuItem totalTasksTimeToolStripMenuItem;
        private ToolStripMenuItem totalMeetingTimeToolStripMenuItem;
        private ToolStripMenuItem totalLunchTimeToolStripMenuItem;
        private ToolStripMenuItem totalLongBreakTimeToolStripMenuItem;
        private ToolStripMenuItem totalWorkTimeToolStripMenuItem;
        private ToolStripMenuItem totalRestTimeToolStripMenuItem;
        private ToolStripMenuItem totalTimeToolStripMenuItem;
        private ToolStripMenuItem totalBreaksCountToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator2;

        private ToolStripMenuItem exitToolStripMenuItem;

        private ToolTip toolTip;
        bool _hideToolTipTimer = false;
        private ToolTipForm _toolTipForm1 = new ToolTipForm();
        private ToolTipForm _toolTipForm2 = new ToolTipForm();

        private int _totalBreakTime;
        private int _totalMeetingTime;
        private int _totalLunchTime;
        private int _totalLongBreakTime;
        private int _totalBreaksCount;
        private Label lblText;
        private int _totalTaskTime;
        private System.Drawing.Size _originalToolTipFormSize;

        private TimerStatus _previousStatus = TimerStatus.Task;
        private int _remainingTime;
        private ToolStripSeparator toolStripMenuItem1;

        public int RemainingTime
        {
            get { return _remainingTime; }
            set { _remainingTime = value; }
        }

        private TimerStatus _currentStatus;
        public TimerStatus CurrentStatus
        {
            get { return _currentStatus; }
            set { _currentStatus = value; }
        }



        public MainForm()
        {
            InitializeComponent();

            _originalToolTipFormSize = _toolTipForm1.Size;

            // If no location is set, put _toolTipForm in center of screen
            if (Properties.Settings.Default.ToolTipFormLocation == null ||
                (Properties.Settings.Default.ToolTipFormLocation.X == 0 &&
                 Properties.Settings.Default.ToolTipFormLocation.Y == 0))
            {
                _toolTipForm1.StartPosition = FormStartPosition.Manual;
                _toolTipForm1.Location = new System.Drawing.Point(
                    Screen.PrimaryScreen.Bounds.Width / 2 - _toolTipForm1.Width / 2,
                    Screen.PrimaryScreen.Bounds.Height / 2 - _toolTipForm1.Height / 2);
            }
            else
            {
                _toolTipForm1.Location = Properties.Settings.Default.ToolTipFormLocation;
            }

            LoadSettings();
            LoadMetrics();
            InitializeTimer();

            // Help tooltip when initializing
            _notifyIcon.Text = "Left click to hide/show timer\r\nALT+F12 or Right click show menu";

            // Add app version to the exit menu item
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = $"Pomodoro Timer v{version.Major}.{version.Minor}";
            exitToolStripMenuItem.Text += $" ({versionString})";

            // Register ALT+F12 hotkey
            RegisterHotKey(this.Handle, HOTKEY_ID_F12, 0x0001 /* MOD_ALT */, (uint)Keys.F12);
        }

        #endregion

        #region Timer Control for Task/Break/Meeting

        private void InitializeTimer()
        {
            _timer?.Stop();
            _timer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _timer.Tick += Timer_Tick;
            _remainingTime = _taskDuration;
            _currentStatus = TimerStatus.Task;

            UpdateDisplay();
            UpdateFullScreenStatus();

            StartTimer();
        }

        private void StartTimer()
        {
            _timer.Start();
            pauseToolStripMenuItem.Text = "Pause timer (&0)";
        }

        private void StopTimer()
        {
            _timer.Stop();
            pauseToolStripMenuItem.Text = "Continue timer (&0)";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Hide this loading window
            if (WindowState != FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                this.Hide();
            }

            // Decrement the remaining time if not in meeting status
            if (_currentStatus != TimerStatus.Meeting)
            {
                _remainingTime--;
            }
            else
            {
                _remainingTime++;
            }

            // Update the display
            UpdateDisplay();

            // Update the total task, break or meeting time
            switch (_currentStatus)
            {
                case TimerStatus.Task:
                    _totalTaskTime++;
                    break;
                case TimerStatus.Break:
                    _totalBreakTime++;
                    break;
                case TimerStatus.Meeting:
                    _totalMeetingTime++;
                    break;
                case TimerStatus.Lunch:
                    _totalLunchTime++;
                    break;
                case TimerStatus.LongBreak:
                    _totalLongBreakTime++;
                    break;
            }


            // Increment _totalBreaksCount when switching from a break, lunch break, or long break to a different status
            if ((_previousStatus == TimerStatus.Break || _previousStatus == TimerStatus.Lunch || _previousStatus == TimerStatus.LongBreak) &&
                (_currentStatus != TimerStatus.Break && _currentStatus != TimerStatus.Lunch && _currentStatus != TimerStatus.LongBreak))
            {
                _totalBreaksCount++;
            }

            _previousStatus = _currentStatus;

            // Play a melody if the remaining time is 10 seconds
            if (_remainingTime == 10 && _currentStatus != TimerStatus.Meeting)
            {
                PlayMelody(_currentStatus == TimerStatus.Task);
            }

            // Switch to the other time if the remaining time is 0
            if (_remainingTime == 0 && _currentStatus != TimerStatus.Meeting)
            {
                if (_currentStatus == TimerStatus.Task)
                {
                    SwitchToBreak();
                }
                else
                {
                    SwitchToTask();
                }

                // Update the total times
                UpdateTotalTimes();
            }
        }

        public void SwitchToTask()
        {
            PauseContinueTimer(pause: false);

            TimerStatus oldStatus = _currentStatus;

            _currentStatus = TimerStatus.Task;
            _remainingTime = _taskDuration;

            UpdateDisplay();
            UpdateFullScreenStatus();

            if (oldStatus != TimerStatus.Meeting &&
                oldStatus != TimerStatus.Task)
            {
                SetTeamStatus("Busy");
            }
        }

        private void SwitchToBreak()
        {
            PauseContinueTimer(pause: false);

            SetTeamStatus("AwayBRB");

            _currentStatus = TimerStatus.Break;
            _remainingTime = _breakDuration;

            UpdateDisplay();
            UpdateFullScreenStatus();
        }

        private void SwitchToMeeting()
        {
            PauseContinueTimer(pause: false);

            _currentStatus = TimerStatus.Meeting;
            _remainingTime = 0;

            UpdateDisplay();
            UpdateFullScreenStatus();
        }

        private void SwitchToLunch()
        {
            PauseContinueTimer(pause: false);
            SetTeamStatus("Away");

            _currentStatus = TimerStatus.Lunch;
            _remainingTime = 0;

            UpdateDisplay();
            UpdateFullScreenStatus();
        }

        private void SwitchToLongBreak()
        {
            PauseContinueTimer(pause: false);
            SetTeamStatus("Away");

            _currentStatus = TimerStatus.LongBreak;
            _remainingTime = 0;

            UpdateDisplay();
            UpdateFullScreenStatus();
        }

        private void UpdateDisplay()
        {
            int hours = Math.Abs(_remainingTime) / 3600;
            int minutes = (Math.Abs(_remainingTime) % 3600) / 60;
            int seconds = Math.Abs(_remainingTime) % 60;
            string time;

            // Show time in 00:00:00 format
            // With hours as optional
            if (_currentStatus == TimerStatus.Meeting || _currentStatus == TimerStatus.Lunch)
            {
                time = $"{hours:00}:{minutes:00}:{seconds:00}";

                // Increase _toolTipForm.Size by 13.5%
                _toolTipForm1.Size = new System.Drawing.Size((int)(_originalToolTipFormSize.Width * 1.35), _originalToolTipFormSize.Height);
            }
            else if (hours > 0)
            {
                time = $"{hours:00}:{minutes:00}:{seconds:00}";

                // Increase _toolTipForm.Size by 12%
                _toolTipForm1.Size = new System.Drawing.Size((int)(_originalToolTipFormSize.Width * 1.2), _originalToolTipFormSize.Height);
            }
            else
            {
                time = $"{minutes:00}:{seconds:00}";
                _toolTipForm1.Size = _originalToolTipFormSize;
            }

            // Get task, break, meeting, or lunch string
            string statusText = Enum.GetName(typeof(TimerStatus), _currentStatus);

            if (statusText == "LongBreak")
            {
                statusText = "Long Break";
            }

            // Create text with task, break, meeting, or lunch and time
            string notificationText = $"{statusText} - {time}";

            // Update tooltip text if required
            if (_hideToolTipTimer)
            {
                _notifyIcon.Text = notificationText;
            }

            // Update times for break, long break, task, meeting, lunch, and total
            UpdateTotalTimes();

            // Update big tooltip text
            ShowToolTip(notificationText);
        }

        private void UpdateFullScreenStatus()
        {
            // Change icon
            if (_currentStatus == TimerStatus.Task || _currentStatus == TimerStatus.Meeting || _currentStatus == TimerStatus.Lunch)
            {
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.greenball.ico"));
            }
            else
            {
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.redball.ico"));
            }

            // Show break as full screen or regular break
            // Full screen lunch and long break
            if ((_currentStatus == TimerStatus.Break && Properties.Settings.Default.BreakFullScreen) ||
                (_currentStatus == TimerStatus.Lunch || _currentStatus == TimerStatus.LongBreak))
            {
                _toolTipForm1.SetFullScreen(true, _currentStatus);
                _toolTipForm2.SetFullScreen(true, _currentStatus, secondMonitor: true);
            }
            else
            {
                _toolTipForm1.SetFullScreen(false, _currentStatus);
                _toolTipForm2.SetFullScreen(false, _currentStatus);
                _toolTipForm2.Visible = false;
            }
        }

        private void UpdateTotalTimes()
        {
            // Update Break/Task/Meeting/Total times
            totalBreaksTimeToolStripMenuItem.Text = $"Total Break Time: {TimeSpan.FromSeconds(_totalBreakTime):hh\\:mm\\:ss}";
            totalTasksTimeToolStripMenuItem.Text = $"Total Task Time: {TimeSpan.FromSeconds(_totalTaskTime):hh\\:mm\\:ss}";
            totalMeetingTimeToolStripMenuItem.Text = $"Total Meeting Time: {TimeSpan.FromSeconds(_totalMeetingTime):hh\\:mm\\:ss}";
            totalLunchTimeToolStripMenuItem.Text = $"Total Lunch Time: {TimeSpan.FromSeconds(_totalLunchTime):hh\\:mm\\:ss}";
            totalLongBreakTimeToolStripMenuItem.Text = $"Total Long Break Time: {TimeSpan.FromSeconds(_totalLongBreakTime):hh\\:mm\\:ss}";

            // Calculate total work time (task + meeting)
            int totalWorkTime = _totalTaskTime + _totalMeetingTime;
            totalWorkTimeToolStripMenuItem.Text = $"Total Work Time: {TimeSpan.FromSeconds(totalWorkTime):hh\\:mm\\:ss}";

            // Calculate total rest time (break + long break + lunch)
            int totalRestTime = _totalBreakTime + _totalLongBreakTime + _totalLunchTime;
            totalRestTimeToolStripMenuItem.Text = $"Total Rest Time: {TimeSpan.FromSeconds(totalRestTime):hh\\:mm\\:ss}";

            // Calculate total time today (work + rest)
            int totalTime = totalWorkTime + totalRestTime;
            totalTimeToolStripMenuItem.Text = $"Total Time Today: {TimeSpan.FromSeconds(totalTime):hh\\:mm\\:ss}";

            // Update total breaks count
            totalBreaksCountToolStripMenuItem.Text = $"Total Breaks Today: {_totalBreaksCount}";
        }

        #endregion

        #region Hide/Show Timer and Notifications

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // If right click, show context menu
            if (e.Button == MouseButtons.Right)
            {
                // Delay to avoid menu closing automatically sometimes
                System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                {
                    Invoke(new Action(() =>
                    {
                        MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                        mi.Invoke(_notifyIcon, null);
                    }));
                });
                _notifyIcon.Text = "";
            }

            // If left click, hide/show timer
            if (e.Button == MouseButtons.Left)
            {
                _hideToolTipTimer = !_hideToolTipTimer;

                if (!_hideToolTipTimer)
                {
                    _notifyIcon.Text = "";
                }
            }
        }

        private void ShowToolTip(string message)
        {
            _toolTipForm1.SetToolTip(message);

            if (_toolTipForm2.Visible)
            {
                _toolTipForm2.SetToolTip(message);
            }

            if (!_hideToolTipTimer)
            {
                _toolTipForm1.Show();
            }
            else
            {
                _toolTipForm1.Hide();
            }
        }

        public void PlayMelody(bool isTask)
        {
            Task.Run(() =>
            {
                int tempo = 300;
                int[] frequencies;
                int[] durations;

                if (isTask)
                {
                    // Task sound
                    frequencies = new int[] { 523, 523 };
                    durations = new int[] { tempo, tempo };
                }
                else
                {
                    // Break sound
                    frequencies = new int[] { 1047, 1047 };
                    durations = new int[] { tempo, tempo };
                }

                for (int i = 0; i < frequencies.Length; i++)
                {
                    Console.Beep(frequencies[i], durations[i]);
                    System.Threading.Thread.Sleep(tempo / 4);
                }
            });
        }

        #endregion

        #region Context Menus
        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseContinueTimer(_timer.Enabled);
        }

        public void PauseContinueTimer(bool pause)
        {
            if (pause)
            {
                StopTimer();
                pauseToolStripMenuItem.Text = "Continue timer (&0)";
            }
            else
            {
                StartTimer();
                pauseToolStripMenuItem.Text = "Pause timer (&0)";
            }
        }

        private void goToBreakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToBreak();
        }

        private void goToTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToTask();
        }

        private void goToMeetingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToMeeting();
        }

        private void goToLunchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToLunch();
        }

        private void goToLongBreakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToLongBreak();
        }

        private void totalTasksTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            goToTaskToolStripMenuItem_Click(sender, e);
        }

        private void totalMeetingTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            goToMeetingToolStripMenuItem_Click((object)sender, e);
        }

        private void totalBreaksTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            goToBreakToolStripMenuItem_Click((object)sender, e);
        }

        private void totalLongBreakTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            goToLongBreakToolStripMenuItem_Click((object)sender, e);
        }

        private void totalLunchTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            goToLunchToolStripMenuItem_Click((object)sender, e);
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open configuration form
            ConfigurationForm configForm = new ConfigurationForm
            {
                FullScreenBreak = _fullScreenBreak,
                TaskDuration = _taskDuration,
                BreakDuration = _breakDuration
            };

            // Save configuration if saved button was clicked
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                _taskDuration = configForm.TaskDuration;
                _breakDuration = configForm.BreakDuration;
                _fullScreenBreak = configForm.FullScreenBreak;

                SaveSettings();
                InitializeTimer();
            }
        }

        #endregion

        #region Teams Status Automation

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;


        private static Point GetTeamsTrayIconPosition()
        {
            AutomationElement teamsIcon = GetTeamsTrayIcon();
            if (teamsIcon != null)
            {
                System.Windows.Rect iconRectWin = teamsIcon.Current.BoundingRectangle;
                Rectangle iconRect = new Rectangle((int)iconRectWin.Left, (int)iconRectWin.Top, (int)iconRectWin.Right - (int)iconRectWin.Left, (int)iconRectWin.Bottom - (int)iconRectWin.Top);
                Point iconCenter = new Point(iconRect.Left + (iconRect.Width / 2), iconRect.Top + (iconRect.Height / 2));

                return new Point(iconCenter.X, iconCenter.Y);
            }
            else
            {
                return new Point(0, 0);
            }
        }

        private static AutomationElement GetTeamsTrayIcon()
        {
            // Note: name for automation elements can be found with 
            // Inspect.exe tool from Windows SDK
            // Sample path: C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\inspect.exe

            // Get desktop
            AutomationElement desktop = AutomationElement.RootElement;

            // Get tray icons
            AutomationElement trayIcons = desktop.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_TrayWnd"));

            if (trayIcons != null)
            {
                // Try to find the Teams icon in the tray with name "Microsoft Teams"
                AutomationElement teamsIcon = trayIcons.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Microsoft Teams"));

                if (teamsIcon == null)
                {
                    // Try to find the Teams icon in the tray with name "Microsoft Teams | Dell Technologies"
                    teamsIcon = trayIcons.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Microsoft Teams | Dell Technologies"));
                }
                if (teamsIcon == null)
                {
                    // Try to find the Teams icon in the tray with name "Microsoft Teams Microsoft Teams | Dell Technologies"
                    teamsIcon = trayIcons.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Microsoft Teams Microsoft Teams | Dell Technologies"));
                }

                return teamsIcon;
            }
            return null;
        }

        public static void SetTeamStatus(string status)
        {
            // Check if the "MS Teams" process is running
            Process[] processes = Process.GetProcessesByName("ms-teams");
            if (processes.Length == 0)
            {
                // "MS Teams" process is not running, so return
                return;
            }

            Thread.Sleep(500);

            Point teamIconPosition = GetTeamsTrayIconPosition();

            // Could not find msteams tray icon position
            if (teamIconPosition.X == 0)
                return;

            // Right-click in Teams tray icon
            SetCursorPos(teamIconPosition.X, teamIconPosition.Y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

            // Select My Status menu
            Thread.Sleep(1000);
            Point myStatusMenuPosition = GetMyStatusMenuPosition(teamIconPosition);
            SetCursorPos(myStatusMenuPosition.X, myStatusMenuPosition.Y);
            Thread.Sleep(1000);

            Point statusOptionPosition = GetStatusOptionPosition(status, teamIconPosition);
            SetCursorPos(statusOptionPosition.X, statusOptionPosition.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

            Thread.Sleep(1000);

            // Place cursor in the center of screen
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            int centerX = screenWidth / 2;
            int centerY = screenHeight / 2;
            SetCursorPos(centerX, centerY);
        }

        private static Point GetMyStatusMenuPosition(Point teamIconPosition)
        {
            // Calculate the position of the "My Status" menu based on the team icon position
            int myStatusMenuX = teamIconPosition.X - 205;
            int myStatusMenuY = teamIconPosition.Y - 110;
            return new Point(myStatusMenuX, myStatusMenuY);
        }

        private static Point GetStatusOptionPosition(string status, Point teamIconPosition)
        {
            int statusOptionX = teamIconPosition.X + 152;
            int statusOptionY;

            if (status.Equals("Away", StringComparison.OrdinalIgnoreCase))
            {
                // Set as status "Away"
                statusOptionY = teamIconPosition.Y - 175;
            }
            else if (status.Equals("AwayBRB", StringComparison.OrdinalIgnoreCase))
            {
                // Set as status "Away - I will be right back"
                statusOptionY = teamIconPosition.Y - 215;
            }
            else if (status.Equals("Busy", StringComparison.OrdinalIgnoreCase))
            {
                // Set as status "Busy"
                statusOptionY = teamIconPosition.Y - 277;
            }
            else
            {
                // Default status option position (e.g., "Available")
                statusOptionY = teamIconPosition.Y - 307;
            }

            return new Point(statusOptionX, statusOptionY);
        }

        #endregion

        #region Global Key Handlers

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_F12 = 12;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                int hotkeyId = m.WParam.ToInt32();

                if (hotkeyId == HOTKEY_ID_F12)
                {
                    ShowContextMenu();
                }
            }
        }

        private void ShowContextMenu()
        {
            // Close if required
            if (ContextMenu.Visible)
            {
                ContextMenu.Hide();
            }
            else
            {
                // Open if required
                Point mousePosition = Control.MousePosition;
                ContextMenu.Show(mousePosition);
            }
        }

        #endregion

        #region Load/Save settings

        private void LoadSettings()
        {
            _taskDuration = int.Parse(Properties.Settings.Default.TaskDuration);
            _breakDuration = int.Parse(Properties.Settings.Default.BreakDuration);
            _fullScreenBreak = Properties.Settings.Default.BreakFullScreen;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.TaskDuration = _taskDuration.ToString();
            Properties.Settings.Default.BreakDuration = _breakDuration.ToString();
            Properties.Settings.Default.BreakFullScreen = _fullScreenBreak;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Save/Load Metrics

        private void SaveMetrics()
        {
            MetricsReportManager.SaveMetricsReport(DateTime.Today, _totalTaskTime, _totalMeetingTime, _totalBreakTime, _totalLongBreakTime, _totalLunchTime, _totalBreaksCount);
        }

        private void LoadMetrics()
        {
            (_totalTaskTime, _totalMeetingTime, _totalBreakTime, _totalLongBreakTime, _totalLunchTime, _totalBreaksCount, _) = MetricsReportManager.LoadMetricsReport(DateTime.Today);
        }

        #endregion

        #region Closing/Exiting

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Unregister F12 hotkey global handler
            UnregisterHotKey(this.Handle, HOTKEY_ID_F12);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveMetrics();
            SaveSettings();

            Application.Exit();
        }

        #endregion

        #region Design

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.goToTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToMeetingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToBreakToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToLongBreakToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToLunchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.configureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.totalTasksTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalMeetingTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalBreaksTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalLongBreakTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalLunchTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.totalWorkTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalRestTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalBreaksCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._timer = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lblText = new System.Windows.Forms.Label();
            this.ContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // _notifyIcon
            // 
            this._notifyIcon.ContextMenuStrip = this.ContextMenu;
            this._notifyIcon.Text = "notifyIcon1";
            this._notifyIcon.Visible = true;
            this._notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // ContextMenu
            // 
            this.ContextMenu.ImageScalingSize = new System.Drawing.Size(40, 40);
            this.ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pauseToolStripMenuItem,
            this.toolStripMenuItemSeparator2,
            this.goToTaskToolStripMenuItem,
            this.goToMeetingToolStripMenuItem,
            this.goToBreakToolStripMenuItem,
            this.goToLongBreakToolStripMenuItem,
            this.goToLunchToolStripMenuItem,
            this.toolStripMenuItemSeparator3,
            this.configureToolStripMenuItem,
            this.toolStripMenuItemSeparator4,
            this.totalTasksTimeToolStripMenuItem,
            this.totalMeetingTimeToolStripMenuItem,
            this.totalBreaksTimeToolStripMenuItem,
            this.totalLongBreakTimeToolStripMenuItem,
            this.totalLunchTimeToolStripMenuItem,
            this.toolStripMenuItem1,
            this.totalWorkTimeToolStripMenuItem,
            this.totalRestTimeToolStripMenuItem,
            this.totalTimeToolStripMenuItem,
            this.totalBreaksCountToolStripMenuItem,
            this.toolStripMenuItemSeparator1,
            this.exitToolStripMenuItem});
            this.ContextMenu.Name = "ContextMenu";
            this.ContextMenu.Size = new System.Drawing.Size(514, 905);
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.pauseToolStripMenuItem.Text = "Pause timer (&0)";
            this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator2
            // 
            this.toolStripMenuItemSeparator2.Name = "toolStripMenuItemSeparator2";
            this.toolStripMenuItemSeparator2.Size = new System.Drawing.Size(510, 6);
            // 
            // goToTaskToolStripMenuItem
            // 
            this.goToTaskToolStripMenuItem.Name = "goToTaskToolStripMenuItem";
            this.goToTaskToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.goToTaskToolStripMenuItem.Text = "Start Task (&1)";
            this.goToTaskToolStripMenuItem.Click += new System.EventHandler(this.goToTaskToolStripMenuItem_Click);
            // 
            // goToMeetingToolStripMenuItem
            // 
            this.goToMeetingToolStripMenuItem.Name = "goToMeetingToolStripMenuItem";
            this.goToMeetingToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.goToMeetingToolStripMenuItem.Text = "Start Meeting (&2)";
            this.goToMeetingToolStripMenuItem.Click += new System.EventHandler(this.goToMeetingToolStripMenuItem_Click);
            // 
            // goToBreakToolStripMenuItem
            // 
            this.goToBreakToolStripMenuItem.Name = "goToBreakToolStripMenuItem";
            this.goToBreakToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.goToBreakToolStripMenuItem.Text = "Start Break (&3)";
            this.goToBreakToolStripMenuItem.Click += new System.EventHandler(this.goToBreakToolStripMenuItem_Click);
            // 
            // goToLongBreakToolStripMenuItem
            // 
            this.goToLongBreakToolStripMenuItem.Name = "goToLongBreakToolStripMenuItem";
            this.goToLongBreakToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.goToLongBreakToolStripMenuItem.Text = "Start Long Break (&4)";
            this.goToLongBreakToolStripMenuItem.Click += new System.EventHandler(this.goToLongBreakToolStripMenuItem_Click);
            // 
            // goToLunchToolStripMenuItem
            // 
            this.goToLunchToolStripMenuItem.Name = "goToLunchToolStripMenuItem";
            this.goToLunchToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.goToLunchToolStripMenuItem.Text = "Start Lunch (&5)";
            this.goToLunchToolStripMenuItem.Click += new System.EventHandler(this.goToLunchToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator3
            // 
            this.toolStripMenuItemSeparator3.Name = "toolStripMenuItemSeparator3";
            this.toolStripMenuItemSeparator3.Size = new System.Drawing.Size(510, 6);
            // 
            // configureToolStripMenuItem
            // 
            this.configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            this.configureToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.configureToolStripMenuItem.Text = "Configure";
            this.configureToolStripMenuItem.Click += new System.EventHandler(this.configureToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator4
            // 
            this.toolStripMenuItemSeparator4.Name = "toolStripMenuItemSeparator4";
            this.toolStripMenuItemSeparator4.Size = new System.Drawing.Size(510, 6);
            // 
            // totalTasksTimeToolStripMenuItem
            // 
            this.totalTasksTimeToolStripMenuItem.Name = "totalTasksTimeToolStripMenuItem";
            this.totalTasksTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalTasksTimeToolStripMenuItem.Text = "Total Tasks Time: 00:00:00";
            this.totalTasksTimeToolStripMenuItem.Click += new System.EventHandler(this.totalTasksTimeToolStripMenuItem_Click);
            // 
            // totalMeetingTimeToolStripMenuItem
            // 
            this.totalMeetingTimeToolStripMenuItem.Name = "totalMeetingTimeToolStripMenuItem";
            this.totalMeetingTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalMeetingTimeToolStripMenuItem.Text = "Total Meeting Time: 00:00:00";
            this.totalMeetingTimeToolStripMenuItem.Click += new System.EventHandler(this.totalMeetingTimeToolStripMenuItem_Click);
            // 
            // totalBreaksTimeToolStripMenuItem
            // 
            this.totalBreaksTimeToolStripMenuItem.Name = "totalBreaksTimeToolStripMenuItem";
            this.totalBreaksTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalBreaksTimeToolStripMenuItem.Text = "Total Breaks Time: 00:00:00";
            this.totalBreaksTimeToolStripMenuItem.Click += new System.EventHandler(this.totalBreaksTimeToolStripMenuItem_Click);
            // 
            // totalLongBreakTimeToolStripMenuItem
            // 
            this.totalLongBreakTimeToolStripMenuItem.Name = "totalLongBreakTimeToolStripMenuItem";
            this.totalLongBreakTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalLongBreakTimeToolStripMenuItem.Text = "Total Long Break Time: 00:00:00";
            this.totalLongBreakTimeToolStripMenuItem.Click += new System.EventHandler(this.totalLongBreakTimeToolStripMenuItem_Click);
            // 
            // totalLunchTimeToolStripMenuItem
            // 
            this.totalLunchTimeToolStripMenuItem.Name = "totalLunchTimeToolStripMenuItem";
            this.totalLunchTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalLunchTimeToolStripMenuItem.Text = "Total Lunch Time: 00:00:00";
            this.totalLunchTimeToolStripMenuItem.Click += new System.EventHandler(this.totalLunchTimeToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(510, 6);
            // 
            // totalWorkTimeToolStripMenuItem
            // 
            this.totalWorkTimeToolStripMenuItem.Name = "totalWorkTimeToolStripMenuItem";
            this.totalWorkTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalWorkTimeToolStripMenuItem.Text = "Total Work Time: 00:00:00";
            // 
            // totalRestTimeToolStripMenuItem
            // 
            this.totalRestTimeToolStripMenuItem.Name = "totalRestTimeToolStripMenuItem";
            this.totalRestTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalRestTimeToolStripMenuItem.Text = "Total Rest Time: 00:00:00";
            // 
            // totalTimeToolStripMenuItem
            // 
            this.totalTimeToolStripMenuItem.Name = "totalTimeToolStripMenuItem";
            this.totalTimeToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalTimeToolStripMenuItem.Text = "Total Time: 00:00:00";
            // 
            // totalBreaksCountToolStripMenuItem
            // 
            this.totalBreaksCountToolStripMenuItem.Name = "totalBreaksCountToolStripMenuItem";
            this.totalBreaksCountToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.totalBreaksCountToolStripMenuItem.Text = "Total Breaks: 0";
            // 
            // toolStripMenuItemSeparator1
            // 
            this.toolStripMenuItemSeparator1.Name = "toolStripMenuItemSeparator1";
            this.toolStripMenuItemSeparator1.Size = new System.Drawing.Size(510, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(513, 48);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolTip
            // 
            this.toolTip.ShowAlways = true;
            // 
            // lblText
            // 
            this.lblText.BackColor = System.Drawing.Color.Black;
            this.lblText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.1F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblText.ForeColor = System.Drawing.Color.LightGreen;
            this.lblText.Location = new System.Drawing.Point(99, 19);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(283, 53);
            this.lblText.TabIndex = 1;
            this.lblText.Text = "Loading...";
            // 
            // MainForm
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(476, 89);
            this.Controls.Add(this.lblText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}