using System;

namespace RemoteHostApp.Helpers;

/// <summary>
/// Helper ghi log ra RichTextBox và Console
/// </summary>
public static class LoggingHelper
{
    // Delegate để MainForm đăng ký nhận log
    public static Action<string, LogLevel>? OnLog;

    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var prefix = level switch
        {
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error => "[ERR ]",
            LogLevel.Debug => "[DBG ]",
            _ => "[LOG ]"
        };
        var formatted = $"{timestamp} {prefix} {message}";
        Console.WriteLine(formatted);
        OnLog?.Invoke(formatted, level);
    }

    public static void Info(string msg) => Log(msg, LogLevel.Info);
    public static void Warning(string msg) => Log(msg, LogLevel.Warning);
    public static void Error(string msg) => Log(msg, LogLevel.Error);
    public static void Debug(string msg) => Log(msg, LogLevel.Debug);
}

public enum LogLevel { Debug, Info, Warning, Error }