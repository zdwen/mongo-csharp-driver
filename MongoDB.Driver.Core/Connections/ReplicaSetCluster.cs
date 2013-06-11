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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Manages multiple <see cref="IServer"/>s for a replica set.
    /// </summary>
    public sealed class ReplicaSetCluster : MultiServerCluster
    {
        // private fields
        private string _replicaSetName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetCluster" /> class.
        /// </summary>
        /// <param name="replicaSetName">Name of the replica set.</param>
        /// <param name="addresses">The addresses.</param>
        /// <param name="serverFactory">The server factory.</param>
        public ReplicaSetCluster(ReplicaSetClusterSettings settings, IEnumerable<DnsEndPoint> dnsEndPoints, IClusterableServerFactory serverFactory)
            : base(MultiServerClusterType.ReplicaSet, dnsEndPoints, serverFactory)
        {
            _replicaSetName = settings.ReplicaSetName;
        }

        // protected methods
        /// <summary>
        /// Handles the updated description.
        /// </summary>
        /// <param name="server">The server.</param>
        protected override void HandleUpdatedDescription(ServerDescription server)
        {
            if (server.Status != ServerStatus.Connected)
            {
                return;
            }

            if (!server.Type.IsReplicaSetMember())
            {
                RemoveServer(server.DnsEndPoint);
                return;
            }

            var currentReplicaSetName = Interlocked.CompareExchange(ref _replicaSetName, server.ReplicaSetInfo.Name, null);

            // if the server has a different replica set name than the one we are connected to,
            // get rid of it.
            if (currentReplicaSetName != null && currentReplicaSetName != server.ReplicaSetInfo.Name)
            {
                RemoveServer(server.DnsEndPoint);
                return;
            }

            // make sure we know about these and only these servers.
            EnsureServers(server.ReplicaSetInfo.Members);

            // if the members do not contain the server we are processing, remove it from the mix.
            if (!server.ReplicaSetInfo.Members.Contains(server.DnsEndPoint))
            {
                RemoveServer(server.DnsEndPoint);
                return;
            }

            if (server.Type == ServerType.ReplicaSetPrimary)
            {
                // we want to take the current primary(ies)and invalidate it so we don't have 2 primaries.
                var currentPrimaries = ((MultiServerClusterDescription)Description).Servers.Where(x => x.Type == ServerType.ReplicaSetPrimary && !x.DnsEndPoint.Equals(server.DnsEndPoint)).ToList();
                currentPrimaries.ForEach(x => InvalidateServer(x.DnsEndPoint));
            }
        }
    }
}