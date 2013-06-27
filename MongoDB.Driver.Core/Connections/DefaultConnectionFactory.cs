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
    /// A factory for <see cref="DefaultConnection"/>s.
    /// </summary>
    public sealed class DefaultConnectionFactory : IConnectionFactory
    {
        // private fields
        private readonly IEventPublisher _events;
        private readonly IStreamFactory _streamFactory;
        private readonly TraceManager _traceManager;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConnectionFactory" /> class.
        /// </summary>
        /// <param name="streamFactory">The stream factory.</param>
        /// <param name="events">The events.</param>
        /// <param name="traceManager">The trace manager.</param>
        public DefaultConnectionFactory(IStreamFactory streamFactory, IEventPublisher events, TraceManager traceManager)
        {
            Ensure.IsNotNull("streamFactory", streamFactory);
            Ensure.IsNotNull("events", events);
            Ensure.IsNotNull("traceManager", traceManager);

            _streamFactory = streamFactory;
            _events = events;
            _traceManager = traceManager;
        }

        // public methods
        /// <summary>
        /// Creates a connection for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A connection.</returns>
        public IConnection Create(DnsEndPoint dnsEndPoint)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            return new DefaultConnection(dnsEndPoint, _streamFactory, _events, _traceManager);
        }
    }
}