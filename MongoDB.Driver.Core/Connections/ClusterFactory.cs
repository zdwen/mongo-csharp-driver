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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Creates <see cref="ICluster"/>s.
    /// </summary>
    internal class ClusterFactory : IClusterFactory
    {
        // private fields
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ClusterSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="serverFactory">The server factory.</param>
        public ClusterFactory(ClusterSettings settings, IClusterableServerFactory serverFactory)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("serverFactory", serverFactory);

            _settings = settings;
            _serverFactory = serverFactory;
        }

        /// <summary>
        /// Creates a cluster with the specified settings.
        /// </summary>
        /// <returns>An <see cref="ICluster" />.</returns>
        public ICluster Create()
        {
            switch(_settings.ConnectionMode)
            {
                case ClusterConnectionMode.Single:
                    return new SingleServerCluster(_settings, _serverFactory);
                case ClusterConnectionMode.Multiple:
                    return new MultiServerCluster(_settings, _serverFactory);
            }

            throw new NotSupportedException(string.Format("An unsupported ClusterConnectionMode of {0} was provided.", _settings.ConnectionMode));
        }
    }
}