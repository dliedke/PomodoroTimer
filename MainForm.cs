using System;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;


namespace PomodoroTimer
{
    public partial class MainForm : Form
    {
        #region Class Variables / Constructor

        private System.ComponentModel.IContainer components;

        private int _taskDuration;
        private int _breakDuration;
        private bool _fullScreenBreak;
        private int _remainingTime;
        private NotifyIcon _notifyIcon;
        private Timer _timer;

        private new ContextMenuStrip ContextMenu;

        private ToolStripMenuItem pauseToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator1;

        private ToolStripMenuItem goToBreakToolStripMenuItem;
        private ToolStripMenuItem goToTaskToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator3;

        private ToolStripMenuItem configureToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator4;

        private ToolStripMenuItem totalBreaksTimeToolStripMenuItem;
        private ToolStripMenuItem totalTasksTimeToolStripMenuItem;
        private ToolStripMenuItem totalTimeToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItemSeparator2;

        private ToolStripMenuItem exitToolStripMenuItem;

        private ToolTip toolTip;
        bool _hideToolTipTimer = false;
        private ToolTipForm _toolTipForm = new ToolTipForm();

        private bool _isTask;
        private int _totalBreaksTime;
        private Label lblText;
        private int _totalTasksTime;
        private System.Drawing.Size _originalToolTipFormSize;

        public MainForm()
        {
            InitializeComponent();

            _originalToolTipFormSize = _toolTipForm.Size;

            // If no location is set, put _toolTipForm in center of screen
            if (Properties.Settings.Default.ToolTipFormLocation == null ||
                (Properties.Settings.Default.ToolTipFormLocation.X == 0 &&
                 Properties.Settings.Default.ToolTipFormLocation.Y == 0))
            {
                _toolTipForm.StartPosition = FormStartPosition.Manual;
                _toolTipForm.Location = new System.Drawing.Point(
                    Screen.PrimaryScreen.Bounds.Width / 2 - _toolTipForm.Width / 2,
                    Screen.PrimaryScreen.Bounds.Height / 2 - _toolTipForm.Height / 2);
            }
            else
            {
                _toolTipForm.Location = Properties.Settings.Default.ToolTipFormLocation;
            }

            LoadSettings();
            InitializeTimer();

            // Help tooltip when initializing
            _notifyIcon.Text = "Left click to hide/show timer\r\nRight click to show menu";

            // Add app version to the exit menu item
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = $"Pomodoro Timer v{version.Major}.{version.Minor}";
            exitToolStripMenuItem.Text += $" ({versionString})";
        }

        #endregion

        #region Timer Control for Task/Break

        private void InitializeTimer()
        {
            _timer?.Stop();
            _timer = new Timer
            {
                Interval = 1000
            };
            _timer.Tick += Timer_Tick;
            _remainingTime = _taskDuration;
            _isTask = true;
            StartTimer();
            UpdateDisplay();
        }

        private void StartTimer()
        {
            _timer.Start();
            pauseToolStripMenuItem.Text = "Pause timer";
        }

