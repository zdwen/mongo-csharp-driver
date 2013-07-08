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

using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Query operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class QueryOperation<TDocument> : ReadOperation
    {
        // private fields
        private readonly int _batchSize;
        private readonly object _fields;
        private readonly QueryFlags _flags;
        private readonly int _limit;
        private readonly BsonDocument _options;
        private readonly object _query;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializationOptions _serializationOptions;
        private readonly IBsonSerializer _serializer;
        private readonly int _skip;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOperation{TDocument}" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="options">The options.</param>
        /// <param name="query">The query.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="skip">The skip.</param>
        public QueryOperation(
            CollectionNamespace collectionNamespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            int batchSize,
            object fields,
            QueryFlags flags,
            int limit,
            BsonDocument options,
            object query,
            ReadPreference readPreference,
            IBsonSerializationOptions serializationOptions,
            IBsonSerializer serializer,
            int skip)
            : base(collectionNamespace, readerSettings, writerSettings)
        {
            Ensure.IsNotNull("serializer", serializer);

            _batchSize = batchSize;
            _fields = fields;
            _flags = flags;
            _limit = limit;
            _options = options;
            _query = query;
            _readPreference = readPreference;
            _serializationOptions = serializationOptions;
            _serializer = serializer;
            _skip = skip;

            // since we're going to block anyway when a tailable cursor is temporarily out of data
            // we might as well do it as efficiently as possible
            if ((_flags & QueryFlags.TailableCursor) != 0)
            {
                _flags |= QueryFlags.AwaitData;
            }
        }

        // public methods
        /// <summary>
        /// Executes the Query operation.
        /// </summary>
        /// <param name="channelProvider">The channel provider.</param>
        /// <returns>An enumerator to enumerate over the results.</returns>
        public IEnumerator<TDocument> Execute(ICursorChannelProvider channelProvider)
        {
            Ensure.IsNotNull("channelProvider", channelProvider);

            var count = 0;
            var limit = (_limit >= 0) ? _limit : -_limit;

            foreach (var document in DeserializeDocuments(channelProvider))
            {
                if (limit != 0 && count == limit)
                {
                    yield break;
                }
                yield return document;
                count++;
            }
        }

        // private methods
        private IEnumerable<TDocument> DeserializeDocuments(ICursorChannelProvider channelProvider)
        {
            var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);

            long cursorId = 0;
            try
            {
                using (var reply = GetFirstBatch(channelProvider))
                {
                    cursorId = reply.CursorId;
                    foreach (var document in reply.DeserializeDocuments<TDocument>(_serializer, _serializationOptions, readerSettings))
                    {
                        yield return document;
                    }
                }

                while (cursorId != 0)
                {
                    ReplyFlags replyFlags = 0;
                    using (var reply = GetNextBatch(channelProvider, cursorId))
                    {
                        cursorId = reply.CursorId;
                        replyFlags = reply.Flags;
                        foreach (var document in reply.DeserializeDocuments<TDocument>(_serializer, _serializationOptions, readerSettings))
                        {
                            yield return document;
                        }
                    }

                    if (cursorId != 0 && (_flags & QueryFlags.TailableCursor) != 0)
                    {
                        if ((replyFlags & ReplyFlags.AwaitCapable) == 0)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        }
                    }
                }
            }
            finally
            {
                if (cursorId != 0)
                {
                    KillCursor(channelProvider, cursorId);
                }
            }
        }

        private ReplyMessage GetFirstBatch(ICursorChannelProvider channelProvider)
        {
            // some of these weird conditions are necessary to get commands to run correctly
            // specifically numberToReturn has to be 1 or -1 for commands
            int numberToReturn;
            if (_limit < 0)
            {
                numberToReturn = _limit;
            }
            else if (_limit == 0)
            {
                numberToReturn = _batchSize;
            }
            else if (_batchSize == 0)
            {
                numberToReturn = _limit;
            }
            else if (_limit < _batchSize)
            {
                numberToReturn = _limit;
            }
            else
            {
                numberToReturn = _batchSize;
            }

            using (var channel = channelProvider.GetChannel())
            {
                var writerSettings = GetServerAdjustedWriterSettings(channel.Server);
                var wrappedQuery = WrapQuery(channel.Server, _query, _options, _readPreference);

                var queryMessage = new QueryMessage(
                    CollectionNamespace,
                    wrappedQuery,
                    _flags,
                    _skip,
                    numberToReturn,
                    _fields,
                    writerSettings);

                using(var packet = new BufferedRequestPacket())
                {
                    packet.AddMessage(queryMessage);
                    channel.Send(packet);
                }

                var receiveArgs = new ChannelReceiveArgs(queryMessage.RequestId);
                return channel.Receive(receiveArgs);
            }
        }

        private ReplyMessage GetNextBatch(ICursorChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel())
            {
                var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
                var getMoreMessage = new GetMoreMessage(CollectionNamespace, cursorId, _batchSize);

                using (var packet = new BufferedRequestPacket())
                {
                    packet.AddMessage(getMoreMessage);
                    channel.Send(packet);
                }

                var receiveArgs = new ChannelReceiveArgs(getMoreMessage.RequestId);
                return channel.Receive(receiveArgs);
            }
        }

        private void KillCursor(ICursorChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel())
            {
                var killCursorsMessage = new KillCursorsMessage(new[] { cursorId });

                using (var packet = new BufferedRequestPacket())
                {
                    packet.AddMessage(killCursorsMessage);
                    channel.Send(packet);
                }
            }
        }
    }
}