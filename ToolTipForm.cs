using System;
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
            if (Properties.Settings.Default.ToolTipFormLocation != null)
            {
                this.Location = Properties.Settings.Default.ToolTipFormLocation;
            }
        }

        #endregion

        #region Set Tool Tip Text and Color

        public void SetToolTip(string text)
        {
            lblText.Text = text;

            // If text contains Task show text green, if text contains Break show text as red
            if (text.Contains("Task"))
            {
                lblText.ForeColor = Color.LightGreen;
            }
            else
            {
                lblText.ForeColor = Color.Red;
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