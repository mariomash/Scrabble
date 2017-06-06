using System;
using System.Linq;
using System.Threading;
using Scrabble.Config;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;

namespace Scrabble.Threads
{
	public class NfcThread : BasicModule, IThread
	{
		static SerialPort _serialPort;
		static Thread _SerialDataCheckThread;
		bool _SerialDataCheckActivated;

		readonly byte[] EchoCommand = { 0x55 };
		readonly byte[] IdnCommand = { 0x01, 0x00 };
		readonly byte[] FieldOffCommand = { 0x02, 0x02, 0x00, 0x00 };
		readonly byte[] InventoryCommand = { 0x04, 0x03, 0x26, 0x01, 0x00 };
		readonly byte[] ProtocolSelectISO15693Command = { 0x02, 0x02, 0x01, 0x05 };
		readonly byte[] ProtocolSelectISO14443ACommand = { 0x02, 0x02, 0x02, 0x00 };
		readonly byte[] ProtocolSelectISO14443BCommand = { 0x02, 0x02, 0x03, 0x01 };

		public int DelayInMs;
		bool _stopSignal;

		public NfcThread()
		{
			DelayInMs = 1000;
			_stopSignal = false;
		}

		void Configuration_StopSignal(Configuration configuration)
		{
			_stopSignal = true;
		}

		/*
		public void Poll()
		{

			try
			{
				using (var context = new SharpNFC.NFCContext())
				{
					var deviceName = context.ListDeviceNames().FirstOrDefault();
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

						Configuration.Instance.Logger.Log(LogType.Verbose, $"-POLL--------{Environment.NewLine}Device: {deviceName}{Environment.NewLine}ID ==> {string.Join(":", nfcTarget.nti.abtUid)}{Environment.NewLine}");

						//Configuration.Instance.Logger.Log(LogType.Verbose, $"nfcTarget.nti.szAtsLen {nfcTarget.nti.szAtsLen}");

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
					using (var device = context.OpenDevice(deviceName))
					{

						var listIDs = device.PollWithHalt();

						Configuration.Instance.Logger.Log(LogType.Verbose, $"-POLLHALT----{Environment.NewLine}Primer Device: {deviceName}{Environment.NewLine}IDs ==> {string.Join(",", listIDs)}{Environment.NewLine}");

					}
				}
			}
			catch (Exception ex)
			{
				Configuration.Logger.Log(LogType.Error, $"{ex}");
			}

		}
		*/

		public void SerialDataCheckThread()
		{
			while (_SerialDataCheckActivated)
			{
				var bytes = new List<byte>();
				while (_serialPort.BytesToRead != 0)
				{
					var tmpByte = (byte)_serialPort.ReadByte();
					bytes.Add(tmpByte);
					Thread.Sleep(20);
				}

				if (bytes.Any())
					Configuration.Logger.Log(LogType.Verbose, $"{_serialPort.PortName} >> String: {System.Text.Encoding.Default.GetString(bytes.ToArray())} || Dec:{String.Join("-", bytes)} || Hex:{BitConverter.ToString(bytes.ToArray())}");
			}
		}

		public void SerialDataWrite(byte[] data)
		{
			Configuration.Logger.Log(LogType.Verbose, $"{_serialPort.PortName} << {BitConverter.ToString(data.ToArray())}");
			_serialPort.Write(data, 0, data.Length);
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

					if (_serialPort == null)
						_serialPort = new SerialPort("/dev/ttyAMA0")
						{
							BaudRate = 57600,
							//Parity = Parity.None,
							//StopBits = StopBits.One,
							//DataBits = 8,
							//Handshake = Handshake.None
							//ReadTimeout = 400,
							//WriteTimeout = 400,
						};

					if (!_serialPort.IsOpen)
						_serialPort.Open();

					_SerialDataCheckActivated = true;
					if (_SerialDataCheckThread == null)
						_SerialDataCheckThread = new Thread(SerialDataCheckThread);

					if (_SerialDataCheckThread.ThreadState != ThreadState.Running)
						_SerialDataCheckThread.Start();

					for (var i = 0; i < 5; i++)
					{
						SerialDataWrite(EchoCommand);
						Thread.Sleep(500);
					}

					SerialDataWrite(IdnCommand);
					Thread.Sleep(1000);

					SerialDataWrite(ProtocolSelectISO15693Command);
					Thread.Sleep(1000);

					SerialDataWrite(InventoryCommand);
					Thread.Sleep(1000);

					SerialDataWrite(FieldOffCommand);
					Thread.Sleep(1000);

					Console.WriteLine("Press any key to finish...");
					Console.WriteLine();
					Console.ReadKey();

					//Poll();
					//PollWithHalt();

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