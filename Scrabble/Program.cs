using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using Scrabble.Config;
using System.Linq;

namespace Scrabble {
    class Program {
        static void Main(string[] args)
        {

            // Please remember that there's also a singleton Configuration that should be
            // already initialized...

            var monitorThread = new Thread(Configuration.Instance.MonitorThread.WorkItem);
            monitorThread.Start();

            var mpcThread = new Thread(Configuration.Instance.MpcThread.WorkItem);
            mpcThread.Start();

            var webThread = new Thread(Configuration.Instance.WebThread.WorkItem);
            webThread.Start();

			var context = new SharpNFC.NFCContext();
			var device = context.OpenDevice(context.ListDeviceNames().FirstOrDefault());
			var nfcTarget = new SharpNFC.PInvoke.nfc_target();
			var list = new System.Collections.Generic.List<SharpNFC.PInvoke.nfc_modulation>();
			list.Add(new SharpNFC.PInvoke.nfc_modulation() {
				nbr = SharpNFC.PInvoke.nfc_baud_rate.NBR_106,
				nmt = SharpNFC.PInvoke.nfc_modulation_type.NMT_ISO14443A
			});
			device.Pool(
				list,
				100,
				100,
				out nfcTarget);
			var eo = nfcTarget.nti.szAtsLen;

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
