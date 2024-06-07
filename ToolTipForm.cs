using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace PomodoroTimer
{
    public partial class ToolTipForm : Form
    {
        #region Class Variables / Constructor

        private bool isDragging = false;
        private Point lastMousePosition;

        public ToolTipForm()
        {
            InitializeComponent();
            lblText.MouseDown += lblText_MouseDown;
            lblText.MouseMove += lblText_MouseMove;
            lblText.MouseUp += lblText_MouseUp;
            this.MouseDown += ToolTipForm_MouseDown;
            this.MouseMove += ToolTipForm_MouseMove;
            this.MouseUp += ToolTipForm_MouseUp;

            // Load last location
            if (Properties.Settings.Default.ToolTipFormLocation != null &&
                Properties.Settings.Default.ToolTipFormLocation.X > 0 &&
                Properties.Settings.Default.ToolTipFormLocation.Y > 0)
            {
                this.Location = Properties.Settings.Default.ToolTipFormLocation;
            }
        }

        #endregion

        #region Set Tool Tip Text and Color

        public void SetToolTip(string text)
        {
            lblText.Text = text;

            // If text contains break show text as red otherwise green
            if (text.Contains("Break"))
            {
                lblText.ForeColor = Color.Red;
            }
            else
            {
                lblText.ForeColor = Color.LightGreen;
            }
        }

        public void SetFullScreen(bool isFullScreen)
        {
            if (isFullScreen)
            {
                // Allow the taskbar to be displayed
                this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                this.WindowState = FormWindowState.Maximized;
                this.Cursor = Cursors.Default;

                // Increase lblText font size to 130
                lblText.Font = new Font(lblText.Font.Name, 130, lblText.Font.Style, lblText.Font.Unit);

                // Put label in middle of the screen considering DPI and label size
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                int labelWidth = lblText.Width;
                int labelHeight = lblText.Height;
                int labelX = (screenWidth - labelWidth) / 2;
                int labelY = (screenHeight - labelHeight) / 2;
                lblText.Location = new Point(labelX, labelY);

                // Add an exit button in the bottom right corner
                Button exitButton = new Button();
                exitButton.Text = "Exit";
                exitButton.Font = new Font(exitButton.Font.Name, 20, exitButton.Font.Style, exitButton.Font.Unit);
                exitButton.Size = new Size(200, 100);
                exitButton.Location = new Point(screenWidth - exitButton.Width - 20, screenHeight - exitButton.Height - 20);
                exitButton.ForeColor = Color.White;
                exitButton.Click += (sender, e) =>
                {
                    // Remove the exit button
                    Button exitButton2 = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Exit");
                    if (exitButton2 != null)
                    {
                        exitButton2.Visible = false;
                    }
                    Properties.Settings.Default.BreakFullScreen = false;
                    SetFullScreen(false);

                    MainForm mainForm = (MainForm)Application.OpenForms["MainForm"];
                    mainForm.Hide();
                    mainForm.Show();

                    if (mainForm._currentStatus == TimerStatus.Launch)
                    {
                        mainForm.SwitchToTask();
                    }
                };
                this.Controls.Add(exitButton);
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

                // Remove the exit button
                Button exitButton2 = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Exit");
                if (exitButton2 != null)
                {
                    this.Controls.Remove(exitButton2);
                    exitButton2.Dispose();
                }
            }
        }

        #endregion

        #region Form/Label Move with Dragging

        private void lblText_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        }

        private void lblText_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                this.Location = new Point(
                    this.Location.X + e.X - lastMousePosition.X,
                    this.Location.Y + e.Y - lastMousePosition.Y);
            }
        }

        private void lblText_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void ToolTipForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePosition = e.Location;
            }
        }

        private void ToolTipForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                this.Location = new Point(
                    this.Location.X + e.X - lastMousePosition.X,
                    this.Location.Y + e.Y - lastMousePosition.Y);
            }
        }

        private void ToolTipForm_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        #endregion

        #region Form Closing

        private void ToolTipForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.ToolTipFormLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        #endregion
    }
}