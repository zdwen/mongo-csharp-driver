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
    /// Occurs when a packet is about to be sent.
    /// </summary>
    public class ConnectionPacketSendingEvent
    {
        // private fields
        private readonly string _connectionId;
        private readonly int _requestId;
        private readonly int _size;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPacketSendingEvent" /> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="size">The size.</param>
        public ConnectionPacketSendingEvent(string connectionId, int requestId, int size)
        {
            _connectionId = connectionId;
            _requestId = requestId;
            _size = size;
        }

        // public properties
        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public string ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the request identifier.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
        }

        /// <summary>
        /// Gets the size.
        /// </summary>
        public int Size
        {
            get { return _size; }
        }
    }
}