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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Command operation.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    public class CommandOperation<TCommandResult> : ReadOperation<TCommandResult> where TCommandResult : CommandResult
    {
        // private fields
        private object _command;
        private DatabaseNamespace _database;
        private QueryFlags _flags;
        private BsonDocument _options;
        private ReadPreference _readPreference;
        private IBsonSerializationOptions _serializationOptions;
        private IBsonSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandOperation{TCommandResult}" /> class.
        /// </summary>
        public CommandOperation()
        {
            _readPreference = ReadPreference.Primary;
        }

        // public properties
        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public object Command
        {
            get { return _command; }
            set { _command = value; }
        }

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        public DatabaseNamespace Database
        {
            get { return _database; }
            set { _database = value; }
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
        /// Gets or sets the options.
        /// </summary>
        public BsonDocument Options
        {
            get { return _options; }
            set { _options = value; }
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

        // public methods
        /// <summary>
        /// Executes the Command operation.
        /// </summary>
        /// <returns>The command result.</returns>
        /// <exception cref="MongoOperationException"></exception>
        public override TCommandResult Execute()
        {
            ValidateRequiredProperties();

            using (var channelProvider = CreateServerChannelProvider(new ReadPreferenceServerSelector(_readPreference), true))
            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
            {
                var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
                var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);
                var wrappedQuery = WrapQuery(channelProvider.Server, _command, _options, _readPreference);

                var queryMessage = new QueryMessage(
                    Database.CommandCollection,
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
                    reply.ThrowIfQueryFailureFlagIsSet();
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

        // protected methods
        /// <summary>
        /// Validates the required properties.
        /// </summary>
        protected override void ValidateRequiredProperties()
        {
            base.ValidateRequiredProperties();
            Ensure.IsNotNull("Command", _command);
            Ensure.IsNotNull("Database", _database);
            Ensure.IsNotNull("Serializer", _serializer);
        }
    }
}
