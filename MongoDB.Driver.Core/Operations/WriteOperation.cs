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
        /// <param name="namespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        protected WriteOperation(
            MongoNamespace @namespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern)
            : base(@namespace, readerSettings, writerSettings)
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
        /// Sends the message with write concern.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The message.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <returns>A result containing the results of a getLastError call if one was issued.</returns>
        protected WriteConcernResult SendMessageWithWriteConcern(
            IServerChannel channel,
            BsonBufferedRequestMessage request,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern)
        {
            Ensure.IsNotNull("channel", channel);
            Ensure.IsNotNull("request", request);
            Ensure.IsNotNull("readerSettings", readerSettings);
            Ensure.IsNotNull("writerSettings", writerSettings);
            Ensure.IsNotNull("writeConcern", writeConcern);

            BsonDocument getLastErrorCommand = null;
            if (writeConcern.Enabled)
            {
                var fsync = (writeConcern.FSync == null) ? null : (BsonValue)writeConcern.FSync;
                var journal = (writeConcern.Journal == null) ? null : (BsonValue)writeConcern.Journal;
                var w = (writeConcern.W == null) ? null : writeConcern.W.ToGetLastErrorWValue();
                var wTimeout = (writeConcern.WTimeout == null) ? null : (BsonValue)(int)writeConcern.WTimeout.Value.TotalMilliseconds;

                getLastErrorCommand = new BsonDocument
                {
                    { "getlasterror", 1 }, // use all lowercase for backward compatibility
                    { "fsync", fsync, fsync != null },
                    { "j", journal, journal != null },
                    { "w", w, w != null },
                    { "wtimeout", wTimeout, wTimeout != null }
                };

                // piggy back on network transmission for message
                var queryMessage = new QueryMessageBuilder(
                    Namespace.CommandCollection,
                    QueryFlags.None,
                    0,
                    1,
                    getLastErrorCommand,
                    null, 
                    writerSettings);

                queryMessage.AddToRequest(request);
            }
                
            channel.SendMessage(request);

            WriteConcernResult writeConcernResult = null;
            if (writeConcern.Enabled)
            {
                var receiveParameters = new ReceiveMessageParameters(request.RequestId);
                using (var reply = channel.ReceiveMessage(receiveParameters))
                {
                    if (reply.NumberReturned == 0)
                    {
                        throw new MongoOperationException("Command 'getLastError' failed. No response returned.");
                    }

                    var serializer = BsonSerializer.LookupSerializer(typeof(WriteConcernResult));
                    writeConcernResult = reply.ReadDocuments<WriteConcernResult>(readerSettings, serializer, null).Single();
                    writeConcernResult.Command = getLastErrorCommand;

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
    }
}