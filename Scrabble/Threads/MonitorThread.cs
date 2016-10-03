using System;
using System.Diagnostics;
using System.Threading;
using Scrabble.Config;

namespace Scrabble.Threads {
    public class MonitorThread : BasicModule, IThread {

        public int DelayInMs, ForceExitEachHours;
        public string SnapshotFileName;

        public MonitorThread()
        {
            DelayInMs = 1000;
            ForceExitEachHours = 0;
            SnapshotFileName = "snapshot.xml";
        }

        public void WorkItem()
        {
            var i = 0;
            Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

            while (Active)
            {
                try
                {

                    if (Configuration.StopSignal)
                    {
                        Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
                        return;
                    }

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
                    Configuration.Logger.Log(LogType.Error, $"{ex.Message}", new StackTrace());
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
