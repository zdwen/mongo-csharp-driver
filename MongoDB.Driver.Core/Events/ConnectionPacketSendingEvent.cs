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
        private readonly IConnection _connection;
        private readonly IRequestPacket _packet;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPacketSendingEvent" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="packet">The packet.</param>
        public ConnectionPacketSendingEvent(IConnection connection, IRequestPacket packet)
        {
            _connection = connection;
            _packet = packet;
        }

        // public properties
        /// <summary>
        /// Gets the connection.
        /// </summary>
        public IConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Gets the packet.
        /// </summary>
        public IRequestPacket Packet
        {
            get { return _packet; }
        }
    }
}