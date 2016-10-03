using System;
using System.Net;
using Mono.Unix;
using Mono.Unix.Native;
using Nancy.Hosting.Self;
using Scrabble.LibMpc;

namespace Scrabble {
    class Program {
        static void Main(string[] args)
        {
            var mpc = new Mpc
            {
                Connection = new MpcConnection
                {
                    Server = new IPEndPoint(IPAddress.Parse(@"127.0.0.1"), 6600),
                    AutoConnect = false
                }
            };

            mpc.OnConnected += Mpc_OnConnected;

            //mpc.Connection.Connect();

            const string uri = "http://localhost:8888";
            Console.WriteLine("Starting Nancy on " + uri);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(uri));
            host.Start();  // start hosting

            // check if we're running on mono
            if (Type.GetType("Mono.Runtime") != null)
            {
                // on mono, processes will usually run as daemons - this allows you to listen
                // for termination signals (ctrl+c, shutdown, etc) and finalize correctly
                UnixSignal.WaitAny(new[] {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM),
                    new UnixSignal(Signum.SIGQUIT),
                    new UnixSignal(Signum.SIGHUP)
                });
            }
            else
            {
                Console.ReadLine();
            }

            Console.WriteLine("Stopping");
            host.Stop();  // stop hosting

        }

        private static void Mpc_OnConnected(Mpc mpc)
        {
            //https://play.spotify.com/track/1TK7llLvbHOVYxfcnEN6HK
            mpc.Add(@"spotify:track:1TK7llLvbHOVYxfcnEN6HK");
            mpc.Play();
        }
    }
}
