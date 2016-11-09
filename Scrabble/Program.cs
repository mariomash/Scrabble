using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using Scrabble.Config;

namespace Scrabble {
    class Program {
        static void Main(string[] args)
        {

            // Please remember that there's also a singleton Configuration that should be
            // initialized when you first access...

            var monitorThread = new Thread(Configuration.Instance.MonitorThread.WorkItem);
            monitorThread.Start();

            var mpcThread = new Thread(Configuration.Instance.MpcThread.WorkItem);
            mpcThread.Start();

            var webThread = new Thread(Configuration.Instance.WebThread.WorkItem);
            webThread.Start();

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

            Configuration.SendStopSignal();

        }

    }
}
