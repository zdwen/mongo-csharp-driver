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
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Protocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a message has been received.
    /// </summary>
    public class ConnectionMessageReceivedEvent
    {
        // private fields
        private readonly string _connectionId;
        private readonly int _responseTo;
        private readonly int _size;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionMessageReceivedEvent" /> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="responseTo">The response to.</param>
        /// <param name="size">The size.</param>
        public ConnectionMessageReceivedEvent(string connectionId, int responseTo, int size)
        {
            _connectionId = connectionId;
            _responseTo = responseTo;
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
        /// Gets the id of the request to which this message is a response.
        /// </summary>
        public int ResponseTo
        {
            get { return _responseTo; }
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