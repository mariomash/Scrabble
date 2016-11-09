﻿using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;
using Scrabble.Config;
using Scrabble.LibMpc;

namespace Scrabble.Threads {
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

            while (true)
            {
                if (!Active)
                    continue;

                try
                {

                    if (_stopSignal)
                    {
                        Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
                        return;
                    }

                    if (!Mpc.Connected)
                        Mpc.Connection.Connect();

                }
                catch (Exception ex)
                {
                    Configuration.Logger.Log(LogType.Error, $"{ex.Message}", new StackTrace());
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(DelayInMs));

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
            //mpc.Add(@"spotify:track:1TK7llLvbHOVYxfcnEN6HK");
            mpc.Add(@"kasumi.mp3");
            mpc.Play();
        }
    }
}