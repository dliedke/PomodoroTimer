using System;
using System.Reflection;
using System.Windows.Forms;

namespace PomodoroTimer
{
    public partial class MainForm : Form
    {
        #region Class Variables / Constructor

        private System.ComponentModel.IContainer components;

        private int _taskDuration;
        private int _breakDuration;
        private int _remainingTime;
        private NotifyIcon _notifyIcon;
        private Timer _timer;

        private new ContextMenuStrip ContextMenu;

        private ToolStripMenuItem startToolStripMenuItem;
        private ToolStripMenuItem stopToolStripMenuItem;
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

        private bool isTask;

        private int _totalBreaksTime;
        private Label lblText;
        private int _totalTasksTime;
     
        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            InitializeTimer();
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
            isTask = true;
            StartTimer();
            UpdateDisplay();
        }

        private void StartTimer()
        {
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _remainingTime--;
            UpdateDisplay();

            if (isTask)
            {
                _totalTasksTime += 1;
            }
            else
            {
                _totalBreaksTime += 1;
            }

            if (_remainingTime == 0)
            {
                if (isTask)
                {
                    SwitchToBreak();
                }
                else
                {
                    SwitchToTask();
                }

                UpdateTotalTimes();
            }
        }

        private void SwitchToBreak()
        {
            isTask = false;
            _remainingTime = _breakDuration;
            UpdateDisplay();
            ShowNotification("BREAK time started!");
            PlayMelody(isTask);
        }

        private void SwitchToTask()
        {
            isTask = true;
            _remainingTime = _taskDuration;
            UpdateDisplay();
            ShowNotification("TASK time started!");
            PlayMelody(isTask);
        }

        private void UpdateDisplay()
        {
            int minutes = _remainingTime / 60;
            int seconds = _remainingTime % 60;
            string time = $"{minutes:00}:{seconds:00}";

            if (isTask)
            {
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.greenball.ico"));
                _notifyIcon.Text = $"Task - {time}";
            }
            else
            {
                _notifyIcon.Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("PomodoroTimer.Resources.redball.ico"));
                _notifyIcon.Text = $"Break - {time}";
            }

            ShowToolTip();
            UpdateTotalTimes();
        }

        private void UpdateTotalTimes()
        {
            totalBreaksTimeToolStripMenuItem.Text = $"Total Breaks Time: {TimeSpan.FromSeconds(_totalBreaksTime):hh\\:mm\\:ss}";
            totalTasksTimeToolStripMenuItem.Text = $"Total Tasks Time: {TimeSpan.FromSeconds(_totalTasksTime):hh\\:mm\\:ss}";
            totalTimeToolStripMenuItem.Text = $"Total Time: {TimeSpan.FromSeconds(_totalBreaksTime + _totalTasksTime):hh\\:mm\\:ss}";
        }

        #endregion

        #region Hide/Show Timer and Notifications

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(_notifyIcon, null);
            }

            if (e.Button == MouseButtons.Left)
            {
                _hideToolTipTimer = !_hideToolTipTimer;
            }
        }

        private void ShowNotification(string message)
        {
            _notifyIcon.ShowBalloonTip(1000, "", message, ToolTipIcon.None);
        }

        private void ShowToolTip()
        {
            _toolTipForm.SetToolTip(_notifyIcon.Text);

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
            int tempo = 100;

            int noteC = 662;
            int noteD = 694;
            int noteE = 830;

            if (isTask)
            {
                noteC = 830;
                noteD = 694;
                noteE = 662;
            }

            Console.Beep(noteC, tempo);
            System.Threading.Thread.Sleep(tempo / 2);
            Console.Beep(noteD, tempo);
            System.Threading.Thread.Sleep(tempo / 2);
            Console.Beep(noteE, tempo);
        }

        #endregion

        #region Context Menus

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartTimer();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopTimer();
        }

        private void goToBreakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If already in break do not switch to break
            if (isTask)
            {
                SwitchToBreak();
            }
        }

        private void goToTaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If already in task do not switch to task
            if (!isTask)
            {
                SwitchToTask();
            }
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigurationForm configForm = new ConfigurationForm();
            configForm.TaskDuration = _taskDuration;
            configForm.BreakDuration = _breakDuration;

            if (configForm.ShowDialog() == DialogResult.OK)
            {
                _taskDuration = configForm.TaskDuration;
                _breakDuration = configForm.BreakDuration;
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
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.TaskDuration = _taskDuration.ToString();
            Properties.Settings.Default.BreakDuration = _breakDuration.ToString();
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
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.startToolStripMenuItem,
            this.stopToolStripMenuItem,
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
            this.ContextMenu.Size = new System.Drawing.Size(452, 460);
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.startToolStripMenuItem.Text = "Start";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.startToolStripMenuItem_Click);
            // 
            // stopToolStripMenuItem
            // 
            this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            this.stopToolStripMenuItem.Size = new System.Drawing.Size(451, 48);
            this.stopToolStripMenuItem.Text = "Stop";
            this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
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