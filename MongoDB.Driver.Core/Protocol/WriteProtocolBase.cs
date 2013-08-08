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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// The base class for write based protocols.
    /// </summary>
    public abstract class WriteProtocolBase<TProtocolResult> : IProtocol<TProtocolResult>
    {
        // private fields
        private readonly CollectionNamespace _collection;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly WriteConcern _writeConcern;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteProtocolBase{TProtocolResult}" /> class.
        /// </summary>
        protected WriteProtocolBase(CollectionNamespace collection, 
            BsonBinaryReaderSettings readerSettings,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
        {
            Ensure.IsNotNull("collection", collection);
            Ensure.IsNotNull("readerSettings", readerSettings);
            Ensure.IsNotNull("writeConcern", writeConcern);
            Ensure.IsNotNull("writerSettings", writerSettings);

            _collection = collection;
            _readerSettings = readerSettings;
            _writeConcern = writeConcern;
            _writerSettings = writerSettings;
        }

        // protected properties
        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        public CollectionNamespace Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Gets the reader settings.
        /// </summary>
        public BsonBinaryReaderSettings ReaderSettings
        {
            get { return _readerSettings; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        /// <summary>
        /// Gets the writer settings.
        /// </summary>
        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
        }

        // public methods
        /// <summary>
        /// Executes the specified channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public abstract TProtocolResult Execute(IChannel channel);

        // protected methods
        /// <summary>
        /// Reads the write concern result.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="sendMessageResult">The send message result.</param>
        /// <returns>A WriteConcern result (or null if sendMessageResult is null).</returns>
        protected WriteConcernResult ReadWriteConcernResult(
            IChannel channel,
            SendPacketWithWriteConcernResult sendMessageResult)
        {
            Ensure.IsNotNull("channel", channel);
            Ensure.IsNotNull("sendMessageResult", sendMessageResult);

            WriteConcernResult writeConcernResult = null;
            if (sendMessageResult.GetLastErrorRequestId.HasValue)
            {
                var receiveArgs = new ChannelReceiveArgs(sendMessageResult.GetLastErrorRequestId.Value);
                using (var reply = channel.Receive(receiveArgs))
                {
                    reply.ThrowIfQueryFailureFlagIsSet();
                    if (reply.NumberReturned == 0)
                    {
                        throw new MongoOperationException("Command 'getLastError' failed. No response returned.");
                    }

                    var serializer = BsonSerializer.LookupSerializer(typeof(WriteConcernResult));
                    writeConcernResult = reply.DeserializeDocuments<WriteConcernResult>(serializer, null, _readerSettings).Single();
                    writeConcernResult.Command = sendMessageResult.GetLastErrorCommand;

                    if (!writeConcernResult.Ok)
                    {
                        var errorMessage = string.Format(
                            "WriteConcern detected an error '{0}'. (response was {1}).",
                            writeConcernResult.ErrorMessage, writeConcernResult.Response.ToJson());
                        throw new MongoWriteConcernException(errorMessage, writeConcernResult);
                    }
                    if (writeConcernResult.HasLastErrorMessage)
                    {
                        var errorMessage = string.Format(
                            "WriteConcern detected an error '{0}'. (Response was {1}).",
                            writeConcernResult.LastErrorMessage, writeConcernResult.Response.ToJson());
                        throw new MongoWriteConcernException(errorMessage, writeConcernResult);
                    }
                }
            }

            return writeConcernResult;
        }

        /// <summary>
        /// Sends the message with write concern.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="packet">The packet.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <returns>A SendPacketWithWriteConcernResult.</returns>
        protected SendPacketWithWriteConcernResult SendPacketWithWriteConcern(
            IChannel channel,
            BufferedRequestPacket packet,
            WriteConcern writeConcern)
        {
            Ensure.IsNotNull("channel", channel);
            Ensure.IsNotNull("request", packet);
            Ensure.IsNotNull("writeConcern", writeConcern);

            var result = new SendPacketWithWriteConcernResult();
            if (writeConcern.Enabled)
            {
                var fsync = (writeConcern.FSync == null) ? null : (BsonValue)writeConcern.FSync;
                var journal = (writeConcern.Journal == null) ? null : (BsonValue)writeConcern.Journal;
                var w = (writeConcern.W == null) ? null : writeConcern.W.ToGetLastErrorWValue();
                var wTimeout = (writeConcern.WTimeout == null) ? null : (BsonValue)(int)writeConcern.WTimeout.Value.TotalMilliseconds;

                var getLastErrorCommand = new BsonDocument
                {
                    { "getlasterror", 1 }, // use all lowercase for backward compatibility
                    { "fsync", fsync, fsync != null },
                    { "j", journal, journal != null },
                    { "w", w, w != null },
                    { "wtimeout", wTimeout, wTimeout != null }
                };

                // piggy back on network transmission for message
                var getLastErrorMessage = new QueryMessage(
                    new DatabaseNamespace(Collection.DatabaseName).CommandCollection,
                    getLastErrorCommand,
                    QueryFlags.None,
                    0,
                    1,
                    null,
                    _writerSettings);

                packet.AddMessage(getLastErrorMessage);

                result.GetLastErrorCommand = getLastErrorCommand;
                result.GetLastErrorRequestId = getLastErrorMessage.RequestId;
            }

            channel.Send(packet);

            return result;
        }

        // nested classes
        /// <summary>
        /// Represents the result of the SendPacketWithWriteConcern method.
        /// </summary>
        protected class SendPacketWithWriteConcernResult
        {
            /// <summary>
            /// The GetLastErrorCommand.
            /// </summary>
            public BsonDocument GetLastErrorCommand;

            /// <summary>
            /// The GetLastErrorRequestId.
            /// </summary>
            public int? GetLastErrorRequestId;
        }
    }
}