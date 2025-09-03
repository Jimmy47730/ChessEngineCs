using System;
using System.IO;
using System.Linq;
namespace Chess;

public static class Logger
{
    private static readonly string logDir = "logs";
    private static string logPath;
    private static readonly int maxLogFiles = 5;

    static Logger()
    {
        // Ensure log directory exists
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        // Create log file with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logPath = Path.Combine(logDir, $"log_{timestamp}.txt");

        // Delete oldest files if more than maxLogFiles
        var logFiles = Directory.GetFiles(logDir, "log_*.txt")
            .OrderBy(f => File.GetCreationTime(f))
            .ToList();
        while (logFiles.Count >= maxLogFiles)
        {
            File.Delete(logFiles[0]);
            logFiles.RemoveAt(0);
        }
    }

    public static void Log(string level, string message)
    {
        using (var writer = new StreamWriter(logPath, true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
        }
    }

    public static void Info(string message) => Log("INFO", message);
    public static void Debug(string message) => Log("DEBUG", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);
}