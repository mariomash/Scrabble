using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Scrabble.Config {
    [Serializable]
    public class Logger {
        public List<LogEntry> LogEntries;

        public bool Active;
        public double MaximumLogEntriesInMemory;
        public LogType Threshold;

        public void Log(LogType logType, string message, StackTrace stack = null)
        {

            if (!Active) return;

            try
            {

                SaveToLogFile($"{logType} - {message}");

                if (logType < Threshold) return;

                var logEntriesQueSobran = Convert.ToInt32(LogEntries.Count - MaximumLogEntriesInMemory);
                if (logEntriesQueSobran > 0)
                {
                    LogEntries.RemoveRange(0, logEntriesQueSobran);
                }

                var logEntry = new LogEntry()
                {
                    Tipo = logType,
                    StackTrace = stack?.ToString(),
                    Message = message,
                    Timestamp = DateTime.Now
                };

                LogEntries.Add(logEntry);

            }
            catch (Exception ex)
            {
                SaveToLogFile($"{ex.Message} - {ex.StackTrace}");
            }
        }

        private void SaveToLogFile(string s)
        {

            try
            {
                var dataPath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}";

                var fileName = $"{dataPath}log.txt";

                s = $"{Environment.NewLine}{DateTime.Now}: {s}";

                var intentosEscrituraLog = 10;
                while (intentosEscrituraLog > 0)
                {
                    try
                    {
                        File.AppendAllText(fileName, s);
                        intentosEscrituraLog = 0;
                    }
                    catch
                    {
                        intentosEscrituraLog--;
                        Thread.Sleep(TimeSpan.FromMilliseconds(500));
                    }
                }

                if (!File.Exists(fileName)) return;

                var length = new FileInfo(fileName).Length;

                // 25.000 Kb
                if (length < 25000000) return;

                var oldFileName = $"{fileName}-{DateTime.Now.ToFileTimeUtc()}";

                try
                {
                    File.Move(fileName, oldFileName);
                }
                catch
                {
                    // Ignore
                }

            }
            catch
            {
                // Ignore
            }
        }
    }
}