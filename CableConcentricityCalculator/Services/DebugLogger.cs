using System;
using System.IO;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Simple debug logger that writes to DEBUG\log.txt
/// </summary>
public static class DebugLogger
{
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DEBUG");
    private static readonly string LogFile = Path.Combine(LogDirectory, "log.txt");
    private static readonly object LockObject = new object();

    public static bool Enabled { get; set; } = true;

    static DebugLogger()
    {
        // Create DEBUG directory if it doesn't exist
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }

        // Clear log file on startup
        try
        {
            File.WriteAllText(LogFile, $"=== Debug Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
        }
        catch
        {
            // Ignore errors during initialization
        }
    }

    public static void Log(string message)
    {
        if (!Enabled) return;

        try
        {
            lock (LockObject)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}\n";

                File.AppendAllText(LogFile, logMessage);

                // Also write to console for backwards compatibility
                Console.WriteLine(message);
            }
        }
        catch
        {
            // Silently ignore logging errors
        }
    }

    public static void Clear()
    {
        try
        {
            lock (LockObject)
            {
                File.WriteAllText(LogFile, $"=== Debug Log Cleared: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
