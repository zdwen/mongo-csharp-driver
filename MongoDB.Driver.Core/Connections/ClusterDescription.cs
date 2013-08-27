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
using System.Text;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Description of a cluster.
    /// </summary>
    public class ClusterDescription
    {
        // private fields
        private readonly List<ServerDescription> _servers;
        private readonly ClusterType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        public ClusterDescription()
            : this(ClusterType.Unknown, Enumerable.Empty<ServerDescription>())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public ClusterDescription(ClusterType type)
            : this(type, Enumerable.Empty<ServerDescription>())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterDescription" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="servers">The servers.</param>
        public ClusterDescription(ClusterType type, IEnumerable<ServerDescription> servers)
        {
            Ensure.IsNotNull("servers", servers);

            _servers = servers.ToList();
            _type = type;
        }

        // public properties
        /// <summary>
        /// Gets the server count.
        /// </summary>
        public int ServerCount
        {
            get { return _servers.Count; }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        public IEnumerable<ServerDescription> Servers
        {
            get { return _servers; }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public ClusterType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{{ Type: '{0}', Servers: [{1}] }}",
                _type,
                string.Join(", ", _servers.Select(x => x.ToString())));
        }

        // private static methods
        /// <summary>
        /// Deduces the type of the cluster from the provided server.
        /// </summary>
        /// <param name="type">The server.</param>
        /// <returns>The cluster type.</returns>
        public static ClusterType DeduceClusterType(ServerType type)
        {
            if (type.IsReplicaSetMember())
            {
                return ClusterType.ReplicaSet;
            }
            else if (type == ServerType.ShardRouter)
            {
                return ClusterType.Sharded;
            }
            else if (type == ServerType.StandAlone)
            {
                return ClusterType.StandAlone;
            }

            return ClusterType.Unknown;
        }
    }
}