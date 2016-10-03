/*
 * Copyright 2008 Matthias Sessler
 * 
 * This file is part of LibMpc.net.
 *
 * LibMpc.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 2.1 of the License, or
 * (at your option) any later version.
 *
 * LibMpc.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with LibMpc.net.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrabble.LibMpc {
    /// <summary>
    /// The delegate for the <see cref="MpcConnection.OnConnected"/> and <see cref="MpcConnection.OnDisconnected"/> events.
    /// </summary>
    /// <param name="connection">The connection firing the event.</param>
    public delegate void MpcConnectionEventDelegate(MpcConnection connection);
    /// <summary>
    /// Keeps the connection to the MPD server and handels the most basic structure of the
    /// MPD protocol. The high level commands are handeled in the <see cref="Mpc"/>
    /// class.
    /// </summary>
    public class MpcConnection {
        /// <summary>
        /// Is fired when a connection to a MPD server is established.
        /// </summary>
        public event MpcConnectionEventDelegate OnConnected;
        /// <summary>
        /// Is fired when the connection to the MPD server is closed.
        /// </summary>
        public event MpcConnectionEventDelegate OnDisconnected;

        private const string FirstLinePrefix = "OK MPD ";

        private const string Ok = "OK";
        private const string Ack = "ACK";

        private static readonly Regex AckRegex = new Regex("^ACK \\[(?<code>[0-9]*)@(?<nr>[0-9]*)] \\{(?<command>[a-z]*)} (?<message>.*)$");

        private IPEndPoint _ipEndPoint;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        private StreamReader _reader;
        private StreamWriter _writer;

        private string _version;
        /// <summary>
        /// If the connection to the MPD is connected.
        /// </summary>
        public bool Connected => (_tcpClient != null) && _tcpClient.Connected;

        /// <summary>
        /// The version of the MPD.
        /// </summary>
        public string Version => _version;

        private bool _autoConnect;
        /// <summary>
        /// If a connection should be established when a command is to be
        /// executed in disconnected state.
        /// </summary>
        public bool AutoConnect {
            get { return _autoConnect; }
            set { _autoConnect = value; }
        }
        /// <summary>
        /// Creates a new MpdConnection.
        /// </summary>
        public MpcConnection() { }
        /// <summary>
        /// Creates a new MpdConnection.
        /// </summary>
        /// <param name="server">The IPEndPoint of the MPD server.</param>
        public MpcConnection(IPEndPoint server) { Connect(server); }
        /// <summary>
        /// The IPEndPoint of the MPD server.
        /// </summary>
        /// <exception cref="AlreadyConnectedException">When a conenction to a MPD server is already established.</exception>
        public IPEndPoint Server {
            get { return _ipEndPoint; }
            set {
                if (Connected)
                    throw new AlreadyConnectedException();

                _ipEndPoint = value;

                ClearConnectionFields();
            }
        }
        /// <summary>
        /// Connects to a MPD server.
        /// </summary>
        /// <param name="server">The IPEndPoint of the server.</param>
        public void Connect(IPEndPoint server)
        {
            Server = server;
            Connect();
        }
        /// <summary>
        /// Connects to the MPD server who's IPEndPoint was set in the Server property.
        /// </summary>
        /// <exception cref="InvalidOperationException">If no IPEndPoint was set to the Server property.</exception>
        public void Connect()
        {
            if (_ipEndPoint == null)
                throw new InvalidOperationException("Server IPEndPoint not set.");

            if (Connected)
                throw new AlreadyConnectedException();

            _tcpClient = new TcpClient(
                _ipEndPoint.Address.ToString(),
                _ipEndPoint.Port);
            _networkStream = _tcpClient.GetStream();

            _reader = new StreamReader(_networkStream, Encoding.UTF8);
            _writer = new StreamWriter(_networkStream, Encoding.UTF8) { NewLine = "\n" };

            var firstLine = _reader.ReadLine();
            if (firstLine != null && !firstLine.StartsWith(FirstLinePrefix))
            {
                Disconnect();
                throw new InvalidDataException("Response of mpd does not start with \"" + FirstLinePrefix + "\".");
            }
            _version = firstLine.Substring(FirstLinePrefix.Length);

            _writer.WriteLine();
            _writer.Flush();

            readResponse();

            OnConnected?.Invoke(this);
        }
        /// <summary>
        /// Disconnects from the current MPD server.
        /// </summary>
        public void Disconnect()
        {
            if (_tcpClient == null)
                return;

            _networkStream.Close();

            ClearConnectionFields();

            OnDisconnected?.Invoke(this);
        }
        /// <summary>
        /// Executes a simple command without arguments on the MPD server and returns the response.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>The MPD server response parsed into a basic object.</returns>
        /// <exception cref="ArgumentException">If the command contains a space of a newline charakter.</exception>
        public MpdResponse Exec(string command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.Contains(" "))
                throw new ArgumentException("command contains space");
            if (command.Contains("\n"))
                throw new ArgumentException("command contains newline");

            CheckConnected();

            try
            {
                _writer.WriteLine(command);
                _writer.Flush();

                return readResponse();
            }
            catch (Exception)
            {
                try { Disconnect(); }
                catch (Exception)
                {
                    // ignored
                }
                throw;
            }
        }
        /// <summary>
        /// Executes a MPD command with arguments on the MPD server.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="argument">The arguments of the command.</param>
        /// <returns>The MPD server response parsed into a basic object.</returns>
        /// <exception cref="ArgumentException">If the command contains a space of a newline charakter.</exception>
        public MpdResponse Exec(string command, string[] argument)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.Contains(" "))
                throw new ArgumentException("command contains space");
            if (command.Contains("\n"))
                throw new ArgumentException("command contains newline");

            if (argument == null)
                throw new ArgumentNullException(nameof(argument));
            for (var i = 0; i < argument.Length; i++)
            {
                if (argument[i] == null)
                    throw new ArgumentNullException("argument[" + i + "]");
                if (argument[i].Contains("\n"))
                    throw new ArgumentException("argument[" + i + "] contains newline");
            }

            CheckConnected();

            try
            {
                _writer.Write(command);
                foreach (var arg in argument)
                {
                    _writer.Write(' ');
                    WriteToken(arg);
                }
                _writer.WriteLine();
                _writer.Flush();

                return readResponse();
            }
            catch (Exception)
            {
                try { Disconnect(); }
                catch (Exception)
                {
                    // ignored
                }
                throw;
            }
        }

        private void CheckConnected()
        {
            if (Connected) return;
            if (_autoConnect)
                Connect();
            else
                throw new NotConnectedException();
        }

        private void WriteToken(string token)
        {
            if (token.Contains(" "))
            {
                _writer.Write("\"");
                foreach (var chr in token)
                    if (chr == '"')
                        _writer.Write("\\\"");
                    else
                        _writer.Write(chr);
            }
            else
                _writer.Write(token);
        }

        private MpdResponse readResponse()
        {
            var ret = new List<string>();
            var line = _reader.ReadLine();
            while (!(line.Equals(Ok) || line.StartsWith(Ack)))
            {
                ret.Add(line);
                line = _reader.ReadLine();
            }
            if (line.Equals(Ok))
                return new MpdResponse(new ReadOnlyCollection<string>(ret));
            var match = AckRegex.Match(line);

            if (match.Groups.Count != 5)
                throw new InvalidDataException("Error response not as expected");

            return new MpdResponse(
                int.Parse(match.Result("${code}")),
                int.Parse(match.Result("${nr}")),
                match.Result("${command}"),
                match.Result("${message}"),
                new ReadOnlyCollection<string>(ret)
            );
        }

        private void ClearConnectionFields()
        {
            _tcpClient = null;
            _networkStream = null;
            _reader = null;
            _writer = null;
            _version = null;
        }
    }
}
