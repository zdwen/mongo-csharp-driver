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
    /// Occurs when a message has been serialized and is about to be sent.
    /// </summary>
    public class ConnectionMessageSendingEvent
    {
        // private fields
        private readonly IConnection _connection;
        private readonly IRequestMessage _message;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionMessageSendingEvent" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="message">The message.</param>
        public ConnectionMessageSendingEvent(IConnection connection, IRequestMessage message)
        {
            _connection = connection;
            _message = message;
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
        /// Gets the message.
        /// </summary>
        public IRequestMessage Message
        {
            get { return _message; }
        }
    }
}