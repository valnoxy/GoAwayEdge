/*
 * Copyright (c) 2021 Exploitox Team
 *
 * Module Name:
 *      Logging.cs
 *
 * Description:
 *      This class is responsible for initializing the logging system.
 *
 * Author:
 *      Jonas Günner (valnoxy)      20-Sep-2021
 *
 * Notes:
 *      Backported from: https://git.heydu.net/valnoxy/xorieos/-/blob/main/srv03rtm/base/ntsetup/winnt32/modernsetup/common/logging.cs
 */

using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GoAwayEdge.Common.Debugging
{
    internal class Logging
    {
        private static string LogPath = Path.Combine(Path.GetTempPath(), "GoAwayEdge", "Logs");
        private static string? _logFile;

        internal enum LogLevel { INFO, ERROR, WARNING }

        public static void Initialize()
        {
            // Ensure the log directory exists
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            // Fetching the current log file
            _logFile = Path.Combine(LogPath, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
            if (!File.Exists(_logFile))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version!;
                Log($"GoAwayEdge {version.Major}.{version.Minor}.{version.Build} (Build {version.Revision})");
                Log("Logging system initialized");
            }

            // Delete old log files
            DeleteOldLogFiles();
        }

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            if (string.IsNullOrEmpty(_logFile))
                throw new Exception("Logging class not initialized!");

            // Get calling method information
            var stackTrace = new StackTrace();
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
            var callingClass = callingMethod?.DeclaringType?.FullName ?? "UnknownClass";
            var methodName = callingMethod?.Name ?? "UnknownMethod";

            // Construct the log message with calling class and method
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {level} - {callingClass}.{methodName} - {message}";

            using (var fileStream = new FileStream(_logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(logMessage);
            }

            // Output log message to Debug console
            System.Diagnostics.Debug.WriteLine(logMessage);
        }

        private static void DeleteOldLogFiles()
        {
            var logFiles = Directory.GetFiles(LogPath, "log_*.txt");
            foreach (var logFile in logFiles)
            {
                var creationTime = File.GetCreationTime(logFile);
                if (creationTime < DateTime.Now.AddDays(-7))
                {
                    File.Delete(logFile);
                    Log("Deleted old log file: " + logFile);
                }
            }
        }
    }
}