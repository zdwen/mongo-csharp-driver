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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a connection pool is opened.
    /// </summary>
    public class ConnectionPoolOpenedEvent
    {
        // private fields
        private readonly DnsEndPoint _address;
        private readonly string _connectionPoolId;
        private readonly ConnectionPoolSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolOpenedEvent" /> class.
        /// </summary>
        /// <param name="connectionPoolId">The connection pool identifier.</param>
        /// <param name="address">The address.</param>
        /// <param name="settings">The settings.</param>
        public ConnectionPoolOpenedEvent(string connectionPoolId, DnsEndPoint address, ConnectionPoolSettings settings)
        {
            _connectionPoolId = connectionPoolId;
            _address = address;
            _settings = settings;
        }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public DnsEndPoint Address
        {
            get { return _address; }
        }

        /// <summary>
        /// Gets the connection pool identifier.
        /// </summary>
        public string ConnectionPoolId
        {
            get { return _connectionPoolId; }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public ConnectionPoolSettings Settings
        {
            get { return _settings; }
        }
    }
}