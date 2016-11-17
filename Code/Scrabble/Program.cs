using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using System.Linq;
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

            try
            {

                var context = new SharpNFC.NFCContext();
                Configuration.Instance.Logger.Log(LogType.Verbose, $"context {context.ListDeviceNames().FirstOrDefault()}");
                var device = context.OpenDevice(context.ListDeviceNames().FirstOrDefault());
                Configuration.Instance.Logger.Log(LogType.Verbose, $"device {device}");
                var nfcTarget = new SharpNFC.PInvoke.nfc_target();
                Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget {nfcTarget}");
                var list = new System.Collections.Generic.List<SharpNFC.PInvoke.nfc_modulation>();
                Configuration.Instance.Logger.Log(LogType.Verbose, $"list {list}");
                list.Add(new SharpNFC.PInvoke.nfc_modulation()
                {
                    nbr = SharpNFC.PInvoke.nfc_baud_rate.NBR_106,
                    nmt = SharpNFC.PInvoke.nfc_modulation_type.NMT_ISO14443A
                });
                device.Pool(
                    list,
                    100,
                    100,
                    out nfcTarget);

                Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget resultante {nfcTarget}");

                Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget.nti.szAtsLen {nfcTarget.nti.szAtsLen}");
            }
            catch (Exception ex)
            {
                Configuration.Instance.Logger.Log(LogType.Verbose, $"{ex}");
            }

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
