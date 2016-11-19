﻿using System;
using System.Linq;
using System.Threading;
using Scrabble.Config;

namespace Scrabble.Threads
{
	public class NfcThread : BasicModule, IThread
	{

		public int DelayInMs;
		private bool _stopSignal;

		public NfcThread()
		{
			DelayInMs = 1000;
			_stopSignal = false;
		}

		private void Configuration_StopSignal(Configuration configuration)
		{
			_stopSignal = true;
		}

		public void Poll()
		{

			try
			{
				using (var context = new SharpNFC.NFCContext())
				{
					var deviceName = context.ListDeviceNames().FirstOrDefault();
					Configuration.Instance.Logger.Log(LogType.Verbose, $"Primer Device existente: {deviceName}");
					using (var device = context.OpenDevice(deviceName))
					{
						var list = new System.Collections.Generic.List<SharpNFC.PInvoke.nfc_modulation>()
							{
								new SharpNFC.PInvoke.nfc_modulation()
								{
									nbr = SharpNFC.PInvoke.nfc_baud_rate.NBR_106,
									nmt = SharpNFC.PInvoke.nfc_modulation_type.NMT_ISO14443A
								}
							};

						SharpNFC.PInvoke.nfc_target nfcTarget;
						device.Poll(
							list,
							1,
							100,
							out nfcTarget);

						Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget resultante {string.Join(",", nfcTarget.nti.abtUid)}");

						Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget.nti.szAtsLen {nfcTarget.nti.szAtsLen}");
					}
				}
			}
			catch (Exception ex)
			{
				Configuration.Logger.Log(LogType.Error, $"{ex}");
			}

		}

		public void PollWithHalt()
		{

			try
			{
				using (var context = new SharpNFC.NFCContext())
				{
					var deviceName = context.ListDeviceNames().FirstOrDefault();
					Configuration.Instance.Logger.Log(LogType.Verbose, $"Primer Device: {deviceName}");
					using (var device = context.OpenDevice(deviceName))
					{
						var list = new System.Collections.Generic.List<SharpNFC.PInvoke.nfc_modulation>()
							{
								new SharpNFC.PInvoke.nfc_modulation()
								{
									nbr = SharpNFC.PInvoke.nfc_baud_rate.NBR_106,
									nmt = SharpNFC.PInvoke.nfc_modulation_type.NMT_ISO14443A
								}
							};

						SharpNFC.PInvoke.nfc_target nfcTarget;
						device.Poll(
							list,
							1,
							100,
							out nfcTarget);

						Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget resultante {string.Join(",", nfcTarget.nti.abtUid)}");

						Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget.nti.szAtsLen {nfcTarget.nti.szAtsLen}");
					}
				}
			}
			catch (Exception ex)
			{
				Configuration.Logger.Log(LogType.Error, $"{ex}");
			}

		}

		public void WorkItem()
		{

			Configuration.Logger.Log(LogType.Verbose, $"Starting {this}");

			Configuration.StopSignal += Configuration_StopSignal;

			while (true)
			{

				if (_stopSignal)
				{
					Configuration.Logger.Log(LogType.Verbose, $"Stopping {this}");
					return;
				}

				if (!Active)
					continue;

				try
				{
					Configuration.Logger.Log(LogType.Verbose, $"{DateTime.Now} ejecutando {this}");

					PollWithHalt();

				}
				catch (Exception ex)
				{
					Configuration.Logger.Log(LogType.Error, $"{ex}");
				}

				Thread.Sleep(TimeSpan.FromMilliseconds(DelayInMs));

			}


		}
	}
}