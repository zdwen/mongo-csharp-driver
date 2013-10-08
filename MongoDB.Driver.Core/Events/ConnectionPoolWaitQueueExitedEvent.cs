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
    /// Occurs when a thread exits wait queue in a connection pool.
    /// </summary>
    public class ConnectionPoolWaitQueueExitedEvent
    {
        // private fields
        private readonly string _connectionPoolId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolWaitQueueExitedEvent" /> class.
        /// </summary>
        /// <param name="connectionPoolId">The connection pool identifier.</param>
        public ConnectionPoolWaitQueueExitedEvent(string connectionPoolId)
        {
            _connectionPoolId = connectionPoolId;
        }

        /// <summary>
        /// Gets the connection pool identifier.
        /// </summary>
        public string ConnectionPoolId
        {
            get { return _connectionPoolId; }
        }
    }
}