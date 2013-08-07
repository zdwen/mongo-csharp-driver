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

using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents the get more protocol.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class GetMoreProtocol<TDocument> : IProtocol<CursorBatch<TDocument>>
    {
        // private fields
        private readonly CollectionNamespace _collection;
        private readonly long _cursorId;
        private readonly int _numberToReturn;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly IBsonSerializer _serializer;
        private readonly IBsonSerializationOptions _serializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMoreProtocol{TDocument}" /> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="cursorId">The cursor id.</param>
        /// <param name="numberToReturn">The number to return.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        public GetMoreProtocol(CollectionNamespace collection,
            long cursorId,
            int numberToReturn,
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializer serializer,
            IBsonSerializationOptions serializationOptions)
        {
            Ensure.IsNotNull("collection", collection);
            Ensure.IsNotNull("readerSettings", readerSettings);
            Ensure.IsNotNull("serializer", serializer);

            _collection = collection;
            _cursorId = cursorId;
            _numberToReturn = numberToReturn;
            _readerSettings = readerSettings;
            _serializer = serializer;
            _serializationOptions = serializationOptions;
        }

        // public methods
        /// <summary>
        /// Executes the get more protocol.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The reply message.</returns>
        public CursorBatch<TDocument> Execute(IChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            var message = new GetMoreMessage(_collection, _cursorId, _numberToReturn);

            using (var packet = new BufferedRequestPacket())
            {
                packet.AddMessage(message);
                channel.Send(packet);
            }

            var receiveArgs = new ChannelReceiveArgs(message.RequestId);
            using (var reply = channel.Receive(receiveArgs))
            {
                reply.ThrowIfQueryFailureFlagIsSet();

                var docs = reply.DeserializeDocuments<TDocument>(_serializer, _serializationOptions, _readerSettings);
                return new CursorBatch<TDocument>(reply.CursorId, docs.ToList());
            }
        }
    }
}