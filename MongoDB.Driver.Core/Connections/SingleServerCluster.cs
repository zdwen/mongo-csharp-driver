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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Manages a single <see cref="IServer"/>.
    /// </summary>
    internal sealed class SingleServerCluster : Cluster
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;

        // private fields
        private readonly ClusterType _clusterType;
        private readonly string _replicaSetName;
        private readonly IClusterableServer _server;
        private readonly StateHelper _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleServerCluster" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="serverFactory">The server factory.</param>
        public SingleServerCluster(ClusterSettings settings, IClusterableServerFactory serverFactory)
            : base(serverFactory)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsEqualTo("settings.Hosts.Count", settings.Hosts.Count(), 1);
            Ensure.IsNotNull("serverFactory", serverFactory);

            _clusterType = settings.Type;
            _replicaSetName = settings.ReplicaSetName;
            _state = new StateHelper(State.Unitialized);

            _server = CreateServer(settings.Hosts.Single());
            PublishServerDescription(_server.Description);

            __trace.TraceVerbose("{0}: {1}", this, settings);
        }

        // public methods
        /// <summary>
        /// Initializes the cluster.
        /// </summary>
        public override void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Unitialized, State.Initialized))
            {
                __trace.TraceInformation("{0}: initialized.", this);
                _server.DescriptionChanged += ServerDescriptionChanged;
                _server.Initialize();
            }
            base.Initialize();
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
                _server.DescriptionChanged -= ServerDescriptionChanged;
                _server.Dispose();
                __trace.TraceInformation("{0}: closed.", this);
            }
            base.Dispose();
        }

        /// <summary>
        /// Tries to get the server from the description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="server">The server.</param>
        /// <returns>
        ///   <c>true</c> if the server was found; otherwise <c>false</c>.
        /// </returns>
        protected override bool TryGetServer(ServerDescription description, out IServer server)
        {
            if (_server.Description.DnsEndPoint == description.DnsEndPoint)
            {
                server = _server;
                return true;
            }

            server = null;
            return false;
        }

        // private methods
        private void PublishServerDescription(ServerDescription serverDescription)
        {
            ClusterType clusterType;
            IEnumerable<ServerDescription> serverDescriptions;
            if (serverDescription == null)
            {
                clusterType = _clusterType;
                serverDescriptions = Enumerable.Empty<ServerDescription>();
            }
            else
            {
                clusterType = ClusterDescription.DeduceClusterType(serverDescription.Type);
                serverDescriptions = new[] { serverDescription };
            }
            var description = new ClusterDescription(clusterType, serverDescriptions);
            UpdateDescription(description);
        }
        
        private void ServerDescriptionChanged(object sender, ChangedEventArgs<ServerDescription> e)
        {
            var descriptionToPublish = e.NewValue;
            if(e.NewValue.Status == ServerStatus.Connected)
            {
                var clusterType = ClusterDescription.DeduceClusterType(e.NewValue.Type);
                if (_clusterType != ClusterType.Unknown && _clusterType != clusterType)
                {
                    descriptionToPublish = null;
                }
                else if (_clusterType == ClusterType.ReplicaSet && !string.IsNullOrEmpty(_replicaSetName))
                {
                    if (_replicaSetName != e.NewValue.ReplicaSetInfo.Name)
                    {
                        descriptionToPublish = null;
                    }
                }
            }
            PublishServerDescription(descriptionToPublish);
        }

        private void ThrowIfDisposed()
        {
            if(_state.Current == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (_state.Current == State.Unitialized)
            {
                throw new InvalidOperationException("SingleServerCluster must be initialized.");
            }
        }

        private static class State
        {
            public const int Unitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }
    }
}
