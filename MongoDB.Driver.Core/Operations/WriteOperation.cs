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
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// The base class for operations that perform writes.
    /// </summary>
    public abstract class WriteOperation : DatabaseOperation
    {
        // private fields
        private readonly WriteConcern _writeConcern;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOperation" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        protected WriteOperation(
            CollectionNamespace collectionNamespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern)
            : base(collectionNamespace, readerSettings, writerSettings)
        {
            Ensure.IsNotNull("writeConcern", writeConcern);

            _writeConcern = writeConcern;
        }

        // protected properties
        /// <summary>
        /// Gets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        protected WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // protected methods
        /// <summary>
        /// Reads the write concern result.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="sendMessageResult">The send message result.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <returns>
        /// A WriteConcern result (or null if sendMessageResult is null).
        /// </returns>
        /// <exception cref="MongoOperationException">Command 'getLastError' failed. No response returned.</exception>
        /// <exception cref="MongoWriteConcernException">
        /// </exception>
        protected WriteConcernResult ReadWriteConcernResult(
            IServerChannel channel,
            SendPacketWithWriteConcernResult sendMessageResult,
            BsonBinaryReaderSettings readerSettings)
        {
            Ensure.IsNotNull("channel", channel);
            Ensure.IsNotNull("sendMessageResult", sendMessageResult);
            Ensure.IsNotNull("readerSettings", readerSettings);

            WriteConcernResult writeConcernResult = null;
            if (sendMessageResult.GetLastErrorRequestId.HasValue)
            {
                var receiveArgs = new ChannelReceiveArgs(sendMessageResult.GetLastErrorRequestId.Value);
                using (var reply = channel.Receive(receiveArgs))
                {
                    if (reply.NumberReturned == 0)
                    {
                        throw new MongoOperationException("Command 'getLastError' failed. No response returned.");
                    }

                    var serializer = BsonSerializer.LookupSerializer(typeof(WriteConcernResult));
                    writeConcernResult = reply.DeserializeDocuments<WriteConcernResult>(serializer, null, readerSettings).Single();
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
        /// <param name="writerSettings">The writer settings.</param>
        /// <returns>A SendPacketWithWriteConcernResult.</returns>
        protected SendPacketWithWriteConcernResult SendPacketWithWriteConcern(
            IServerChannel channel,
            BufferedRequestPacket packet,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
        {
            Ensure.IsNotNull("channel", channel);
            Ensure.IsNotNull("request", packet);
            Ensure.IsNotNull("writerSettings", writerSettings);
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
                    new DatabaseNamespace(CollectionNamespace.DatabaseName).CommandCollection,
                    getLastErrorCommand,
                    QueryFlags.None,
                    0,
                    1,
                    null,
                    writerSettings);

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