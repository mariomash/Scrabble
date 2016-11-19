using SharpNFC.PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpNFC
{
	public class NFCDevice : IDisposable
	{
		//protected nfc_device device;
		public readonly IntPtr DevicePointer;

		protected internal NFCDevice(IntPtr devicePointer)
		{
			//var device = (nfc_device)Marshal.PtrToStructure(devicePointer, typeof(nfc_device));
			DevicePointer = devicePointer;
		}

		private bool transmit_bytes(byte[] pbtTx)
		{

			var szTx = (uint)pbtTx.Length;

			var pbtRx = new byte[10000];
			var szRx = (uint)pbtRx.Length;
			uint cycles = 0;

			var res = Functions.nfc_initiator_transceive_bytes_timed(DevicePointer, pbtTx, szTx, pbtRx, szRx, ref cycles);

			if (res < 0)
				return false;

			return true;


		}

		public void PollWithHalt()
		{

			// Initialise NFC device as "initiator"
			var initResult = Functions.nfc_initiator_init(DevicePointer);

			// Configure the CRC
			Functions.nfc_device_set_property_bool(
				DevicePointer,
				nfc_property.NP_HANDLE_CRC,
				false
			);

			// Use raw send/receive methods
			Functions.nfc_device_set_property_bool(
				DevicePointer,
				nfc_property.NP_EASY_FRAMING,
				false
			);

			// Disable 14443-4 autoswitching

			Functions.nfc_device_set_property_bool(
				DevicePointer,
				nfc_property.NP_AUTO_ISO14443_4,
				false
			);

			// Send the 7 bits request command specified in ISO 14443A (0x26)
			byte[] abtReqa = { 0x26 };
			transmit_bytes(abtReqa);

			// Anti-collision
			byte[] abtSelectAll = { 0x93, 0x20 };
			transmit_bytes(abtSelectAll);


		}

		public int Poll(List<nfc_modulation> modulation,
						byte pollCount,
						byte pollingInterval,
						out nfc_target nfc_target)
		{
			//var ptrArray = new IntPtr[modulation.Count];
			//for (int i = 0; i < modulation.Count; i++)
			//{
			//    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(modulation[i]));
			//    Marshal.StructureToPtr(modulation[i], ptr, false);
			//    ptrArray[i] = ptr;
			//}

			var target = new nfc_target();
			//var targetPtr = Marshal.AllocHGlobal(Marshal.SizeOf(target));

			var modArr = modulation.ToArray();
			var intResult = Functions.nfc_initiator_poll_target(
				DevicePointer,
				modArr,
				(uint)modArr.Length,
				pollCount,
				pollingInterval,
				out target);

			nfc_target = target;

			return intResult;
		}

		public void Dispose()
		{
			Functions.nfc_close(DevicePointer);
		}
	}
}
