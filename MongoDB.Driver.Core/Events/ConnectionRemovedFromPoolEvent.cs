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

using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a <see cref="MongoDB.Driver.Core.Connections.IConnection"/> 
    /// is removed from a connection pool.
    /// </summary>
    public class ConnectionRemovedFromPoolEvent
    {
        // private fields
        private readonly IConnection _connection;
        private readonly IConnectionPool _connectionPool;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionRemovedFromPoolEvent" /> class.
        /// </summary>
        /// <param name="connectionPool">The connection pool.</param>
        /// <param name="connection">The connection.</param>
        public ConnectionRemovedFromPoolEvent(IConnectionPool connectionPool, IConnection connection)
        {
            _connectionPool = connectionPool;
            _connection = connection;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Gets the connection pool.
        /// </summary>
        public IConnectionPool ConnectionPool
        {
            get { return _connectionPool; }
        }
    }
}