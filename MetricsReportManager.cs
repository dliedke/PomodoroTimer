using System;
using System.IO;
using System.Linq;

namespace PomodoroTimer
{
    public class MetricsReportManager
    {
        private static string ReportFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pomodoro_timer_report.csv");

        public static void SaveMetricsReport(DateTime date, int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLongBreakTime, int totalLunchTime, int totalBreaksCount)
        {
            int totalWorkTime = totalTasksTime + totalMeetingTime;
            int totalRestTime = totalBreaksTime + totalLongBreakTime + totalLunchTime;
            int totalTime = totalWorkTime + totalRestTime;

            string reportLine = $"{date.ToString("yyyy-MM-dd")},{totalTasksTime},{totalMeetingTime},{totalBreaksTime},{totalLongBreakTime},{totalLunchTime},{totalBreaksCount},{totalWorkTime},{totalRestTime},{totalTime},{FormatTime(totalTasksTime)},{FormatTime(totalMeetingTime)},{FormatTime(totalBreaksTime)},{FormatTime(totalLongBreakTime)},{FormatTime(totalLunchTime)},{FormatTime(totalWorkTime)},{FormatTime(totalRestTime)},{FormatTime(totalTime)}";

            // Add header row if the file doesn't exist
            if (!File.Exists(ReportFilePath))
            {
                string headerRow = "Date,Total Task Seconds,Total Meeting Seconds,Total Break Seconds,Total Long Break Seconds,Total Lunch Seconds,Total Breaks Count,Total Work Seconds,Total Rest Seconds,Total Seconds,Total Task Time,Total Meeting Time,Total Break Time,Total Long Break Time,Total Lunch Time,Total Work Time,Total Rest Time,Total Time";
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

        public static (int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLongBreakTime, int totalLunchTime, int totalBreaksCount, int totalTime) LoadMetricsReport(DateTime date)
        {
            if (File.Exists(ReportFilePath))
            {
                string reportLine = File.ReadLines(ReportFilePath)
                    .Skip(1) // Skip the header row
                    .FirstOrDefault(line => line.StartsWith(date.ToString("yyyy-MM-dd")));

                if (!string.IsNullOrEmpty(reportLine))
                {
                    string[] values = reportLine.Split(',');
                    if (values.Length == 18 &&
                        int.TryParse(values[1], out int totalTasksTime) &&
                        int.TryParse(values[2], out int totalMeetingTime) &&
                        int.TryParse(values[3], out int totalBreaksTime) &&
                        int.TryParse(values[4], out int totalLongBreakTime) &&
                        int.TryParse(values[5], out int totalLunchTime) &&
                        int.TryParse(values[6], out int totalBreaksCount) &&
                        int.TryParse(values[9], out int totalTime))
                    {
                        return (totalTasksTime, totalMeetingTime, totalBreaksTime, totalLongBreakTime, totalLunchTime, totalBreaksCount, totalTime);
                    }
                }
            }

            return (0, 0, 0, 0, 0, 0, 0);
        }

        private static string FormatTime(int totalSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return timeSpan.ToString("hh\\:mm\\:ss");
        }
    }
}