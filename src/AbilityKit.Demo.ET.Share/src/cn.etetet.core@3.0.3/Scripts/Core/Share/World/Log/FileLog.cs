using System;
using System.IO;

namespace ET
{
    /// <summary>
    /// 文件日志实现，支持同时输出到控制台
    /// </summary>
    public class FileLog : ILog
    {
        private readonly string _logFile;
        private readonly object _lock = new();
        private bool _enableConsole = true;

        public FileLog(string logDirectory = "Logs", bool enableConsole = true)
        {
            _enableConsole = enableConsole;
            Directory.CreateDirectory(logDirectory);
            _logFile = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        public string LogFilePath => _logFile;

        private void Write(string level, string message, ConsoleColor? consoleColor = null)
        {
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            lock (_lock)
            {
                File.AppendAllText(_logFile, logLine + Environment.NewLine);

                if (_enableConsole)
                {
                    if (consoleColor.HasValue)
                    {
                        Console.ForegroundColor = consoleColor.Value;
                    }
                    Console.WriteLine(logLine);
                    Console.ResetColor();
                }
            }
        }

        public void Trace(string message) => Write("TRACE", message, ConsoleColor.Gray);
        public void Warning(string message) => Write("WARN", message, ConsoleColor.Yellow);
        public void Info(string message) => Write("INFO", message, ConsoleColor.Cyan);

        public void Debug(string message)
        {
#if DEBUG
            Write("DEBUG", message, ConsoleColor.White);
#endif
        }

        public void Error(string message) => Write("ERROR", message, ConsoleColor.Red);
        public void Error(Exception e) => Write("ERROR", $"{e.Message}\n{e.StackTrace}", ConsoleColor.Red);

#if DOTNET
        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            Trace(message.ToStringAndClear());
        }

        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            Warning(message.ToStringAndClear());
        }

        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            Info(message.ToStringAndClear());
        }

        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
#if DEBUG
            Debug(message.ToStringAndClear());
#endif
        }

        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            Error(message.ToStringAndClear());
        }
#endif
    }
}
