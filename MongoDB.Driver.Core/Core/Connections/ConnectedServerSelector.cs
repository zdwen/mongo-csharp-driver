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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Selects only connected servers.
    /// </summary>
    public class ConnectedServerSelector : IServerSelector
    {
        /// <summary>
        /// The default instance.
        /// </summary>
        public static readonly ConnectedServerSelector Instance = new ConnectedServerSelector();

        /// <summary>
        /// Selects a server from the provided servers.
        /// </summary>
        /// <param name="servers">The servers.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        public IEnumerable<ServerDescription> SelectServers(IEnumerable<ServerDescription> servers)
        {
            Ensure.IsNotNull("servers", servers);

            return SelectServersFromConnectedServers(servers.Where(x => x.Status == ServerStatus.Connected));
        }

        /// <summary>
        /// Selects the servers from the connected servers.
        /// </summary>
        /// <param name="connectedServers">The connected servers.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        protected virtual IEnumerable<ServerDescription> SelectServersFromConnectedServers(IEnumerable<ServerDescription> connectedServers)
        {
            return connectedServers;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Connected";
        }
    }
}