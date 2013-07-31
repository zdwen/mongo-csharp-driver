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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Query operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class QueryOperation<TDocument> : ReadOperation<IEnumerator<TDocument>>
    {
        // private fields
        private int _batchSize;
        private CollectionNamespace _collection;
        private object _fields;
        private QueryFlags _flags;
        private int _limit;
        private BsonDocument _options;
        private object _query;
        private ReadPreference _readPreference;
        private IBsonSerializationOptions _serializationOptions;
        private IBsonSerializer _serializer;
        private int _skip;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOperation{TDocument}" /> class.
        /// </summary>
        public QueryOperation()
        {
            _readPreference = ReadPreference.Primary;
        }

        // public properties
        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        public int BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        public CollectionNamespace Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        public object Fields
        {
            get { return _fields; }
            set { _fields = value; }
        }

        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        public QueryFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the query options.
        /// </summary>
        public BsonDocument Options
        {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// Gets or sets the query object.
        /// </summary>
        public object Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the serialization options.
        /// </summary>
        public IBsonSerializationOptions SerializationOptions
        {
            get { return _serializationOptions; }
            set { _serializationOptions = value; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }

        /// <summary>
        /// Gets or sets the skip.
        /// </summary>
        public int Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }

        // public methods
        /// <summary>
        /// Executes the Query operation.
        /// </summary>
        /// <returns>An enumerator to enumerate over the results.</returns>
        public override IEnumerator<TDocument> Execute()
        {
            ValidateRequiredProperties();

            // since we're going to block anyway when a tailable cursor is temporarily out of data
            // we might as well do it as efficiently as possible
            var flags = _flags;
            if ((flags & QueryFlags.TailableCursor) != 0)
            {
                flags |= QueryFlags.AwaitData;
            }

            var count = 0;
            var limit = (_limit >= 0) ? _limit : -_limit;

            using (var channelProvider = CreateServerChannelProvider(new ReadPreferenceServerSelector(_readPreference), true))
            {
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
        }

        // protected methods
        /// <summary>
        /// Validates the required properties.
        /// </summary>
        protected override void ValidateRequiredProperties()
        {
            base.ValidateRequiredProperties();
            Ensure.IsNotNull("Collection", _collection);
            Ensure.IsNotNull("Query", _query);
            Ensure.IsNotNull("Serializer", _serializer);
        }

        // private methods
        private IEnumerable<TDocument> DeserializeDocuments(IServerChannelProvider channelProvider)
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

        private ReplyMessage GetFirstBatch(IServerChannelProvider channelProvider)
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

            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
            {
                var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);
                var wrappedQuery = WrapQuery(channelProvider.Server, _query, _options, _readPreference);

                var queryMessage = new QueryMessage(
                    Collection,
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
                var reply = channel.Receive(receiveArgs);
                reply.ThrowIfQueryFailureFlagIsSet();

                return reply;
            }
        }

        private ReplyMessage GetNextBatch(IServerChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
            {
                var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
                var getMoreMessage = new GetMoreMessage(Collection, cursorId, _batchSize);

                using (var packet = new BufferedRequestPacket())
                {
                    packet.AddMessage(getMoreMessage);
                    channel.Send(packet);
                }

                var receiveArgs = new ChannelReceiveArgs(getMoreMessage.RequestId);
                var reply = channel.Receive(receiveArgs);
                reply.ThrowIfQueryFailureFlagIsSet();

                return reply;
            }
        }

        private void KillCursor(IServerChannelProvider channelProvider, long cursorId)
        {
            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
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