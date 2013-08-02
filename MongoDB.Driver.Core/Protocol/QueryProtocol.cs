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
    /// Represents the query protocol.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class QueryProtocol<TDocument> : IProtocol<CursorResult<TDocument>>
    {
        // private fields
        private readonly CollectionNamespace _collection;
        private readonly object _fields;
        private readonly QueryFlags _flags;
        private readonly int _numberToReturn;
        private readonly object _query;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly IBsonSerializer _serializer;
        private readonly IBsonSerializationOptions _serializationOptions;
        private readonly int _skip;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryProtocol{TDocument}" /> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="numberToReturn">The number to return.</param>
        /// <param name="query">The query.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public QueryProtocol(CollectionNamespace collection,
            object fields,
            QueryFlags flags,
            int numberToReturn,
            object query,
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializer serializer,
            IBsonSerializationOptions serializationOptions,
            int skip,
            BsonBinaryWriterSettings writerSettings)
        {
            Ensure.IsNotNull("collection", collection);
            Ensure.IsNotNull("query", query);
            Ensure.IsNotNull("readerSettings", readerSettings);
            Ensure.IsNotNull("serializer", serializer);
            Ensure.IsNotNull("writerSettings", writerSettings);
            // NOTE: fields can be null

            _collection = collection;
            _fields = fields;
            _flags = flags;
            _numberToReturn = numberToReturn;
            _query = query;
            _readerSettings = readerSettings;
            _serializer = serializer;
            _serializationOptions = serializationOptions;
            _skip = skip;
            _writerSettings = writerSettings;
        }

        // public methods
        /// <summary>
        /// Executes the query protocol.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The reply message.</returns>
        public CursorResult<TDocument> Execute(IChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            // since we're going to block anyway when a tailable cursor is temporarily out of data
            // we might as well do it as efficiently as possible
            var flags = _flags;
            if ((flags & QueryFlags.TailableCursor) != 0)
            {
                flags |= QueryFlags.AwaitData;
            }

            var queryMessage = new QueryMessage(
                _collection,
                _query,
                _flags,
                _skip,
                _numberToReturn,
                _fields,
                _writerSettings);

            using (var packet = new BufferedRequestPacket())
            {
                packet.AddMessage(queryMessage);
                channel.Send(packet);
            }

            var receiveArgs = new ChannelReceiveArgs(queryMessage.RequestId);
            using (var reply = channel.Receive(receiveArgs))
            {
                reply.ThrowIfQueryFailureFlagIsSet();

                var docs = reply.DeserializeDocuments<TDocument>(_serializer, _serializationOptions, _readerSettings);
                return new CursorResult<TDocument>(reply.CursorId, docs.ToList());
            }
        }
    }
}