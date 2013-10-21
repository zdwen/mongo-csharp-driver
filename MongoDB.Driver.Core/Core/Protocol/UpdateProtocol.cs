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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents the update protocol.
    /// </summary>
    public sealed class UpdateProtocol : WriteProtocolBase<WriteConcernResult>
    {
        // private fields
        private readonly bool _checkUpdateDocument;
        private readonly UpdateFlags _flags;
        private readonly BsonDocument _query;
        private readonly BsonDocument _update;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProtocol" /> class.
        /// </summary>
        /// <param name="checkUpdateDocument">if set to <c>true</c> [check update document].</param>
        /// <param name="collection">The collection.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="query">The query.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="update">The update.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public UpdateProtocol(bool checkUpdateDocument,
            CollectionNamespace collection,
            UpdateFlags flags,
            BsonDocument query,
            BsonBinaryReaderSettings readerSettings,
            BsonDocument update,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
            : base(collection, readerSettings, writeConcern, writerSettings)
        {
            Ensure.IsNotNull("query", query);
            Ensure.IsNotNull("update", update);

            _checkUpdateDocument = true;
            _flags = flags;
            _query = query;
            _update = update;
        }

        // public methods
        /// <summary>
        /// Executes the Update operation.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A WriteConcern result (or null if WriteConcern was not enabled).</returns>
        public override WriteConcernResult Execute(IChannel channel)
        {
            var updateMessage = new UpdateMessage(
                Collection,
                _query,
                _update,
                _flags,
                _checkUpdateDocument,
                WriterSettings);

            SendPacketWithWriteConcernResult sendMessageResult;
            using (var packet = new BufferedRequestPacket())
            {
                packet.AddMessage(updateMessage);
                sendMessageResult = SendPacketWithWriteConcern(channel, packet, WriteConcern);
            }

            return ReadWriteConcernResult(channel, sendMessageResult);
        }
    }
}