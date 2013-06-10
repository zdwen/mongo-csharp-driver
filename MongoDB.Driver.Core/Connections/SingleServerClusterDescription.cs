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

using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Describes a <see cref="ICluster"/> with a single server.
    /// </summary>
    public sealed class SingleServerClusterDescription : ClusterDescription
    {
        // private fields
        private readonly ServerDescription _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleServerClusterDescription" /> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public SingleServerClusterDescription(ServerDescription server)
            : base(ClusterDescriptionType.Single)
        {
            Ensure.IsNotNull("server", server);

            _server = server;
        }

        // public properties
        /// <summary>
        /// Gets the server.
        /// </summary>
        public ServerDescription Server
        {
            get { return _server; }
        }
    }
}