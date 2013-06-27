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
    /// Occurs when a connection has been opened.
    /// </summary>
    public class ConnectionOpenedEvent
    {
        // private fields
        private readonly IConnection _connection;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionOpenedEvent" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public ConnectionOpenedEvent(IConnection connection)
        {
            _connection = connection;
        }

        // public properties
        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IConnection Connection
        {
            get { return _connection; }
        }
    }
}