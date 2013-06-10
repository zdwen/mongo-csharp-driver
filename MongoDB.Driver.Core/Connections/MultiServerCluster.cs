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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Manages multiple <see cref="IServer"/>s.
    /// </summary>
    public abstract class MultiServerCluster : ClusterBase
    {
        // private fields
        private readonly object _serversLock = new object();
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ConcurrentDictionary<DnsEndPoint, IClusterableServer> _servers;
        private readonly MultiServerClusterType _type;
        private MultiServerClusterDescription _currentDescription;
        private ManualResetEventSlim _currentWaitHandle;
        private int _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiServerCluster" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="dnsEndPoints">The addresses.</param>
        /// <param name="serverFactory">The server factory.</param>
        protected MultiServerCluster(MultiServerClusterType type, IEnumerable<DnsEndPoint> dnsEndPoints, IClusterableServerFactory serverFactory)
        {
            Ensure.IsNotNull("dnsEndPoints", dnsEndPoints);
            Ensure.IsNotNull("serverFactory", serverFactory);

            _serverFactory = serverFactory;
            _servers = new ConcurrentDictionary<DnsEndPoint, IClusterableServer>();
            _type = type;
            _currentWaitHandle = new ManualResetEventSlim();

            var descriptions = new List<ServerDescription>();
            foreach (var dnsEndPoint in dnsEndPoints)
            {
                var server = CreateServer(dnsEndPoint);
                if (_servers.TryAdd(dnsEndPoint, server))
                {
                    server.DescriptionUpdated += ServerDescriptionUpdated;
                    descriptions.Add(server.Description);
                }
            }

            _currentDescription = new MultiServerClusterDescription(_type, descriptions);
        }

        // public properties
        /// <summary>
        /// Gets the description.
        /// </summary>
        public sealed override ClusterDescription Description
        {
            get 
            {
                ThrowIfDisposed();
                return Interlocked.CompareExchange(ref _currentDescription, null, null); 
            }
        }

        // public methods
        /// <summary>
        /// Initializes the cluster.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Initialize()
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _state, (int)State.Initialized, (int)State.Unitialized) == (int)State.Unitialized)
            {
                foreach (var server in _servers)
                {
                    server.Value.Initialize();
                }
            }
        }

        /// <summary>
        /// Selects a server using the specified selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A server.</returns>
        /// <exception cref="MongoDriverException"></exception>
        public sealed override IServer SelectServer(IServerSelector selector, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull("selector", selector);

            ThrowIfUninitialized();
            ThrowIfDisposed();

            Func<IEnumerable<ServerDescription>> getDescriptions = () => ((MultiServerClusterDescription)Interlocked.CompareExchange(ref _currentDescription, null, null)).Servers;

            var timeoutAt = DateTime.UtcNow.Add(timeout);
            TimeSpan remaining;
            ManualResetEventSlim currentWaitHandle;
            do
            {
                currentWaitHandle = Interlocked.CompareExchange(ref _currentWaitHandle, null, null);
                var descriptions = getDescriptions();

                var selectedDescription = selector.SelectServer(descriptions);
                IClusterableServer selectedServer;
                if (selectedDescription != null && _servers.TryGetValue(selectedDescription.DnsEndPoint, out selectedServer))
                {
                    return new DisposalProtectedServer(selectedServer);
                }

                if (!descriptions.Any(x => x.Status == ServerStatus.Connecting))
                {
                    // nothing we can do if none are connecting
                    break;
                }

                if (timeout.TotalMilliseconds == Timeout.Infinite)
                {
                    remaining = TimeSpan.FromMilliseconds(Timeout.Infinite);
                }
                else
                {
                    remaining = timeoutAt - DateTime.UtcNow;
                }
            }
            while ((timeout.TotalMilliseconds == Timeout.Infinite || remaining > TimeSpan.Zero) && currentWaitHandle.Wait(remaining, cancellationToken));

            throw new MongoDriverException(string.Format("Unable to find a server matching '{0}'.", selector.Description));
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _state, State.Disposed) != State.Disposed && disposing)
            {
                lock (_serversLock)
                {
                    foreach (var server in _servers.Values)
                    {
                        server.DescriptionUpdated -= ServerDescriptionUpdated;
                        server.Dispose();
                    }

                    _servers.Clear();
                    _currentWaitHandle.Dispose();
                }
            }
        }

        /// <summary>
        /// Ensures that a server with the specified address is managed.
        /// </summary>
        /// <param name="dnsEndPoint">The address.</param>
        protected void EnsureServer(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            if (_servers.ContainsKey(dnsEndPoint))
            {
                return;
            }

            AddServer(dnsEndPoint);
        }

        /// <summary>
        /// Ensures that servers for the specified address are the only ones getting managed.  
        /// It will remove servers with addresses not in this list and add servers for the address
        /// that are not currently in being managed.
        /// </summary>
        /// <param name="dnsEndPoints">The addresses.</param>
        protected void EnsureServers(IEnumerable<DnsEndPoint> dnsEndPoints)
        {
            Ensure.IsNotNull("dnsEndPoints", dnsEndPoints);

            var currentAddresses = _servers.Keys.ToList();
            var needingAddition = dnsEndPoints.Except(currentAddresses).ToList();
            var needingRemoval = currentAddresses.Except(dnsEndPoints).ToList();

            foreach (var add in needingAddition)
            {
                AddServer(add);
            }

            foreach (var remove in needingRemoval)
            {
                RemoveServer(remove);
            }
        }

        /// <summary>
        /// Handles the updated description of a server.
        /// </summary>
        /// <param name="description">The description.</param>
        protected abstract void HandleUpdatedDescription(ServerDescription description);

        /// <summary>
        /// Invalidates the server with the specified dnsEndPoint.
        /// </summary>
        /// <param name="dnsEndPoint">The dnsEndPoint.</param>
        protected void InvalidateServer(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            IClusterableServer server;
            if (_servers.TryGetValue(dnsEndPoint, out server))
            {
                server.Invalidate();
            }
        }

        /// <summary>
        /// Removes the server with the specified dnsEndPoint.
        /// </summary>
        /// <param name="dnsEndPoint">The dnsEndPoint.</param>
        protected void RemoveServer(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            IClusterableServer server;
            if (_servers.TryRemove(dnsEndPoint, out server))
            {
                server.DescriptionUpdated -= ServerDescriptionUpdated;
                server.Dispose();
                OnDescriptionUpdated();
            }
        }

        // private methods
        private void AddServer(DnsEndPoint dnsEndPoint)
        {
            if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Initialized)
            {
                lock (_serversLock)
                {
                    if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Initialized)
                    {
                        var server = CreateServer(dnsEndPoint);

                        if (_servers.TryAdd(dnsEndPoint, server))
                        {
                            server.DescriptionUpdated += ServerDescriptionUpdated;
                            server.Initialize();
                        }
                        else
                        {
                            server.Dispose();
                        }
                    }
                }
            }
        }

        private IClusterableServer CreateServer(DnsEndPoint dnsEndPoint)
        {
            return _serverFactory.Create(dnsEndPoint);
        }

        private void ServerDescriptionUpdated(object sender, UpdatedEventArgs<ServerDescription> e)
        {
            HandleUpdatedDescription(e.New);
            OnDescriptionUpdated();           
        }

        private void OnDescriptionUpdated()
        {
            var descriptions = _servers.Select(x => x.Value.Description).ToList();
            var newDescription = new MultiServerClusterDescription(_type, descriptions);
            Interlocked.Exchange(ref _currentDescription, newDescription);
            var old = Interlocked.Exchange(ref _currentWaitHandle, new ManualResetEventSlim());
            old.Set();
        }

        private void ThrowIfDisposed()
        {
            if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Unitialized)
            {
                throw new InvalidOperationException("MultiServerCluster must be initialized.");
            }
        }

        private class State
        {
            public const int Unitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }
    }
}