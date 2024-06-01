using System;
using System.Windows.Forms;

namespace PomodoroTimer
{
    public partial class ConfigurationForm : Form
    {
        private NumericUpDown numTaskDuration;
        private Label lblTaskDuration;
        private Label lblBreakDuration;
        private Button btnSave;
        private NumericUpDown numBreakDuration;

        public int TaskDuration { get; set; }
        public int BreakDuration { get; set; }

        public ConfigurationForm()
        {
            InitializeComponent();
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            // Clear user settings (for testing)
            //Properties.Settings.Default.Reset();
            //Properties.Settings.Default.Save();

            numTaskDuration.Value = TaskDuration / 60;
            numBreakDuration.Value = BreakDuration / 60;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            TaskDuration = (int)numTaskDuration.Value * 60;
            BreakDuration = (int)numBreakDuration.Value * 60;
            DialogResult = DialogResult.OK;
        }

        private void InitializeComponent()
        {
            this.numBreakDuration = new System.Windows.Forms.NumericUpDown();
            this.numTaskDuration = new System.Windows.Forms.NumericUpDown();
            this.lblTaskDuration = new System.Windows.Forms.Label();
            this.lblBreakDuration = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numBreakDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTaskDuration)).BeginInit();
            this.SuspendLayout();
            // 
            // numBreakDuration
            // 
            this.numBreakDuration.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.numBreakDuration.ForeColor = System.Drawing.Color.White;
            this.numBreakDuration.Location = new System.Drawing.Point(461, 163);
            this.numBreakDuration.Maximum = new decimal(new int[] {
            480,
            0,
            0,
            0});
            this.numBreakDuration.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numBreakDuration.Name = "numBreakDuration";
            this.numBreakDuration.Size = new System.Drawing.Size(120, 38);
            this.numBreakDuration.TabIndex = 0;
            this.numBreakDuration.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numTaskDuration
            // 
            this.numTaskDuration.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.numTaskDuration.ForeColor = System.Drawing.Color.White;
            this.numTaskDuration.Location = new System.Drawing.Point(461, 66);
            this.numTaskDuration.Maximum = new decimal(new int[] {
            480,
            0,
            0,
            0});
            this.numTaskDuration.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTaskDuration.Name = "numTaskDuration";
            this.numTaskDuration.Size = new System.Drawing.Size(120, 38);
            this.numTaskDuration.TabIndex = 1;
            this.numTaskDuration.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblTaskDuration
            // 
            this.lblTaskDuration.AutoSize = true;
            this.lblTaskDuration.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.900001F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTaskDuration.ForeColor = System.Drawing.Color.White;
            this.lblTaskDuration.Location = new System.Drawing.Point(107, 63);
            this.lblTaskDuration.Name = "lblTaskDuration";
            this.lblTaskDuration.Size = new System.Drawing.Size(315, 39);
            this.lblTaskDuration.TabIndex = 2;
            this.lblTaskDuration.Text = "Task Duration (min)";
            // 
            // lblBreakDuration
            // 
            this.lblBreakDuration.AutoSize = true;
            this.lblBreakDuration.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.900001F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBreakDuration.ForeColor = System.Drawing.Color.White;
            this.lblBreakDuration.Location = new System.Drawing.Point(92, 160);
            this.lblBreakDuration.Name = "lblBreakDuration";
            this.lblBreakDuration.Size = new System.Drawing.Size(330, 39);
            this.lblBreakDuration.TabIndex = 3;
            this.lblBreakDuration.Text = "Break Duration (min)";
            // 
            // btnSave
            // 
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.900001F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(241, 270);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(207, 83);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // ConfigurationForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(698, 414);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblBreakDuration);
            this.Controls.Add(this.lblTaskDuration);
            this.Controls.Add(this.numTaskDuration);
            this.Controls.Add(this.numBreakDuration);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuration";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ConfigurationForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numBreakDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTaskDuration)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}