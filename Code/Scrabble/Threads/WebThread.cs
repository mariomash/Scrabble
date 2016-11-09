using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Nancy.Hosting.Self;
using Scrabble.Config;

namespace Scrabble.Threads
{
    public class WebThread : BasicModule, IThread {
        public string ServerUri;
        private bool _stopSignal;

        [XmlIgnore]
        [IgnoreDataMember]
        public NancyHost Host;

        public WebThread()
        {
            ServerUri = @"http://localhost:8888";
            _stopSignal = false;
        }

        private void Configuration_StopSignal(Configuration configuration)
        {
            _stopSignal = true;

            Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
            Host.Stop();
            Host.Dispose();
        }

        public void WorkItem()
        {

            Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

            Configuration.StopSignal += Configuration_StopSignal;

            Host = new NancyHost(new Uri(ServerUri));
            Host.Start();

        }
    }
}