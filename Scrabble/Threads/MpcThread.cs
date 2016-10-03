using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;
using Nancy.Hosting.Self;
using Scrabble.Config;
using Scrabble.LibMpc;

namespace Scrabble.Threads {
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
        }

        public void WorkItem()
        {

            Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

            Configuration.StopSignal += Configuration_StopSignal;

            Host = new NancyHost(new Uri(ServerUri));
            Host.Start();

        }
    }

    public class MpcThread : BasicModule, IThread {

        public int ServerPort, DelayInMs;
        public string ServerAdress;
        private bool _stopSignal;

        [XmlIgnore]
        [IgnoreDataMember]
        public Mpc Mpc;

        public MpcThread()
        {
            ServerAdress = @"127.0.0.1";
            ServerPort = 6600;
            _stopSignal = false;

            Mpc = new Mpc
            {
                Connection = new MpcConnection
                {
                    Server = new IPEndPoint(
                        IPAddress.Parse(ServerAdress),
                        ServerPort),
                    AutoConnect = false
                }
            };

            Mpc.OnConnected += Mpc_OnConnected;

            Mpc.OnDisconnected += Mpc_OnDisconnected;

        }

        private void Configuration_StopSignal(Configuration configuration)
        {
            _stopSignal = true;
            Mpc.Connection.Disconnect();
        }

        public void WorkItem()
        {
            Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

            Configuration.StopSignal += Configuration_StopSignal;

            while (Active)
            {
                try
                {

                    if (_stopSignal)
                    {
                        Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
                        return;
                    }

                }
                catch (Exception ex)
                {
                    Configuration.Logger.Log(LogType.Error, $"{ex.Message}", new StackTrace());
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(DelayInMs));
                //mpc.Connection.Connect();
            }

        }

        private void Mpc_OnDisconnected(Mpc mpc)
        {
            if (!_stopSignal)
                mpc.Connection.Connect();
        }

        private void Mpc_OnConnected(Mpc mpc)
        {
            //https://play.spotify.com/track/1TK7llLvbHOVYxfcnEN6HK
            mpc.Add(@"spotify:track:1TK7llLvbHOVYxfcnEN6HK");
            mpc.Play();
        }
    }
}