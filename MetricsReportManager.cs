using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml;

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

            string reportLine = $"{date.ToString("dddd")},{date.ToString("yyyy-MM-dd")},{FormatTime(totalTasksTime)},{FormatTime(totalMeetingTime)},{FormatTime(totalBreaksTime)},{FormatTime(totalLongBreakTime)},{FormatTime(totalLunchTime)},{totalBreaksCount},{FormatTime(totalWorkTime)},{FormatTime(totalRestTime)},{FormatTime(totalTime)},{totalTasksTime},{totalMeetingTime},{totalBreaksTime},{totalLongBreakTime},{totalLunchTime},{totalWorkTime},{totalRestTime},{totalTime}";

            // Create the app data folder if it doesn't exist
            Directory.CreateDirectory(AppDataFolderPath);

            // Add header row if the file doesn't exist
            if (!File.Exists(ReportFilePath))
            {
                string headerRow = "Day of the Week,Date,Total Task Time,Total Meeting Time,Total Break Time,Total Long Break Time,Total Lunch Time,Total Breaks Count,Total Work Time,Total Rest Time,Total Time,Total Task Seconds,Total Meeting Seconds,Total Break Seconds,Total Long Break Seconds,Total Lunch Seconds,Total Work Seconds,Total Rest Seconds,Total Seconds";
                File.WriteAllText(ReportFilePath, headerRow + Environment.NewLine);
            }

            // Check if the date already exists in the file
            bool dateExists = File.ReadLines(ReportFilePath).Skip(1).Any(line => line.Split(',')[1] == date.ToString("yyyy-MM-dd"));

            if (dateExists)
            {
                // If the date exists, update the existing line
                string[] lines = File.ReadAllLines(ReportFilePath);
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Split(',')[1] == date.ToString("yyyy-MM-dd"))
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

            SaveMetricsReportExcel();
        }

        public static (int totalTasksTime, int totalMeetingTime, int totalBreaksTime, int totalLongBreakTime, int totalLunchTime, int totalBreaksCount, int totalTime) LoadMetricsReport(DateTime date)
        {
            if (File.Exists(ReportFilePath))
            {
                string reportLine = File.ReadLines(ReportFilePath)
                    .Skip(1) // Skip the header row
                    .FirstOrDefault(line => line.Split(',')[1] == date.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrEmpty(reportLine))
                {
                    string[] values = reportLine.Split(',');
                    if (values.Length == 19 &&
                        int.TryParse(values[11], out int totalTasksTime) &&
                        int.TryParse(values[12], out int totalMeetingTime) &&
                        int.TryParse(values[13], out int totalBreaksTime) &&
                        int.TryParse(values[14], out int totalLongBreakTime) &&
                        int.TryParse(values[15], out int totalLunchTime) &&
                        int.TryParse(values[7], out int totalBreaksCount) &&
                        int.TryParse(values[18], out int totalTime))
                    {
                        return (totalTasksTime, totalMeetingTime, totalBreaksTime, totalLongBreakTime, totalLunchTime, totalBreaksCount, totalTime);
                    }
                }
            }

            return (0, 0, 0, 0, 0, 0, 0);
        }

        private static void SaveMetricsReportExcel()
        {
            if (File.Exists(ReportFilePath))
            {
                while (IsExcelFileOpen(ReportExcelFilePath))
                {
                    DialogResult result = MessageBox.Show($"Please close the Excel file '{ReportExcelFilePath}' to save the updated metrics.", "File in Use", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(ReportExcelFilePath)))
                {
                    // Check if the worksheet already exists
                    var worksheet = package.Workbook.Worksheets["Metrics Report"];
                    if (worksheet != null)
                    {
                        // Delete the existing worksheet
                        package.Workbook.Worksheets.Delete("Metrics Report");
                    }

                    // Add a new worksheet with the same name
                    worksheet = package.Workbook.Worksheets.Add("Metrics Report");

                    // Set header row
                    worksheet.Cells[1, 1].Value = "Day of the Week";
                    worksheet.Cells[1, 2].Value = "Date";
                    worksheet.Cells[1, 3].Value = "Total Task";
                    worksheet.Cells[1, 4].Value = "Total Meeting";
                    worksheet.Cells[1, 5].Value = "Total Break";
                    worksheet.Cells[1, 6].Value = "Total Long Break";
                    worksheet.Cells[1, 7].Value = "Total Lunch";
                    worksheet.Cells[1, 8].Value = "Total Breaks Count";
                    worksheet.Cells[1, 9].Value = "Total Work";
                    worksheet.Cells[1, 10].Value = "Total Rest";
                    worksheet.Cells[1, 11].Value = "Total Time";

                    // Fill data rows
                    int rowIndex = 2;
                    foreach (var line in File.ReadLines(ReportFilePath).Skip(1))
                    {
                        string[] values = line.Split(',');
                        if (values.Length == 19)
                        {
                            worksheet.Cells[rowIndex, 1].Value = values[0];
                            worksheet.Cells[rowIndex, 2].Value = values[1];
                            worksheet.Cells[rowIndex, 3].Value = values[2];
                            worksheet.Cells[rowIndex, 4].Value = values[3];
                            worksheet.Cells[rowIndex, 5].Value = values[4];
                            worksheet.Cells[rowIndex, 6].Value = values[5];
                            worksheet.Cells[rowIndex, 7].Value = values[6];
                            worksheet.Cells[rowIndex, 8].Value = values[7];
                            worksheet.Cells[rowIndex, 9].Value = values[8];
                            worksheet.Cells[rowIndex, 10].Value = values[9];
                            worksheet.Cells[rowIndex, 11].Value = values[10];

                            rowIndex++;
                        }
                    }

                    // Apply table style
                    var tableRange = worksheet.Cells[1, 1, rowIndex - 1, 11];
                    var table = worksheet.Tables.Add(tableRange, "MetricsReportTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium9; // Blue table style

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    // Adjust row height based on cell contents
                    for (int row = 1; row <= rowIndex - 1; row++)
                    {
                        worksheet.Row(row).CustomHeight = true;
                        worksheet.Row(row).Style.WrapText = true;
                    }

                    package.Save();
                }
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
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        private static string FormatTime(int totalSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return timeSpan.ToString("hh\\:mm\\:ss");
        }
    }
}