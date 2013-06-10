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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Description of a MultiServerManager.
    /// </summary>
    public sealed class MultiServerClusterDescription : ClusterDescription
    {
        // private fields
        private readonly ReadOnlyCollection<ServerDescription> _servers;
        private readonly MultiServerClusterType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiServerClusterDescription" /> class.
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="servers">The servers.</param>
        public MultiServerClusterDescription(MultiServerClusterType connectionType, IEnumerable<ServerDescription> servers)
            : base(ClusterDescriptionType.Multi)
        {
            Ensure.IsNotNull("servers", servers);

            _servers = servers.ToList().AsReadOnly();
            _type = connectionType;
        }

        // public properties
        /// <summary>
        /// Gets the type of the multiple manager.
        /// </summary>
        public MultiServerClusterType MultipleManagerType
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the number of servers.
        /// </summary>
        public int Count
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
    }
}