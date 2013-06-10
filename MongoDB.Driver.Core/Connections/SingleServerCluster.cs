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
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Manages a single server.
    /// </summary>
    public sealed class SingleServerCluster : ClusterBase
    {
        // private fields
        private readonly IClusterableServer _server;
        private ManualResetEventSlim _currentWaitHandle;
        private int _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleServerCluster" /> class.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="serverFactory">The server factory.</param>
        public SingleServerCluster(DnsEndPoint dnsEndPoint, IClusterableServerFactory serverFactory)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("factory", serverFactory);

            _currentWaitHandle = new ManualResetEventSlim();
            _server = serverFactory.Create(dnsEndPoint);
            _server.DescriptionUpdated += ServerDescriptionUpdated;
        }

        // public properties
        /// <summary>
        /// Gets the description.
        /// </summary>
        public override ClusterDescription Description
        {
            get { return new SingleServerClusterDescription(_server.Description); }
        }

        // public methods
        /// <summary>
        /// Initializes the cluster.
        /// </summary>
        public override void Initialize()
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _state, State.Initialized, State.Unitialized) == State.Unitialized)
            {
                _server.Initialize();
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
        public override IServer SelectServer(IServerSelector selector, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull("selector", selector);

            ThrowIfUninitialized();
            ThrowIfDisposed();

            var timeoutAt = DateTime.UtcNow.Add(timeout);
            TimeSpan remaining;
            ManualResetEventSlim currentWaitHandle;
            ServerDescription description;
            do
            {
                currentWaitHandle = Interlocked.CompareExchange(ref _currentWaitHandle, null, null);
                description = _server.Description;

                var selectedDescription = selector.SelectServer(new[] { description });
                if (selectedDescription != null)
                {
                    return new DisposalProtectedServer(_server);
                }

                if (description.Status != ServerStatus.Connecting)
                {
                    // nothing we can do if we aren't connecting
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

            throw new MongoDriverException(string.Format("The server {0} does not match '{1}'.", description.DnsEndPoint, selector.Description));
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
                _server.DescriptionUpdated -= ServerDescriptionUpdated;
                _server.Dispose();
                _currentWaitHandle.Dispose();
            }
        }

        // private methods
        private void ServerDescriptionUpdated(object sender, UpdatedEventArgs<ServerDescription> e)
        {
            var old = Interlocked.Exchange(ref _currentWaitHandle, new ManualResetEventSlim());
            old.Set();
        }

        private void ThrowIfDisposed()
        {
            if(Interlocked.CompareExchange(ref _state, 0, 0) == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if(Interlocked.CompareExchange(ref _state, 0, 0) == State.Unitialized)
            {
                throw new InvalidOperationException("SingleServerCluster must be initialized.");
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