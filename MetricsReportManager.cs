﻿/* *******************************************************************************************************************
 * Application: PomodoroTimer
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2024
 * Usage and reproduction in any manner whatsoever without the written permission of Daniel Liedke is strictly forbidden.
 *  
 * Purpose: Save/load CSV with metrics per day
 *           
 * *******************************************************************************************************************/

using System;
using System.IO;
using System.Linq;

namespace PomodoroTimer
{
    public class MetricsReportManager
    {
        private static string ReportFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pomodoro_timer_report.csv");

        public static void SaveMetricsReport(DateTime date, int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLunchTime, int totalLongBreakTime)
        {
            int totalTime = totalTasksTime + totalMeetingTime + totalBreaksTime + totalLunchTime + totalLongBreakTime;

            string reportLine = $"{date.ToString("yyyy-MM-dd")},{totalTasksTime},{totalMeetingTime},{totalBreaksTime},{totalLunchTime},{totalLongBreakTime},{totalTime},{FormatTime(totalTasksTime)},{FormatTime(totalMeetingTime)},{FormatTime(totalBreaksTime)},{FormatTime(totalLunchTime)},{FormatTime(totalLongBreakTime)},{FormatTime(totalTime)}";


            // Add header row if the file doesn't exist
            if (!File.Exists(ReportFilePath))
            {
                string headerRow = "Date,Total Task Seconds,Total Meeting Seconds,Total Break Seconds,Total Lunch Seconds,Total Seconds,Total Task Time,Total Meeting Time,Total Break Time,Total Lunch Time,Total Time";
                File.WriteAllText(ReportFilePath, headerRow + Environment.NewLine);
            }

            // Check if the date already exists in the file
            bool dateExists = File.ReadLines(ReportFilePath).Skip(1).Any(line => line.StartsWith(date.ToString("yyyy-MM-dd")));

            if (dateExists)
            {
                // If the date exists, update the existing line
                string[] lines = File.ReadAllLines(ReportFilePath);
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(date.ToString("yyyy-MM-dd")))
                    {
                        lines[i] = reportLine;
                        break;
                    }
                }
                File.WriteAllLines(ReportFilePath, lines);
            }
            else
            {
                // If the date doesn't exist, append a new line
                File.AppendAllText(ReportFilePath, reportLine + Environment.NewLine);
            }
        }

        public static (int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLunchTime, int totalLongBreakTime, int totalTime) LoadMetricsReport(DateTime date)
        {
            if (File.Exists(ReportFilePath))
            {
                string reportLine = File.ReadLines(ReportFilePath)
                    .Skip(1) // Skip the header row
                    .FirstOrDefault(line => line.StartsWith(date.ToString("yyyy-MM-dd")));

                if (!string.IsNullOrEmpty(reportLine))
                {
                    string[] values = reportLine.Split(',');
                    if (values.Length == 13 &&
                        int.TryParse(values[1], out int totalTasksTime) &&
                        int.TryParse(values[2], out int totalMeetingTime) &&
                        int.TryParse(values[3], out int totalBreaksTime) &&
                        int.TryParse(values[4], out int totalLunchTime) &&
                        int.TryParse(values[5], out int totalLongBreakTime) &&
                        int.TryParse(values[6], out int totalTime))
                    {
                        return (totalTasksTime, totalMeetingTime, totalBreaksTime, totalLunchTime, totalLongBreakTime, totalTime);
                    }
                }
            }

            return (0, 0, 0, 0, 0, 0);
        }

        private static string FormatTime(int totalSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return timeSpan.ToString("hh\\:mm\\:ss");
        }
    }
}