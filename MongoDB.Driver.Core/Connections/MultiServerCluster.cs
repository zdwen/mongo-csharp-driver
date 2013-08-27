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
using System.Diagnostics;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Manages multiple <see cref="IServer"/>s.
    /// </summary>
    internal sealed class MultiServerCluster : Cluster
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;

        // private fields
        // Writes to _servers are secured underneath this _serversLock.
        // However, reads are free to be done without a lock.  The only access to
        // _servers which is done outside a lock should be in the constructor
        // or in TryGetServer.
        private readonly object _serversLock = new object();
        private readonly ConcurrentDictionary<DnsEndPoint, ServerDescriptionPair> _servers;
        private readonly StateHelper _state;
        private ClusterType _clusterType;
        private int _configVersion;
        private string _replicaSetName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiServerCluster" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="serverFactory">The server factory.</param>
        public MultiServerCluster(ClusterSettings settings, IClusterableServerFactory serverFactory)
            : base(serverFactory)
        {
            Ensure.IsNotNull("settings", settings);

            _replicaSetName = settings.ReplicaSetName;
            _clusterType = settings.Type;
            _configVersion = int.MinValue;
            _servers = new ConcurrentDictionary<DnsEndPoint, ServerDescriptionPair>();
            foreach (var host in settings.Hosts)
            {
                // if we fail to add one, it means the same host
                // appeared twice and it's ok to not add it in.
                var server = CreateServer(host);
                if (!_servers.TryAdd(host, new ServerDescriptionPair(server)))
                {
                    server.Dispose();
                }
            }
            _state = new StateHelper(State.Uninitialized);
            PublishClusterDescription();

            __trace.TraceVerbose("{0}: {1}", this, settings);
        }

        // public methods
        /// <summary>
        /// Initializes the cluster.
        /// </summary>
        public override void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Uninitialized, State.Initialized))
            {
                __trace.TraceInformation("{0}: initialized.", this);
                __trace.TraceInformation("{0}: type is {1}.", this, _clusterType);
                if (!string.IsNullOrEmpty(_replicaSetName))
                {
                    __trace.TraceInformation("{0}: replica set name is {1}", this, _replicaSetName);
                }
                lock (_serversLock)
                {
                    foreach (var entry in _servers)
                    {
                        entry.Value.Server.DescriptionChanged += ServerDescriptionChanged;
                        entry.Value.Server.Initialize();
                    }
                }
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
            lock (_serversLock)
            {
                if (_state.TryChange(State.Disposed) && disposing)
                {
                    foreach (var entry in _servers.Values)
                    {
                        entry.Server.DescriptionChanged -= ServerDescriptionChanged;
                        entry.Server.Dispose();
                    }

                    _servers.Clear();
                }
                __trace.TraceInformation("{0}: closed.", this);
            }
            base.Dispose();
        }

        /// <summary>
        /// Tries to get the server from the description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="server">The server.</param>
        /// <returns><c>true</c> if the server was found; otherwise <c>false</c>.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override bool TryGetServer(ServerDescription description, out IServer server)
        {
            ServerDescriptionPair entry;
            if (_servers.TryGetValue(description.DnsEndPoint, out entry))
            {
                server = entry.Server;
                return true;
            }

            server = null;
            return false;
        }

        // private methods
        private void AddServer(DnsEndPoint host)
        {
            if (!_servers.ContainsKey(host))
            {
                var server = CreateServer(host);

                if (_servers.TryAdd(host, new ServerDescriptionPair(server)))
                {
                    __trace.TraceInformation("{0}: added {1}.", this, server);
                    server.DescriptionChanged += ServerDescriptionChanged;
                    server.Initialize();
                }
                else
                {
                    server.Dispose();
                }
            }
        }

        private void EnsureServers(IEnumerable<DnsEndPoint> hosts)
        {
            var currentAddresses = _servers.Keys.ToList();
            var needingAddition = hosts.Except(currentAddresses).ToList();
            var needingRemoval = currentAddresses.Except(hosts).ToList();

            foreach (var remove in needingRemoval)
            {
                __trace.TraceVerbose("{0}: host is not in peers - {1}.", this, remove);
                RemoveServer(remove);
            }

            foreach (var add in needingAddition)
            {
                __trace.TraceVerbose("{0}: discovered new peer - {1}.", this, add);
                AddServer(add);
            }
        }

        private void HandleReplicaSetMemberChange(ChangedEventArgs<ServerDescription> e)
        {
            if (e.NewValue.Status != ServerStatus.Connected)
            {
                return;
            }

            if (!e.NewValue.Type.IsReplicaSetMember())
            {
                __trace.TraceWarning("{0}: {1} is a {2} and cannot be in a replica set.", this, e.NewValue.DnsEndPoint, e.NewValue.Type);
                RemoveServer(e.NewValue.DnsEndPoint);
                return;
            }

            if (_replicaSetName == null)
            {
                _replicaSetName = e.NewValue.ReplicaSetInfo.Name;
                __trace.TraceInformation("{0}: replica set name is {1}.", this, _replicaSetName);
            }

            if (_replicaSetName != null && _replicaSetName != e.NewValue.ReplicaSetInfo.Name)
            {
                __trace.TraceWarning("{0}: {1} is a member of a different replica set named {2}.", this, e.NewValue.DnsEndPoint, e.NewValue.ReplicaSetInfo.Name);
                RemoveServer(e.NewValue.DnsEndPoint);
                return;
            }

            if (!e.NewValue.ReplicaSetInfo.Version.HasValue || e.NewValue.ReplicaSetInfo.Version > _configVersion)
            {
                if (e.NewValue.ReplicaSetInfo.Version.HasValue)
                {
                    _configVersion = e.NewValue.ReplicaSetInfo.Version.Value;
                    __trace.TraceInformation("{0}: replica set config version set to {1}.", this, _configVersion);
                }

                EnsureServers(e.NewValue.ReplicaSetInfo.Members);
            }

            if (e.NewValue.Type == ServerType.ReplicaSetPrimary)
            {
                // we want to take the current primary(ies)and invalidate it so we don't have 2 primaries.
                var currentPrimaries = Description.Servers.Where(x => x.Type == ServerType.ReplicaSetPrimary && !x.DnsEndPoint.Equals(e.NewValue.DnsEndPoint)).ToList();
                currentPrimaries.ForEach(x => InvalidateServer(x.DnsEndPoint));
            }
        }

        private void HandleShardRouterChanged(ChangedEventArgs<ServerDescription> e)
        {
            if (e.NewValue.Status != ServerStatus.Connected)
            {
                return;
            }

            if (e.NewValue.Type != ServerType.ShardRouter)
            {
                __trace.TraceWarning("{0}: {1} is a {2} and cannot be in a sharded cluster.", this, e.NewValue.DnsEndPoint, e.NewValue.Type);
                RemoveServer(e.NewValue.DnsEndPoint);
            }
        }

        private void InvalidateServer(DnsEndPoint host)
        {
            ServerDescriptionPair entry;
            if (_servers.TryGetValue(host, out entry))
            {
                __trace.TraceInformation("{0}: invalidating {1}.", this, entry.Server);
                entry.Server.Invalidate();
                entry.CachedDescription = entry.Server.Description;
            }
        }

        private void PublishClusterDescription()
        {
            var serverDescriptions = _servers
                .Select(x => x.Value.CachedDescription)
                .ToList();

            var newDescription = new ClusterDescription(_clusterType, serverDescriptions);
            UpdateDescription(newDescription);
        }

        private void RemoveServer(DnsEndPoint host)
        {
            ServerDescriptionPair entry;
            if (_servers.TryRemove(host, out entry))
            {
                __trace.TraceInformation("{0}: removed {1}.", this, entry.Server);
                entry.Server.DescriptionChanged -= ServerDescriptionChanged;
                entry.Server.Dispose();
            }
        }

        private void ServerDescriptionChanged(object sender, ChangedEventArgs<ServerDescription> e)
        {
            lock (_serversLock)
            {
                if (_state.Current == State.Initialized)
                {
                    ServerDescriptionPair entry;
                    if (!_servers.TryGetValue(e.NewValue.DnsEndPoint, out entry))
                    {
                        return;
                    }

                    var deducedClusterType = ClusterDescription.DeduceClusterType(e.NewValue.Type);
                    if (_clusterType == ClusterType.Unknown)
                    {
                        _clusterType = deducedClusterType;
                        __trace.TraceInformation("{0}: type is {1}.", this, _clusterType);
                    }

                    switch (_clusterType)
                    {
                        case ClusterType.ReplicaSet:
                            HandleReplicaSetMemberChange(e);
                            break;
                        case ClusterType.Sharded:
                            HandleShardRouterChanged(e);
                            break;
                        case ClusterType.StandAlone:
                            RemoveServer(e.NewValue.DnsEndPoint);
                            break;
                    }

                    entry.CachedDescription = e.NewValue;
                    PublishClusterDescription();
                }
            }
        }

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
                throw new InvalidOperationException(string.Format("{0} must be initialized.", GetType().Name));
            }
        }

        // nested classes
        private class ServerDescriptionPair
        {
            public IClusterableServer Server;
            public volatile ServerDescription CachedDescription;

            public ServerDescriptionPair(IClusterableServer server)
            {
                Server = server;
                CachedDescription = server.Description;
            }
        }

        private static class State
        {
            public const int Uninitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }
    }
}
