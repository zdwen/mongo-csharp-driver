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
using System.Diagnostics;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// The default channel provider, implemented as a connection pool.
    /// </summary>
    internal sealed class ConnectionPoolChannelProvider : ChannelProviderBase
    {
        // private fields
        private readonly IConnectionPool _connectionPool;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly IEventPublisher _events;
        private readonly StateHelper _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolChannelProvider" /> class.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="connectionPool">The connection factory.</param>
        /// <param name="events">The events.</param>
        public ConnectionPoolChannelProvider(DnsEndPoint dnsEndPoint, IConnectionPool connectionPool, IEventPublisher events)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("connectionFactory", connectionPool);
            Ensure.IsNotNull("events", events);

            _connectionPool = connectionPool;
            _dnsEndPoint = dnsEndPoint;
            _events = events;
            _state = new StateHelper(State.Unitialized);
        }

        // public properties
        /// <summary>
        /// Gets the DNS end point.
        /// </summary>
        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        // public methods
        /// <summary>
        /// Gets a connection.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A connection.</returns>
        /// <exception cref="MongoDriverException">Too many threads are already waiting for a connection.</exception>
        public override IChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var connection = _connectionPool.GetConnection(timeout, cancellationToken);
            try
            {
                connection.Open();
            }
            catch
            {
                connection.Dispose();
                throw;
            }
            return new ConnectionChannel(connection);
        }

        /// <summary>
        /// Initializes the channel provider.
        /// </summary>
        public override void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Unitialized, State.Initialized))
            {
                _connectionPool.Initialize();
            }
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed) && disposing)
            {
                _connectionPool.Dispose();
            }
            base.Dispose(disposing);
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_state.Current == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // nested classes
        private static class State
        {
            public const int Unitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }

        private sealed class ConnectionChannel : ChannelBase
        {
            private readonly IConnection _connection;
            private bool _disposed;

            public ConnectionChannel(IConnection connection)
            {
                _connection = connection;
            }

            public IConnection Connection
            {
                get
                {
                    ThrowIfDisposed();
                    return _connection;
                }
            }

            public override DnsEndPoint DnsEndPoint
            {
                get
                {
                    ThrowIfDisposed();
                    return _connection.DnsEndPoint;
                }
            }

            public override ReplyMessage Receive(ChannelReceiveArgs args)
            {
                ThrowIfDisposed();

                var message = _connection.Receive();
                if (message.ResponseTo != args.RequestId)
                {
                    var formatted = string.Format("Expected a response to '{0}' but got '{1}' instead.", args.RequestId, message.ResponseTo);
                    throw new MongoProtocolException(formatted);
                }

                return message;
            }

            public override void Send(IRequestPacket packet)
            {
                ThrowIfDisposed();
                _connection.Send(packet);
            }

            public override string ToString()
            {
                return _connection.ToString();
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _connection.Dispose();
                    }
                    _disposed = true;
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }
    }
}
