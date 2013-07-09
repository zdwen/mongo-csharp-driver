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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionPoolFactory : IConnectionPoolFactory
    {
        // private fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly IEventPublisher _events;
        private readonly ConnectionPoolSettings _settings;
        private readonly TraceManager _traceManager;

        // constructors
        public ConnectionPoolFactory(ConnectionPoolSettings settings, IConnectionFactory connectionFactory, IEventPublisher events, TraceManager traceManager)
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
        /// Creates a connection pool for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A connection pool.</returns>
        public IConnectionPool Create(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("address", dnsEndPoint);

            return new ConnectionPool(_settings, dnsEndPoint, _connectionFactory, _events, _traceManager);
        }
    }
}