        private void StopTimer()
        {
            _timer.Stop();
            pauseToolStripMenuItem.Text = "Continue timer";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Decrement the remaining time
            _remainingTime--;

            // Update the display
            UpdateDisplay();

            // Update the total task or break time
            if (_isTask)
            {
                _totalTasksTime += 1;
            }
            else
            {
                _totalBreaksTime += 1;
            }

            // Play a melody if the remaining time is 10 seconds
            if (_remainingTime == 10)
            {
                PlayMelody(!_isTask);
            }

            // Switch to the other time if the remaining time is 0
            if (_remainingTime == 0)
            {
                if (_isTask)
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

        private void SwitchToBreak()
        {
            _isTask = false;
            _remainingTime = _breakDuration;
            UpdateDisplay();
            ShowNotification("BREAK time started!");
        }

        private void SwitchToTask()
        {
            _isTask = true;
            _remainingTime = _taskDuration;
            UpdateDisplay();
            ShowNotification("TASK time started!");
        }

        private void UpdateDisplay()
        {
            int hours = _remainingTime / 3600;
            int minutes = (_remainingTime % 3600) / 60;
            int seconds = _remainingTime % 60;
            string time;

            // Show time in 00:00:00 format
            // With hours as optional
            if (hours > 0)
            {
                time = $"{hours:00}:{minutes:00}:{seconds:00}";

                // Increase _toolTipForm.Size by 12%
                _toolTipForm.Size = new System.Drawing.Size((int)(_originalToolTipFormSize.Width * 1.2), _originalToolTipFormSize.Height);
            }
            else
            {
                time = $"{minutes:00}:{seconds:00}";
                _toolTipForm.Size = _originalToolTipFormSize;
            }

            // Get task or break string
            string taskOrBreak = _isTask ? "Task" : "Break";

            if (_isTask)
            {
                // Set green ball icon for task
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.greenball.ico"));
            }
            else
            {
                // Set red ball icon for break
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.redball.ico"));
            }

            // Show break as full screen or regular break
            if (_isTask == false && Properties.Settings.Default.BreakFullScreen)
            {
                _toolTipForm.SetFullScreen(true);
            }
            else
            {
                _toolTipForm.SetFullScreen(false);
            }

            // Create text with task or break and time
            string notificationText = $"{taskOrBreak} - {time}";

            // Update tooltip text if required
            if (_hideToolTipTimer)
            {
                _notifyIcon.Text = notificationText;
            }

            // Show big tooltip if required
            ShowToolTip(notificationText);

            // Update times for break, task and total
            UpdateTotalTimes();
        }

        private void UpdateTotalTimes()
        {
            // Update Break/Task/Total times
            totalBreaksTimeToolStripMenuItem.Text = $"Total Break Time: {TimeSpan.FromSeconds(_totalBreaksTime):hh\\:mm\\:ss}";
            totalTasksTimeToolStripMenuItem.Text = $"Total Task Time: {TimeSpan.FromSeconds(_totalTasksTime):hh\\:mm\\:ss}";
            totalTimeToolStripMenuItem.Text = $"Total Time: {TimeSpan.FromSeconds(_totalBreaksTime + _totalTasksTime):hh\\:mm\\:ss}";
        }

        #endregion

        #region Hide/Show Timer and Notifications

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // If right click, show context menu
            if (e.Button == MouseButtons.Right)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(_notifyIcon, null);
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

        private void ShowNotification(string message)
        {
            // Show notification
            _notifyIcon.ShowBalloonTip(100, "", message, ToolTipIcon.None);
        }

        private void ShowToolTip(string message)
        {
            _toolTipForm.SetToolTip(message);

            if (!_hideToolTipTimer)
            {
                _toolTipForm.Show();
            }
            else
            {
                _toolTipForm.Hide();
            }

            this.Hide();
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
            if (_timer.Enabled)
            {
                StopTimer();
                pauseToolStripMenuItem.Text = "Continue timer";
            }
            else
            {
                StartTimer();
                pauseToolStripMenuItem.Text = "Pause timer";
            }
        }

        private void goToBreakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If already in break do not switch to break
            if (_isTask)
            {
                SwitchToBreak();
            }
        }

        private void goToTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If already in task do not switch to task
            if (!_isTask)
            {
                SwitchToTask();
            }
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

        #region Closing/Exiting

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Design

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.goToTaskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToBreakToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.configureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.totalBreaksTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalTasksTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
                                            this.goToBreakToolStripMenuItem,
                                            this.toolStripMenuItemSeparator3,
                                            this.configureToolStripMenuItem,
                                            this.toolStripMenuItemSeparator4,
                                            this.totalBreaksTimeToolStripMenuItem,
                                            this.totalTasksTimeToolStripMenuItem,
                                            this.totalTimeToolStripMenuItem,
                                            this.toolStripMenuItemSeparator1,
                                            this.exitToolStripMenuItem});
            this.ContextMenu.Name = "ContextMenu";
            this.ContextMenu.Size = new System.Drawing.Size(452, 412);
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.pauseToolStripMenuItem.Text = "Pause timer";
            this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator2
            // 
            this.toolStripMenuItemSeparator2.Name = "toolStripMenuItemSeparator2";
            this.toolStripMenuItemSeparator2.Size = new System.Drawing.Size(448, 6);
            // 
            // goToTaskToolStripMenuItem
            // 
            this.goToTaskToolStripMenuItem.Name = "goToTaskToolStripMenuItem";
            this.goToTaskToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.goToTaskToolStripMenuItem.Text = "Start Task";
            this.goToTaskToolStripMenuItem.Click += new System.EventHandler(this.goToTaskToolStripMenuItem_Click);
            // 
            // goToBreakToolStripMenuItem
            // 
            this.goToBreakToolStripMenuItem.Name = "goToBreakToolStripMenuItem";
            this.goToBreakToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.goToBreakToolStripMenuItem.Text = "Start Break";
            this.goToBreakToolStripMenuItem.Click += new System.EventHandler(this.goToBreakToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator3
            // 
            this.toolStripMenuItemSeparator3.Name = "toolStripMenuItemSeparator3";
            this.toolStripMenuItemSeparator3.Size = new System.Drawing.Size(448, 6);
            // 
            // configureToolStripMenuItem
            // 
            this.configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            this.configureToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.configureToolStripMenuItem.Text = "Configure";
            this.configureToolStripMenuItem.Click += new System.EventHandler(this.configureToolStripMenuItem_Click);
            // 
            // toolStripMenuItemSeparator4
            // 
            this.toolStripMenuItemSeparator4.Name = "toolStripMenuItemSeparator4";
            this.toolStripMenuItemSeparator4.Size = new System.Drawing.Size(448, 6);
            // 
            // totalBreaksTimeToolStripMenuItem
            // 
            this.totalBreaksTimeToolStripMenuItem.Name = "totalBreaksTimeToolStripMenuItem";
            this.totalBreaksTimeToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.totalBreaksTimeToolStripMenuItem.Text = "Total Breaks Time: 00:00:00";
            // 
            // totalTasksTimeToolStripMenuItem
            // 
            this.totalTasksTimeToolStripMenuItem.Name = "totalTasksTimeToolStripMenuItem";
            this.totalTasksTimeToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.totalTasksTimeToolStripMenuItem.Text = "Total Tasks Time: 00:00:00";
            // 
            // totalTimeToolStripMenuItem
            // 
            this.totalTimeToolStripMenuItem.Name = "totalTimeToolStripMenuItem";
            this.totalTimeToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.totalTimeToolStripMenuItem.Text = "Total Time: 00:00:00";
            // 
            // toolStripMenuItemSeparator1
            // 
            this.toolStripMenuItemSeparator1.Name = "toolStripMenuItemSeparator1";
            this.toolStripMenuItemSeparator1.Size = new System.Drawing.Size(448, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
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
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}