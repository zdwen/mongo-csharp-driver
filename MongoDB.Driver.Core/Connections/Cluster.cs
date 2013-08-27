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
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Base class for an <see cref="ICluster"/>.
    /// </summary>
    internal abstract class Cluster : ClusterBase
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;

        // private fields
        private readonly IClusterableServerFactory _serverFactory;
        private readonly StateHelper _state;
        private readonly string _toStringDescription;
        private ManualResetEventSlim _selectServerEvent;
        private volatile ClusterDescription _description;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Cluster" /> class.
        /// </summary>
        /// <param name="serverFactory">The server factory.</param>
        protected Cluster(IClusterableServerFactory serverFactory)
        {
            Ensure.IsNotNull("serverFactory", serverFactory);

            _serverFactory = serverFactory;
            _state = new StateHelper(State.Uninitialized);
            _selectServerEvent = new ManualResetEventSlim();
            _toStringDescription = string.Format("cluster#{0}", IdGenerator<ICluster>.GetNextId());
        }

        // public properties
        /// <summary>
        /// Gets the description.
        /// </summary>
        public override ClusterDescription Description
        {
            get { return _description; }
        }

        // public methods
        /// <summary>
        /// Initializes the cluster.
        /// </summary>
        public override void Initialize()
        {
            ThrowIfDisposed();
            _state.TryChange(State.Uninitialized, State.Initialized);
        }

        /// <summary>
        /// Selects a server using the specified selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A server if one has matched; otherwise <c>null</c>.</returns>
        public sealed override IServer SelectServer(IServerSelector selector, TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            Ensure.IsNotNull("selector", selector);

            ThrowIfUninitialized();
            ThrowIfDisposed();

            var timeoutAt = DateTime.UtcNow.Add(timeout);
            TimeSpan remaining;
            ManualResetEventSlim currentWaitHandle;
            do
            {
                currentWaitHandle = Interlocked.CompareExchange(ref _selectServerEvent, null, null);
                var descriptions = _description.Servers;

                var selectedDescriptions = selector.SelectServers(descriptions).ToList();
                while (selectedDescriptions.Any())
                {
                    var random = selectedDescriptions.RandomOrDefault();
                    IServer selectedServer;
                    if (TryGetServer(random, out selectedServer))
                    {
                        return selectedServer;
                    }

                    selectedDescriptions.Remove(random);
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

            throw new MongoDriverException(string.Format("Unable to find a server matching '{0}'.", selector));
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public sealed override string ToString()
        {
            return _toStringDescription;
        }

        // protected methods
        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A clusterable server.</returns>
        protected IClusterableServer CreateServer(DnsEndPoint dnsEndPoint)
        {
            return _serverFactory.Create(dnsEndPoint);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed) && disposing)
            {
                _selectServerEvent.Dispose();
            }
        }

        /// <summary>
        /// Tries to get the server from the description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="server">The server.</param>
        /// <returns><c>true</c> if the server was found; otherwise <c>false</c>.</returns>
        protected abstract bool TryGetServer(ServerDescription description, out IServer server);

        /// <summary>
        /// Updates the cluster description.
        /// </summary>
        /// <param name="description">The description.</param>
        protected void UpdateDescription(ClusterDescription description)
        {
            __trace.TraceInformation("{0}: description updated: {1}", _toStringDescription, description);
            _description = description;
            var old = Interlocked.Exchange(ref _selectServerEvent, new ManualResetEventSlim());
            old.Set();
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_state.Current == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (_state.Current == State.Uninitialized)
            {
                throw new InvalidOperationException(string.Format("{0} is unitialized.", GetType().Name));
            }
        }

        // nested classes
        private static class State
        {
            public const int Uninitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }
    }
}
