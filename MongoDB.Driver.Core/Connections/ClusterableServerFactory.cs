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

using System.Net;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A factory for <see cref="ClusterableServer"/>s.
    /// </summary>
    internal sealed class ClusterableServerFactory : IClusterableServerFactory
    {
        // private fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly IChannelProviderFactory _channelProviderFactory;
        private readonly IEventPublisher _events;
        private readonly ClusterableServerSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterableServerFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="channelProviderFactory">The channel provider factory.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        public ClusterableServerFactory(ClusterableServerSettings settings, IChannelProviderFactory channelProviderFactory, IConnectionFactory connectionFactory, IEventPublisher events)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("connectionPoolFactory", channelProviderFactory);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);

            _settings = settings;
            _channelProviderFactory = channelProviderFactory;
            _connectionFactory = connectionFactory;
            _events = events;
        }

        // public methods
        /// <summary>
        /// Creates a server for the specified DNS end point.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A server.</returns>
        public IClusterableServer Create(ClusterId clusterId,DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("clusterId", clusterId);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            return new ClusterableServer(clusterId, _settings, dnsEndPoint, _channelProviderFactory, _connectionFactory, _events);
        }
    }
}
