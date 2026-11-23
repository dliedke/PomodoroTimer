/* *******************************************************************************************************************
 * Application: PomodoroTimer
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2025
 * Usage and reproduction in any manner whatsoever without the written permission of Daniel Liedke is strictly forbidden.
 *  
 * Purpose: Program entry point
 *           
 * *******************************************************************************************************************/

using System;
using System.Windows.Forms;

namespace DEMAgentProcess
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // user.config is stored in a folder per assembly version, so every version bump
            // starts with default settings (timer shown, default durations, timer at 0,0).
            // Copy the previous version values once. This must run before MainForm is
            // created because its ToolTipForm field reads the saved location in its constructor.
            if (Properties.Settings.Default.SettingsUpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            Application.Run(new MainForm());
        }
    }
}
