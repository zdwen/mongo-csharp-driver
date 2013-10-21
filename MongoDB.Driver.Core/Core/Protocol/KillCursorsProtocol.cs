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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents the kill cursors protocol.
    /// </summary>
    public sealed class KillCursorsProtocol
    {
        // private fields
        private readonly long[] _cursorIds;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KillCursorsProtocol" /> class.
        /// </summary>
        /// <param name="cursorIds">The cursor ids.</param>
        public KillCursorsProtocol(IEnumerable<long> cursorIds)
        {
            Ensure.IsNotNull("cursorIds", cursorIds);

            _cursorIds = cursorIds.ToArray();
        }

        // public methods
        /// <summary>
        /// Executes the kill cursors protocol using the channel provider.
        /// </summary>
        /// <param name="channel">The channel.</param>
        public void Execute(IChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            if (_cursorIds == null || _cursorIds.Length == 0)
            {
                return;
            }

            var message = new KillCursorsMessage(_cursorIds);

            using (var packet = new BufferedRequestPacket())
            {
                packet.AddMessage(message);
                channel.Send(packet);
            }
        }
    }
}