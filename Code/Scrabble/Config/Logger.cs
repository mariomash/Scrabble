using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;

namespace Scrabble.Config
{
	[Serializable]
	public class Logger
	{

		[XmlIgnore]
		[IgnoreDataMember]
		public ConcurrentDictionary<Guid, LogEntry> LogEntries;

		public List<LogEntry> SerializedLogEntries;

		public bool Active;
		public double MaximumLogEntriesInMemory;
		public LogType Threshold;

		public Logger()
		{
			Active = false;
			MaximumLogEntriesInMemory = 1000;
			Threshold = LogType.Info;
			LogEntries = new ConcurrentDictionary<Guid, LogEntry>();
			SerializedLogEntries = new List<LogEntry>();
		}

		public void Log(LogType logType, string message)
		{

			if (!Active) return;

			try
			{
				if (logType == LogType.Verbose)
					Console.WriteLine($"{message}");
				else
					Console.WriteLine($"{logType} - {message}");

				SaveToLogFile($"{logType} - {message}");

				if (logType < Threshold) return;

				var logEntry = new LogEntry
				{
					Tipo = logType,
					Message = message,
					Timestamp = DateTime.Now
				};

				AddToLogEntries(logEntry);

			}
			catch (Exception ex)
			{
				SaveToLogFile($"{LogType.Error} - {ex}");
			}
		}

		private void AddToLogEntries(LogEntry logEntry)
		{
			try
			{

				var logEntriesQueSobran = Convert.ToInt32(LogEntries.Count - MaximumLogEntriesInMemory);
				if (logEntriesQueSobran > 0)
				{
					var logEntriesToRemove = LogEntries.OrderBy(l => l.Value.Timestamp).Take(logEntriesQueSobran);
					foreach (var logEntryToRemove in logEntriesToRemove)
					{
						var isRemoved = false;
						var guidToRemove = logEntryToRemove.Key;
						while (!isRemoved)
						{
							LogEntry removedEntry;
							isRemoved = LogEntries.TryRemove(guidToRemove, out removedEntry) || !LogEntries.ContainsKey(guidToRemove);
							Thread.Sleep(TimeSpan.FromMilliseconds(50));
						}
					}
				}

				var isAdded = false;
				while (!isAdded)
				{
					var guid = Guid.NewGuid();
					isAdded = LogEntries.TryAdd(guid, logEntry) || LogEntries.ContainsKey(guid);
					Thread.Sleep(TimeSpan.FromMilliseconds(50));
				}
			}
			catch
			{
				// Ignore
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