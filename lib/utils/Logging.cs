using System;
using Godot;

public enum LogLevel {
  Dev = 0,
  Debug = 1,
  Info = 2,
  Warning = 3,
  Error = 4,
  Critical = 5,
}

public static class Logger {
  public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Dev;
  public static bool ShowTimestamps { get; set; } = true;
  public static bool ShowCallerInfo { get; set; } = true;
  public static bool UseColors { get; set; } = true;

  private static string GetLevelColor(LogLevel level) {
    if (!UseColors) return "black";

    return level switch {
      LogLevel.Dev => "gray",
      LogLevel.Debug => "blue",
      LogLevel.Info => "green",
      LogLevel.Warning => "orange",
      LogLevel.Error => "red",
      LogLevel.Critical => "darkred",
      _ => "black"
    };
  }

  private static string GetMessageColor(LogLevel level) {
    if (!UseColors) return "black";
    
    return level switch {
      // grayscale, dev and debug are dim, info and warning are normal, error and critical are bright
      LogLevel.Dev => "gray",
      LogLevel.Debug => "gray",
      LogLevel.Info => "white",
      LogLevel.Warning => "white",
      LogLevel.Error => "red",
      LogLevel.Critical => "red",
      _ => "black"
    };
  }

  private static string GetLogPrefix(LogLevel level) {
    return level switch {
      LogLevel.Dev => "[DEV]",
      LogLevel.Debug => "[DEBUG]",
      LogLevel.Info => "[INFO]",
      LogLevel.Warning => "[WARNING]",
      LogLevel.Error => "[ERROR]",
      LogLevel.Critical => "[CRITICAL]",
      _ => "[UNKNOWN]"
    };
  }

  public static void Log(string message, LogLevel level) {
    if (level < CurrentLogLevel) return;

    var prefix = GetLogPrefix(level);
    var color = GetLevelColor(level);
    var alpha = GetMessageColor(level);

    var callerInfo = "";
    if (ShowCallerInfo) {
      var caller = new System.Diagnostics.StackTrace().GetFrame(2)?.GetMethod()!;
      callerInfo = $" <{caller.DeclaringType?.Name}.{caller.Name}>";
    }

    var timestampInfo = "";
    if (ShowTimestamps) {
      var timestamp = DateTime.Now.ToString("HH:mm:ss");
      timestampInfo = $" [{timestamp}]";
    }

    var fullMessage = $"[color={color}]{prefix}[/color][color={alpha}]{timestampInfo}{callerInfo}: {message}[/color]";

    GD.PrintRich(fullMessage);
  }

  public static void Dev(string message) => Log(message, LogLevel.Dev);
  public static void Debug(string message) => Log(message, LogLevel.Debug);
  public static void Info(string message) => Log(message, LogLevel.Info);
  public static void Warn(string message) => Log(message, LogLevel.Warning);
  public static void Error(string message) => Log(message, LogLevel.Error);
  public static void Critical(string message) => Log(message, LogLevel.Critical);
}