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

using System;
using System.Threading;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Settings for the <see cref="NetworkStreamFactory"/>.
    /// </summary>
    public sealed class NetworkStreamSettings
    {
        // public static fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly NetworkStreamSettings Defaults = new Builder().Build();

        // private fields
        private readonly TimeSpan _connectTimeout;
        private readonly TimeSpan _readTimeout;
        private readonly int _tcpReceiveBufferSize;
        private readonly int _tcpSendBufferSize;
        private readonly TimeSpan _writeTimeout;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkStreamSettings" /> class.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readTimeout">The socket read timeout.</param>
        /// <param name="writeTimeout">The socket write timeout.</param>
        /// <param name="tcpReceiveBufferSize">The size of the TCP receive buffer.</param>
        /// <param name="tcpSendBufferSize">The size of the TCP send buffer.</param>
        public NetworkStreamSettings(TimeSpan connectTimeout, TimeSpan readTimeout, TimeSpan writeTimeout, int tcpReceiveBufferSize, int tcpSendBufferSize)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("connectTimeout", connectTimeout);
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("readTimeout", readTimeout);
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("writeTimeout", writeTimeout);
            Ensure.IsGreaterThan("tcpReceiveBufferSize", tcpReceiveBufferSize, 0);
            Ensure.IsGreaterThan("tcpSendBufferSize", tcpSendBufferSize, 0);

            _connectTimeout = connectTimeout;
            _readTimeout= readTimeout;
            _writeTimeout = writeTimeout;
            _tcpReceiveBufferSize = tcpReceiveBufferSize;
            _tcpSendBufferSize = tcpSendBufferSize;
        }

        // public properties
        /// <summary>
        /// Gets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
        }

        /// <summary>
        /// Gets the socket read timeout.
        /// </summary>
        public TimeSpan ReadTimeout
        {
            get { return _readTimeout; }
        }

        /// <summary>
        /// Gets the size of the TCP receive buffer.
        /// </summary>
        public int TcpReceiveBufferSize
        {
            get { return _tcpReceiveBufferSize; }
        }

        /// <summary>
        /// Gets the size of the TCP send buffer.
        /// </summary>
        public int TcpSendBufferSize
        {
            get { return _tcpSendBufferSize; }
        }

        /// <summary>
        /// Gets the socket write timeout.
        /// </summary>
        public TimeSpan WriteTimeout
        {
            get { return _writeTimeout; }
        }

        // public methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{{ ConnectTimeout: '{0}', ReadTimeout: '{1}', TcpReceiveBufferSize: {2}, TcpSendBufferSize: {3}, WriteTimeout: '{4}' }}",
                _connectTimeout,
                _readTimeout,
                _tcpReceiveBufferSize,
                _tcpSendBufferSize,
                _writeTimeout);
        }

        // public static methods
        /// <summary>
        /// A method used to build up settings.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static NetworkStreamSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up NetworkStreamFactorySettings.
        /// </summary>
        public sealed class Builder
        {
            private TimeSpan _connectTimeout;
            private TimeSpan _readTimeout;
            private int _tcpReceiveBufferSize;
            private int _tcpSendBufferSize;
            private TimeSpan _writeTimeout;

            internal Builder()
            {
                _connectTimeout = TimeSpan.FromSeconds(30);
                _readTimeout = TimeSpan.FromMilliseconds(Timeout.Infinite); // OS default
                _tcpReceiveBufferSize = 64 * 1024; // 64KiB (note: larger than 2MiB fails on Mac using Mono)
                _tcpSendBufferSize = 64 * 1024; // 64KiB (TODO: what is the optimum value for the buffers?)
                _writeTimeout = TimeSpan.FromMilliseconds(Timeout.Infinite); // OS default
            }

            internal NetworkStreamSettings Build()
            {
                return new NetworkStreamSettings(
                    _connectTimeout,
                    _readTimeout,
                    _writeTimeout,
                    _tcpReceiveBufferSize,
                    _tcpSendBufferSize);
            }

            /// <summary>
            /// Sets the connect timeout.
            /// </summary>
            /// <param name="timeout">The timeout.</param>
            public void SetConnectTimeout(TimeSpan timeout)
            {
                _connectTimeout = timeout;
            }

            /// <summary>
            /// Sets the socket read timeout.
            /// </summary>
            /// <param name="timeout">The timeout.</param>
            public void SetReadTimeout(TimeSpan timeout)
            {
                _readTimeout = timeout;
            }

            /// <summary>
            /// Sets the size of the TCP receive buffer.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetTcpReceiveBufferSize(int size)
            {
                _tcpReceiveBufferSize = size;
            }

            /// <summary>
            /// Sets the size of the TCP send buffer.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetTcpSendBufferSize(int size)
            {
                _tcpSendBufferSize = size;
            }

            /// <summary>
            /// Sets the socket write timeout.
            /// </summary>
            /// <param name="timeout">The timeout.</param>
            public void SetWriteTimeout(TimeSpan timeout)
            {
                _writeTimeout = timeout;
            }
        }
    }
}