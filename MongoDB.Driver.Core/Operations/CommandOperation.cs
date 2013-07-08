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
    /// Represents a Command operation.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    public class CommandOperation<TCommandResult> : ReadOperation where TCommandResult : CommandResult
    {
        // private fields
        private readonly object _command;
        private readonly QueryFlags _flags;
        private readonly BsonDocument _options;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializationOptions _serializationOptions;
        private readonly IBsonSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandOperation{TCommandResult}" /> class.
        /// </summary>
        /// <param name="databaseNamespace">Name of the database.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="command">The command.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="options">The options.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <param name="serializer">The serializer.</param>
        public CommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            object command,
            QueryFlags flags,
            BsonDocument options,
            ReadPreference readPreference,
            IBsonSerializationOptions serializationOptions,
            IBsonSerializer serializer)
            : base(databaseNamespace.CommandCollection, readerSettings, writerSettings)
        {
            Ensure.IsNotNull("command", command);
            Ensure.IsNotNull("serializer", serializer);

            _command = command;
            _flags = flags;
            _options = options;
            _readPreference = readPreference;
            _serializationOptions = serializationOptions;
            _serializer = serializer;
        }

        // public methods
        /// <summary>
        /// Executes the Command operation.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The command result.</returns>
        public TCommandResult Execute(IServerChannel channel)
        {
            Ensure.IsNotNull("connection", channel);

            var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
            var writerSettings = GetServerAdjustedWriterSettings(channel.Server);
            var wrappedQuery = WrapQuery(channel.Server, _command, _options, _readPreference);

            var queryMessage = new QueryMessage(
                CollectionNamespace,
                wrappedQuery,
                _flags,
                0,
                -1,
                null,
                writerSettings);

            using (var packet = new BufferedRequestPacket())
            {
                packet.AddMessage(queryMessage);
                channel.Send(packet);
            }

            var receiveArgs = new ChannelReceiveArgs(queryMessage.RequestId);
            using (var reply = channel.Receive(receiveArgs))
            {
                if (reply.NumberReturned == 0)
                {
                    var commandDocument = _command.ToBsonDocument();
                    var commandName = commandDocument.ElementCount == 0 ? "(no name)" : commandDocument.GetElement(0).Name;
                    var message = string.Format("Command '{0}' failed. No response returned.", commandName);
                    throw new MongoOperationException(message);
                }

                var commandResult = reply.DeserializeDocuments<TCommandResult>(_serializer, _serializationOptions, readerSettings).Single();
                commandResult.Command = _command;

                if (!commandResult.Ok)
                {
                    var commandDocument = _command.ToBsonDocument();
                    var commandName = commandDocument.ElementCount == 0 ? "(no name)" : commandDocument.GetElement(0).Name;
                    var message = string.Format("Command '{0}' failed.", commandName);
                    throw new MongoOperationException(message, commandResult.ToBsonDocument());
                }

                return commandResult;
            }
        }
    }
}