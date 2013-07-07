/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.IO;
using System.Net;
using System.Net.Sockets;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Creates a <see cref="NetworkStream"/>.
    /// </summary>
    public class NetworkStreamFactory : IStreamFactory
    {
        // private fields
        private readonly DnsCache _dnsCache;
        private readonly NetworkStreamFactorySettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkStreamFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="dnsCache">The DNS cache.</param>
        public NetworkStreamFactory(NetworkStreamFactorySettings settings, DnsCache dnsCache)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("dnsCache", dnsCache);

            _settings = settings;
            _dnsCache = dnsCache;
        }

        // public methods
        /// <summary>
        /// Creates a NetowrkStream for the specified DNS end point.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A NetworkStream.</returns>
        public Stream Create(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            var ipEndPoint = _dnsCache.Resolve(dnsEndPoint);

            var protocolType = ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ?
                ProtocolType.IPv6 :
                ProtocolType.IP;

            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, protocolType);

            OnBeforeConnectingSocket(socket);

            var connectResult = socket.BeginConnect(ipEndPoint, null, null);
            connectResult.AsyncWaitHandle.WaitOne(_settings.ConnectTimeout);
            if (!socket.Connected)
            {
                socket.Close();
                throw new SocketException(10060); // Connection timed out.
            }

            OnAfterConnectingSocket(socket);

            var stream = new NetworkStream(socket, ownsSocket: true);

            var readTimeout = (int)_settings.ReadTimeout.TotalMilliseconds;
            if (readTimeout > 0)
            {
                stream.ReadTimeout = readTimeout;
            }

            var writeTimeout = (int)_settings.WriteTimeout.TotalMilliseconds;
            if (writeTimeout > 0)
            {
                stream.WriteTimeout = writeTimeout;
            }

            return stream;
        }

        // protected methods
        /// <summary>
        /// Called after the socket has been connected.
        /// </summary>
        /// <param name="socket">The socket.</param>
        protected virtual void OnAfterConnectingSocket(Socket socket)
        {
            socket.NoDelay = true;
            socket.ReceiveBufferSize = _settings.TcpReceiveBufferSize;
            socket.SendBufferSize = _settings.TcpSendBufferSize;
        }

        /// <summary>
        /// Called before connecting the socket.
        /// </summary>
        /// <param name="socket">The socket.</param>
        protected virtual void OnBeforeConnectingSocket(Socket socket)
        {
            // nothing to do...
        }
    }
}