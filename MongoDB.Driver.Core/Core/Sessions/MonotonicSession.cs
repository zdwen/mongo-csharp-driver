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
using System.Linq;
using System.Threading;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Sessions
{
    /// <summary>
    /// A session bound to a particular server.
    /// </summary>
    public sealed class MonotonicSession : ISession
    {
        // private fields
        private readonly ICluster _cluster;
        private bool _disposed;
        private bool _pinnedToPrimary;
        private IServer _serverToUse;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicSession" /> class.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        public MonotonicSession(ICluster cluster)
        {
            Ensure.IsNotNull("cluster", cluster);

            _cluster = cluster;
        }

        // public methods
        /// <summary>
        /// Creates an operation channel provider.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>An operation channel provider.</returns>
        public IServerChannelProvider CreateServerChannelProvider(CreateServerChannelProviderArgs args)
        {
            Ensure.IsNotNull("args", args);
            ThrowIfDisposed();

            if (_serverToUse == null || (!args.IsQuery && !_pinnedToPrimary))
            {
                _serverToUse = _cluster.SelectServer(args.ServerSelector, args.Timeout, args.CancellationToken);
                _pinnedToPrimary |= !args.IsQuery;
            }

            // verify that the server selector for the operation is compatible with the selected server.
            var selected = args.ServerSelector.SelectServers(new[] { _serverToUse.Description });
            if (!selected.Any())
            {
                throw new Exception("The current operation does not match the selected server.");
            }

            return new MonotonicServerChannelProvider(this, _serverToUse, args.DisposeSession);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        //private methods
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // nested classes
        private sealed class MonotonicServerChannelProvider : IServerChannelProvider
        {
            private readonly MonotonicSession _session;
            private readonly IServer _server;
            private readonly bool _disposeSession;
            private bool _disposed;

            public MonotonicServerChannelProvider(MonotonicSession session, IServer server, bool disposeSession)
            {
                _session = session;
                _server = server;
                _disposeSession = disposeSession;
            }

            public ServerDescription Server
            {
                get
                {
                    ThrowIfDisposed();
                    return _server.Description;
                }
            }

            public IChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _server.GetChannel(timeout, cancellationToken);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (_disposeSession)
                    {
                        _session.Dispose();
                    }
                    GC.SuppressFinalize(this);
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                _session.ThrowIfDisposed();
            }
        }
    }
}
