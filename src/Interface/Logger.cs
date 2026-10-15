using System;
using System.IO;
using System.Linq;
namespace Chess;

public static class Logger
{
    // Logs now go to solution root /logs.
    private static readonly string logDir;
    private static readonly int maxLogFiles = 5;
    private static string logPath = string.Empty;
    private static bool initialized = false;
    private static readonly object initLock = new();

    static Logger()
    {
        logDir = ResolveLogDirectory();
        Initialize();
    }

    private static string ResolveLogDirectory()
    {
        try
        {
            // Allow environment override
            var env = Environment.GetEnvironmentVariable("CHESS_ENGINE_LOG_DIR");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Path.GetFullPath(env);
            }

            DirectoryInfo? current = new(AppContext.BaseDirectory);
            // Walk up at most 8 levels looking for a solution or a csproj file
            for (int i = 0; i < 8 && current != null; i++)
            {
                bool hasSolution = current.GetFiles("*.sln").Any();
                bool hasProject = current.GetFiles("*.csproj").Any();
                if (hasSolution || hasProject)
                {
                    return Path.Combine(current.FullName, "logs");
                }
                current = current.Parent;
            }
            // Fallback to base directory/logs
            return Path.Combine(AppContext.BaseDirectory, "logs");
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, "logs");
        }
    }

    public static string LogFilePath => logPath;

    public static void Initialize()
    {
        if (initialized) return;
        lock (initLock)
        {
            if (initialized) return;
            Directory.CreateDirectory(logDir);

            // Create new file each run with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logPath = Path.Combine(logDir, $"log_{timestamp}.txt");

            // Rotate old logs
            var logFiles = Directory.GetFiles(logDir, "log_*.txt")
                .OrderBy(f => File.GetCreationTime(f))
                .ToList();
            while (logFiles.Count >= maxLogFiles)
            {
                File.Delete(logFiles[0]);
                logFiles.RemoveAt(0);
            }
            initialized = true;
        }
    }

    public static void Log(string level, string message)
    {
        if (!initialized) Initialize();
        try
        {
            using (var writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }
        catch (Exception)
        {
            // Swallow logging exceptions to avoid breaking engine/tests.
        }
    }

    public static void Info(string message) => Log("INFO", message);
    public static void Debug(string message) => Log("DEBUG", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);
}