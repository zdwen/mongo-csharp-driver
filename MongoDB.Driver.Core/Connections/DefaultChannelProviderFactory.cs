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
    /// A factory for <see cref="DefaultChannelProvider"/>s.
    /// </summary>
    public sealed class DefaultChannelProviderFactory : IChannelProviderFactory
    {
        // private fields
        private readonly DefaultChannelProviderSettings _settings;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IEventPublisher _events;
        private readonly TraceManager _traceManager;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultChannelProviderFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        /// <param name="traceManager">The trace manager.</param>
        public DefaultChannelProviderFactory(DefaultChannelProviderSettings settings, IConnectionFactory connectionFactory, IEventPublisher events, TraceManager traceManager)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);
            Ensure.IsNotNull("traceManager", traceManager);

            _settings = settings;
            _connectionFactory = connectionFactory;
            _events = events;
            _traceManager = traceManager;
        }

        // public methods
        /// <summary>
        /// Creates a channel provider for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A channel provider.</returns>
        public IChannelProvider Create(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("address", dnsEndPoint);

            return new DefaultChannelProvider(_settings, dnsEndPoint, _connectionFactory, _events, _traceManager);
        }
    }
}
