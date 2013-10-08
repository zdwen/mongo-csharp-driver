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
    /// Creates a <see cref="ConnectionPool"/>.
    /// </summary>
    internal class ConnectionPoolFactory : IConnectionPoolFactory
    {
        // private fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly IEventPublisher _events;
        private readonly ConnectionPoolSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        public ConnectionPoolFactory(ConnectionPoolSettings settings, IConnectionFactory connectionFactory, IEventPublisher events)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);

            _settings = settings;
            _connectionFactory = connectionFactory;
            _events = events;
        }

        // public methods
        /// <summary>
        /// Creates a connection pool for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A connection pool.</returns>
        public IConnectionPool Create(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("address", dnsEndPoint);

            return new ConnectionPool(_settings, dnsEndPoint, _connectionFactory, _events);
        }
    }
}
