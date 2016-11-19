using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Scrabble.Threads;
using System.Threading;

namespace Scrabble.Config {
    [Serializable]
    //[XmlRoot("Configuration")]
    public sealed class Configuration {

        public static Configuration Instance { get; }

        static Configuration()
        {
            //    Instance.Logger = new Logger();
            //    Instance.MonitorThread = new MonitorThread();
            //    Instance.MpcThread = new MpcThread();
            //    Instance.NfcThread = new NfcThread();
            //    Instance.WebThread = new WebThread();


            var utils = new Utils();

            var dataPath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}";

#if DEBUG
            const string fileConfigName = @"configuration.xml";
#else
            const string fileConfigName = @"configuration.xml";
#endif

            Instance = (Configuration)utils.DeSerializeObjectFromXml(
                $"{dataPath}{fileConfigName}",
                typeof(Configuration));

            Instance.Logger.Log(LogType.Verbose, @"Loading Config");

            if (File.Exists($"{dataPath}{Instance.MonitorThread.SnapshotFileName}"))
            {
                try
                {
                    Instance = (Configuration)utils.DeSerializeObjectFromXml(
                        $"{dataPath}{Instance.MonitorThread.SnapshotFileName}",
                        typeof(Configuration));

                    Instance.Logger.LogEntries = new ConcurrentDictionary<Guid, LogEntry>();
                    foreach (var logEntry in Instance.Logger.SerializedLogEntries)
                    {
                        var isAdded = false;
                        while (!isAdded)
                        {
                            var guid = Guid.NewGuid();

                            isAdded = Instance.Logger.LogEntries.TryAdd(guid, logEntry) || Instance.Logger.LogEntries.ContainsKey(guid);
                            Thread.Sleep(TimeSpan.FromMilliseconds(50));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Instance.Logger.Log(LogType.Error, $"Cannot use the current Snapshot, there was some error deserializing it: {ex.Message}");
                    File.Move($"{dataPath}{Instance.MonitorThread.SnapshotFileName}", $"{dataPath}{Instance.MonitorThread.SnapshotFileName}-{DateTime.Now.ToFileTimeUtc()}-bad");
                }
            }

            Instance.LastStartup = DateTime.Now;


        }

        public static void SaveSnapshot()
        {
            try
            {

                var dataPath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}";

                var serializer = new XmlSerializer(typeof(Configuration));
                var snapshotUbication = $"{dataPath}{Instance.MonitorThread.SnapshotFileName}";
                var snapshotTempUbication = $"{dataPath}{Instance.MonitorThread.SnapshotFileName}_tmp";

                Instance.Logger.SerializedLogEntries = new List<LogEntry>();
                foreach (var logEntry in Instance.Logger.LogEntries)
                    Instance.Logger.SerializedLogEntries.Add(logEntry.Value);

                using (var writer = new StreamWriter($"{snapshotTempUbication}"))
                    serializer.Serialize(writer, Instance);

                if (File.Exists(snapshotUbication)) File.Delete(snapshotUbication);
                File.Move(snapshotTempUbication, snapshotUbication);

            }
            catch (Exception ex)
            {
                Instance.Logger.Log(LogType.Error, $"{ex}");
            }
        }

        public static void SendStopSignal()
        {
            Instance.StopSignal(Instance);
        }

        //[XmlText]
        [XmlAttribute]
        public DateTime SerializedTimestamp {
            get { return DateTime.Now; }
            set { }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public DateTime LastStartup { get; private set; }

        public MonitorThread MonitorThread { get; set; }

        public MpcThread MpcThread { get; set; }

        public WebThread WebThread { get; set; }

        public NfcThread NfcThread { get; set; }

        public Logger Logger { get; set; }

        public event StopSignalHandler StopSignal;

        public delegate void StopSignalHandler(Configuration configuration);

    }
}