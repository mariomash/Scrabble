using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using Scrabble.Config;
using System.Linq;
using SharpNFC;
using SharpNFC.PInvoke;

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

			/* TODO comprobar que este modo funciona
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
			*/




			/*
			// To compile this simple example:
// $ gcc -o quick_start_example1 quick_start_example1.c -lnfc

#include <stdlib.h>
#include <nfc/nfc.h>

static void
print_hex(const uint8_t *pbtData, const size_t szBytes)
{
  size_t  szPos;

  for (szPos = 0; szPos < szBytes; szPos++) {
    printf("%02x  ", pbtData[szPos]);
  }
  printf("\n");
}

int
main(int argc, const char *argv[])
{
  nfc_device *pnd;
  nfc_target nt;

  // Allocate only a pointer to nfc_context
  nfc_context *context;

  // Initialize libnfc and set the nfc_context
  nfc_init(&context);
  if (context == NULL) {
    printf("Unable to init libnfc (malloc)\n");
    exit(EXIT_FAILURE);
  }

  // Display libnfc version
  const char *acLibnfcVersion = nfc_version();
  (void)argc;
  printf("%s uses libnfc %s\n", argv[0], acLibnfcVersion);

  // Open, using the first available NFC device which can be in order of selection:
  //   - default device specified using environment variable or
  //   - first specified device in libnfc.conf (/etc/nfc) or
  //   - first specified device in device-configuration directory (/etc/nfc/devices.d) or
  //   - first auto-detected (if feature is not disabled in libnfc.conf) device
  pnd = nfc_open(context, NULL);

  if (pnd == NULL) {
    printf("ERROR: %s\n", "Unable to open NFC device.");
    exit(EXIT_FAILURE);
  }
  // Set opened NFC device to initiator mode
  if (nfc_initiator_init(pnd) < 0) {
    nfc_perror(pnd, "nfc_initiator_init");
    exit(EXIT_FAILURE);
  }

  printf("NFC reader: %s opened\n", nfc_device_get_name(pnd));

  // Poll for a ISO14443A (MIFARE) tag
  const nfc_modulation nmMifare = {
    .nmt = NMT_ISO14443A,
    .nbr = NBR_106,
  };
  if (nfc_initiator_select_passive_target(pnd, nmMifare, NULL, 0, &nt) > 0) {
    printf("The following (NFC) ISO14443A tag was found:\n");
    printf("    ATQA (SENS_RES): ");
    print_hex(nt.nti.nai.abtAtqa, 2);
    printf("       UID (NFCID%c): ", (nt.nti.nai.abtUid[0] == 0x08 ? '3' : '1'));
    print_hex(nt.nti.nai.abtUid, nt.nti.nai.szUidLen);
    printf("      SAK (SEL_RES): ");
    print_hex(&nt.nti.nai.btSak, 1);
    if (nt.nti.nai.szAtsLen) {
      printf("          ATS (ATR): ");
      print_hex(nt.nti.nai.abtAts, nt.nti.nai.szAtsLen);
    }
  }
  // Close NFC device
  nfc_close(pnd);
  // Release the context
  nfc_exit(context);
  exit(EXIT_SUCCESS);
}
			*/


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
