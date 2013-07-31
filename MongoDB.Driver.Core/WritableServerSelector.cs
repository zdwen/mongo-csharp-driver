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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Selects only servers that can execute non-queries.
    /// </summary>
    public class WritableServerSelector : ConnectedServerSelector
    {
        // public static fields
        /// <summary>
        /// The default instance.
        /// </summary>
        public new static WritableServerSelector Instance = new WritableServerSelector();

        // private fields
        private readonly LatencyLimitingServerSelector _latencySelector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WritableServerSelector" /> class.
        /// </summary>
        private WritableServerSelector()
        {
            _latencySelector = new LatencyLimitingServerSelector();
        }

        // protected methods
        /// <summary>
        /// Selects a server from the connected servers.
        /// </summary>
        /// <param name="connectedServers">The connected servers.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        protected override IEnumerable<ServerDescription> SelectServersFromConnectedServers(IEnumerable<ServerDescription> connectedServers)
        {
            return _latencySelector.SelectServers(connectedServers.Where(x => x.Type.CanWrite()));
        }
    }
}