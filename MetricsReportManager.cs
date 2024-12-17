using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml;
using System.Globalization;

namespace PomodoroTimer
{
    public class MetricsReportManager
    {
        private static string AppDataFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PomodoroTimer");
        private static string ReportFilePath => Path.Combine(AppDataFolderPath, "pomodoro_timer_report.csv");
        private static string ReportExcelFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pomodoro_timer_report.xlsx");

        public static void SaveMetricsReport(DateTime date, int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLongBreakTime, int totalLunchTime, int totalBreaksCount)
        {
            int totalWorkTime = totalTasksTime + totalMeetingTime;
            int totalRestTime = totalBreaksTime + totalLongBreakTime + totalLunchTime;
            int totalTime = totalWorkTime + totalRestTime;

            // Use the culture's full day name to ensure consistency
            string dayName = date.ToString("dddd", CultureInfo.InvariantCulture);

            string reportLine = string.Join(",",
                dayName,
                date.ToString("yyyy-MM-dd"),
                FormatTime(totalTasksTime),
                FormatTime(totalMeetingTime),
                FormatTime(totalBreaksTime),
                FormatTime(totalLongBreakTime),
                FormatTime(totalLunchTime),
                totalBreaksCount.ToString(),
                FormatTime(totalWorkTime),
                FormatTime(totalRestTime),
                FormatTime(totalTime),
                totalTasksTime.ToString(),
                totalMeetingTime.ToString(),
                totalBreaksTime.ToString(),
                totalLongBreakTime.ToString(),
                totalLunchTime.ToString(),
                totalWorkTime.ToString(),
                totalRestTime.ToString(),
                totalTime.ToString()
            );

            try
            {
                // Create the app data folder if it doesn't exist
                Directory.CreateDirectory(AppDataFolderPath);

                // Add header row if the file doesn't exist
                if (!File.Exists(ReportFilePath))
                {
                    string headerRow = "Day of the Week,Date,Total Task Time,Total Meeting Time,Total Break Time,Total Long Break Time,Total Lunch Time,Total Breaks Count,Total Work Time,Total Rest Time,Total Time,Total Task Seconds,Total Meeting Seconds,Total Break Seconds,Total Long Break Seconds,Total Lunch Seconds,Total Work Seconds,Total Rest Seconds,Total Seconds";
                    File.WriteAllText(ReportFilePath, headerRow + Environment.NewLine);
                }

                // Read all existing lines
                string[] existingLines = File.Exists(ReportFilePath)
                    ? File.ReadAllLines(ReportFilePath)
                    : new string[] { };

                // Find if the date already exists (skip header row)
                bool dateExists = false;
                string dateToMatch = date.ToString("yyyy-MM-dd");

                for (int i = 1; i < existingLines.Length; i++)
                {
                    string[] fields = existingLines[i].Split(',');
                    if (fields.Length > 1 && fields[1] == dateToMatch)
                    {
                        existingLines[i] = reportLine;
                        dateExists = true;
                        break;
                    }
                }

                if (!dateExists)
                {
                    // If the date doesn't exist, append the new line
                    Array.Resize(ref existingLines, existingLines.Length + 1);
                    existingLines[existingLines.Length - 1] = reportLine;
                }

                // Write all lines back to the file
                File.WriteAllLines(ReportFilePath, existingLines);

                // Update Excel report
                SaveMetricsReportExcel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving metrics report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static (int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLongBreakTime, int totalLunchTime, int totalBreaksCount, int totalTime) LoadMetricsReport(DateTime date)
        {
            if (!File.Exists(ReportFilePath))
            {
                return (0, 0, 0, 0, 0, 0, 0);
            }

            try
            {
                string dateToFind = date.ToString("yyyy-MM-dd");
                string[] lines = File.ReadAllLines(ReportFilePath);

                // Skip header and find matching date
                var reportLine = lines.Skip(1)
                    .FirstOrDefault(line => line.Split(',')[1].Trim() == dateToFind);

                if (reportLine != null)
                {
                    string[] values = reportLine.Split(',');
                    if (values.Length >= 19)
                    {
                        return (
                            int.Parse(values[11]), // totalTasksTime
                            int.Parse(values[12]), // totalMeetingTime
                            int.Parse(values[13]), // totalBreaksTime
                            int.Parse(values[14]), // totalLongBreakTime
                            int.Parse(values[15]), // totalLunchTime
                            int.Parse(values[7]),  // totalBreaksCount
                            int.Parse(values[18])  // totalTime
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading metrics report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return (0, 0, 0, 0, 0, 0, 0);
        }

        private static string FormatTime(int totalSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        private static void SaveMetricsReportExcel()
        {
            if (!File.Exists(ReportFilePath))
            {
                return;
            }

            try
            {
                while (IsExcelFileOpen(ReportExcelFilePath))
                {
                    DialogResult result = MessageBox.Show(
                        $"Please close the Excel file '{ReportExcelFilePath}' to save the updated metrics.",
                        "File in Use",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(ReportExcelFilePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Metrics Report");
                    if (worksheet != null)
                    {
                        package.Workbook.Worksheets.Delete("Metrics Report");
                    }

                    worksheet = package.Workbook.Worksheets.Add("Metrics Report");

                    // Read all lines from CSV
                    string[] lines = File.ReadAllLines(ReportFilePath);

                    // Process header
                    string[] headers = lines[0].Split(',');
                    for (int i = 0; i < 11; i++) // Only first 11 columns for display
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                    }

                    // Process data rows
                    for (int row = 1; row < lines.Length; row++)
                    {
                        string[] values = lines[row].Split(',');
                        for (int col = 0; col < 11; col++) // Only first 11 columns for display
                        {
                            worksheet.Cells[row + 1, col + 1].Value = values[col];
                        }
                    }

                    // Apply table style
                    var tableRange = worksheet.Cells[1, 1, lines.Length, 11];
                    var table = worksheet.Tables.Add(tableRange, "MetricsReportTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium9;

                    worksheet.Cells.AutoFitColumns();

                    package.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving Excel report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsExcelFileOpen(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}