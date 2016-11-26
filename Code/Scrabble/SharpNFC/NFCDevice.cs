using SharpNFC.PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Scrabble.Config;
using System.Collections.ObjectModel;

namespace SharpNFC
{
	public class NFCDevice : IDisposable
	{
		//protected nfc_device device;
		public readonly IntPtr DevicePointer;
		const byte CASCADE_BIT = 0x04;
		const int MAX_FRAME_LEN = 264;
		const byte SAK_FLAG_ATS_SUPPORTED = 0x20;

		protected internal NFCDevice(IntPtr devicePointer)
		{
			//var device = (nfc_device)Marshal.PtrToStructure(devicePointer, typeof(nfc_device));
			DevicePointer = devicePointer;
		}

		private bool transmit_bits(byte[] pbtTx, uint szTxBits, ref byte[] pbtRx)
		{

			var szRx = (uint)pbtRx.Length;

			// Transmit the bit frame command, we don't use the arbitrary parity feature

			var res = Functions.nfc_initiator_transceive_bits(DevicePointer, pbtTx, szTxBits, null, pbtRx, szRx, null);

			if (res < 0)
				return false;

			return true;

		}

		private bool transmit_bytes(byte[] pbtTx, uint szTx, ref byte[] pbtRx)
		{

			var szRx = (uint)pbtRx.Length;

			var res = Functions.nfc_initiator_transceive_bytes(DevicePointer, pbtTx, szTx, pbtRx, szRx, 0);

			if (res < 0)
				return false;

			return true;


		}

		public List<string> PollWithHalt()
		{
			var szCL = 1;//Always start with Cascade Level 1 (CL1)

			var abtRx = new byte[MAX_FRAME_LEN];

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
			transmit_bits(abtReqa, 7, ref abtRx);

			// Anti-collisionn
			byte[] abtSelectAll = { 0x93, 0x20 };
			transmit_bytes(abtSelectAll, 2, ref abtRx);
			var abtRawUid = new List<byte>();
			var completeAbtRawUid = abtRx.ToList();
			abtRawUid.AddRange(abtRx.ToList().Take(4));

			// Prepare and send CL1 Select-Command
			// byte[] abtSelectTag = { 0x93, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var abtSelectTagArray = new ArrayList { (byte)0x93, (byte)0x70 };
			abtSelectTagArray.AddRange(abtRawUid);
			abtSelectTagArray.Add((byte)0x00);
			abtSelectTagArray.Add((byte)0x00);

			byte[] abtSelectTag = new byte[9];
			for (var i = 0; i < abtSelectTagArray.Count; i++)
				abtSelectTag[i] = (byte)abtSelectTagArray[i];

			Functions.iso14443a_crc_append(abtSelectTag, 7);
			transmit_bytes(abtSelectTag, 9, ref abtRx);
			byte abtSak = abtRx[0];

			// Test if we are dealing with a CL2
			var e = (abtSak & CASCADE_BIT);
			if (e == 1)
			{
				szCL = 2;//or more
			}

			if (szCL == 2) // We have to do the anti-collision for cascade level 2
			{
				// Prepare CL2 commands
				abtSelectAll[0] = 0x95;

				// Anti-collision
				transmit_bytes(abtSelectAll, 2, ref abtRx);

				// Save UID CL2
				abtRawUid.AddRange(abtRx.ToList().Take(4));

				// Selection
				abtSelectTagArray = new ArrayList { (byte)0x95, (byte)0x70 };
				abtSelectTagArray.AddRange(abtRx);
				abtSelectTagArray.Add((byte)0x00);
				abtSelectTagArray.Add((byte)0x00);

				abtSelectTag = new byte[9];
				for (var i = 0; i < abtSelectTagArray.Count; i++)
					abtSelectTag[i] = (byte)abtSelectTagArray[i];

				Functions.iso14443a_crc_append(abtSelectTag, 7);
				transmit_bytes(abtSelectTag, 9, ref abtRx);
				abtSak = abtRx[0];

				// Test if we are dealing with a CL2
				e = (abtSak & CASCADE_BIT);
				if (e == 1)
				{
					szCL = 3;//or more
				}

				if (szCL == 3) // We have to do the anti-collision for cascade level 3
				{
					// Prepare and send CL3 AC-Command
					abtSelectAll[0] = 0x97;

					transmit_bytes(abtSelectAll, 2, ref abtRx);

					// Save UID CL3
					abtRawUid.AddRange(abtRx.ToList().Take(4));

					// Prepare and send final Select-Command
					abtSelectTagArray = new ArrayList { (byte)0x97, (byte)0x70 };
					abtSelectTagArray.AddRange(abtRx);
					abtSelectTagArray.Add((byte)0x00);
					abtSelectTagArray.Add((byte)0x00);

					abtSelectTag = new byte[9];
					for (var i = 0; i < abtSelectTagArray.Count; i++)
						abtSelectTag[i] = (byte)abtSelectTagArray[i];

					Functions.iso14443a_crc_append(abtSelectTag, 7);
					transmit_bytes(abtSelectTag, 9, ref abtRx);
					abtSak = abtRx[0];
				}

			}

			// Request ATS, this only applies to tags that support ISO 14443A-4
			//byte[] abtRats = { 0xe0, 0x50, 0x00, 0x00 };
			// We wont implement ATS...

			byte[] abtHalt = { 0x50, 0x00, 0x00, 0x00 };
			Functions.iso14443a_crc_append(abtHalt, 2);
			transmit_bytes(abtHalt, 4, ref abtRx);

			var RawUID = string.Join(":", completeAbtRawUid);

			return new List<string> { RawUID };

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
