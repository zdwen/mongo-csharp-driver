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
    /// Performs a query.
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
        /// <param name="namespace">The namespace.</param>
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
            MongoNamespace @namespace,
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
            : base(@namespace, readerSettings, writerSettings)
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
        /// Executes the specified connection provider.
        /// </summary>
        /// <param name="channelProvider">The connection provider.</param>
        /// <returns>An enumerator to enumerate over the results.</returns>
        public IEnumerator<TDocument> Execute(ICursorChannelProvider channelProvider)
        {
            Ensure.IsNotNull("channelProvider", channelProvider);

            var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
            ReplyMessage reply = null;
            try
            {
                var count = 0;
                var limit = (_limit >= 0) ? _limit : -_limit;

                reply = GetFirstBatch(channelProvider);
                foreach (var document in GetDocuments(reply, readerSettings))
                {
                    if (limit != 0 && count == limit)
                    {
                        yield break;
                    }
                    yield return document;
                    count++;
                }

                while (reply.CursorId != 0)
                {
                    var cursorId = reply.CursorId;
                    reply.Dispose();
                    reply = GetNextBatch(channelProvider, cursorId);
                    foreach (var document in GetDocuments(reply, readerSettings))
                    {
                        if (limit != 0 && count == limit)
                        {
                            yield break;
                        }
                        yield return document;
                        count++;
                    }

                    if (reply.CursorId != 0 && (_flags & QueryFlags.TailableCursor) != 0)
                    {
                        if ((reply.Flags & ReplyFlags.AwaitCapable) == 0)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        }
                    }
                }
            }
            finally
            {
                if (reply != null)
                {
                    var cursorId = reply.CursorId;
                    reply.Dispose();
                    if (cursorId != 0)
                    {
                        KillCursor(channelProvider, cursorId);
                    }
                }
            }
        }

        // private methods
        private IEnumerable<TDocument> GetDocuments(ReplyMessage message, BsonBinaryReaderSettings settings)
        {
            return message.ReadDocuments<TDocument>(settings, _serializer, _serializationOptions);
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
                var queryMessage = new QueryMessageBuilder(
                    Namespace,
                    _flags,
                    _skip,
                    numberToReturn,
                    _query,
                    _fields,
                    writerSettings);

                int requestId;
                using(var request = new BsonBufferedRequestMessage())
                {
                    queryMessage.AddToRequest(request);
                    channel.SendMessage(request);
                    requestId = request.RequestId;
                }

                var receiveParameters = new ReceiveMessageParameters(requestId);
                return channel.ReceiveMessage(receiveParameters);
            }
        }

        private ReplyMessage GetNextBatch(ICursorChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel())
            {
                var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
                var getMoreMessage = new GetMoreMessageBuilder(
                    Namespace,
                    _limit,
                    cursorId);

                int requestId;
                using (var request = new BsonBufferedRequestMessage())
                {
                    getMoreMessage.AddToRequest(request);
                    channel.SendMessage(request);
                    requestId = request.RequestId;
                }

                var receiveParameters = new ReceiveMessageParameters(requestId);
                return channel.ReceiveMessage(receiveParameters);
            }
        }

        private void KillCursor(ICursorChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel())
            {
                var killCursorsMessage = new KillCursorsMessageBuilder(new[] { cursorId });
                using (var request = new BsonBufferedRequestMessage())
                {
                    killCursorsMessage.AddToRequest(request);
                    channel.SendMessage(request);
                }
            }
        }
    }
}