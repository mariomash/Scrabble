﻿using System;
using System.Threading;
using Scrabble.Config;

namespace Scrabble.Threads
{
	public class MonitorThread : BasicModule, IThread
	{

		public int DelayInMs, ForceExitEachHours;
		public string SnapshotFileName;
		bool _stopSignal;

		public MonitorThread()
		{
			DelayInMs = 1000;
			ForceExitEachHours = 0;
			SnapshotFileName = "snapshot.xml";
			_stopSignal = false;
		}

		void Configuration_StopSignal(Configuration configuration)
		{
			_stopSignal = true;
		}

		public void WorkItem()
		{
			var i = 0;
			Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

			Configuration.StopSignal += Configuration_StopSignal;

			while (true)
			{
				if (_stopSignal)
				{
					Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
					return;
				}

				if (!Active)
					continue;

				try
				{

					Configuration.SaveSnapshot();

					if (ForceExitEachHours > 0)
					{
						if (DateTime.Now.Subtract(Configuration.Instance.LastStartup).TotalSeconds > ForceExitEachHours * 3600)
						{
							Environment.Exit(1);
						}
					}

				}
				catch (Exception ex)
				{
					Configuration.Logger.Log(LogType.Error, $"{ex}");
				}
				Thread.Sleep(TimeSpan.FromMilliseconds(DelayInMs));

				i++;
				if (i <= 10) continue;

				GC.Collect();
				GC.WaitForPendingFinalizers();
				i = 0;

			}
		}

	}
}
