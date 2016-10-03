using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Scrabble.Threads;

namespace Scrabble.Config {
    [Serializable]
    //[XmlRoot("Configuration")]
    public sealed class Configuration {

        public static Configuration Instance { get; }

        static Configuration()
        {

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

            Instance.Logger.Log(LogType.Verbose, $"Loading Config");

            if (File.Exists($"{dataPath}{Instance.MonitorThread.SnapshotFileName}"))
            {
                try
                {
                    Instance = (Configuration)utils.DeSerializeObjectFromXml(
                        $"{dataPath}{Instance.MonitorThread.SnapshotFileName}",
                        typeof(Configuration));
                }
                catch (Exception ex)
                {
                    Instance.Logger.Log(LogType.Error, $"Cannot use the current Snapshot, there are some deserializing it -a {ex.Message}");
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

                using (var writer = new StreamWriter($"{snapshotTempUbication}"))
                {
                    serializer.Serialize(writer, Instance);
                }

                if (File.Exists(snapshotUbication)) File.Delete(snapshotUbication);
                File.Move(snapshotTempUbication, snapshotUbication);

            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null) message = $"{message} - {ex.InnerException.Message}";

                Instance.Logger.Log(LogType.Error, $"{message}", new StackTrace());
            }
        }

        //[XmlText]
        [XmlAttribute]
        public DateTime SerializedTimestamp {
            get { return DateTime.Now; }
            set { }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public DateTime LastStartup { get; set; }

        public bool StopSignal { get; set; }

        public MonitorThread MonitorThread { get; set; }

        public Logger Logger { get; set; }

    }
}