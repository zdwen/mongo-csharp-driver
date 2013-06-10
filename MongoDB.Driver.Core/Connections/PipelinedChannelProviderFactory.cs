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

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Provides channels used in a pipelined manner.
    /// </summary>
    public class PipelinedChannelProviderFactory : IChannelProviderFactory
    {
        // private fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _numberOfConcurrentConnections;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelinedChannelProviderFactory" /> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="numberOfConcurrentConnections">The number of concurrent connections.</param>
        public PipelinedChannelProviderFactory(IConnectionFactory connectionFactory, int numberOfConcurrentConnections)
        {
            _connectionFactory = connectionFactory;
            _numberOfConcurrentConnections = numberOfConcurrentConnections;
        }

        // public methods
        /// <summary>
        /// Creates a channel provider for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A channel provider.</returns>
        public IChannelProvider Create(DnsEndPoint dnsEndPoint)
        {
            return new PipelinedChannelProvider(dnsEndPoint, _connectionFactory, _numberOfConcurrentConnections);
        }
    }
}