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
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Sessions;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Base class for command operations.
    /// </summary>
    public abstract class CommandOperationBase<TResult, TCommandResult> : QueryOperationBase<TResult> where TCommandResult : CommandResult
    {
        // protected methods
        /// <summary>
        /// Executes the command with the given args.
        /// </summary>
        /// <param name="channelProvider">The channel provider.</param>
        /// <param name="args">The args.</param>
        /// <returns>The result of the execution.</returns>
        protected TCommandResult Execute(IServerChannelProvider channelProvider, ExecutionArgs args)
        {
            var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
            var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);

            var protocol = new QueryProtocol<TCommandResult>(
                collection: args.Database.CommandCollection,
                fields: null,
                flags: args.ReadPreference.ReadPreferenceMode == ReadPreferenceMode.Primary ? QueryFlags.None : QueryFlags.SlaveOk,
                numberToReturn: 1,
                query: WrapQuery(channelProvider.Server, args.Command, null, args.ReadPreference),
                readerSettings: readerSettings,
                serializer: args.Serializer,
                serializationOptions: args.SerializationOptions,
                skip: 0,
                writerSettings: writerSettings);

            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
            {
                var result = protocol.Execute(channel);
                var docs = result.Documents.ToList();
                if (docs.Count == 0)
                {
                    var commandDocument = args.Command.ToBsonDocument();
                    var commandName = commandDocument.ElementCount == 0 ? "(no name)" : commandDocument.GetElement(0).Name;
                    var message = string.Format("Command '{0}' failed. No response returned.", commandName);
                    throw new MongoOperationException(message);
                }

                var commandResult = docs.Single();
                commandResult.Command = args.Command;

                if (!commandResult.Ok)
                {
                    var commandDocument = args.Command.ToBsonDocument();
                    var commandName = commandDocument.ElementCount == 0 ? "(no name)" : commandDocument.GetElement(0).Name;
                    var message = string.Format("Command '{0}' failed.", commandName);
                    throw new MongoOperationException(message, commandResult.ToBsonDocument());
                }

                return commandResult;
            }
        }

        // nested class
        /// <summary>
        /// Arguments for executing a command.
        /// </summary>
        protected class ExecutionArgs
        {
            /// <summary>
            /// Gets or sets the command.
            /// </summary>
            public object Command { get; set; }

            /// <summary>
            /// Gets or sets the database.
            /// </summary>
            public DatabaseNamespace Database { get; set; }

            /// <summary>
            /// Gets or sets the read preference.
            /// </summary>
            public ReadPreference ReadPreference { get; set; }

            /// <summary>
            /// Gets or sets the serializer.
            /// </summary>
            public IBsonSerializer Serializer { get; set; }

            /// <summary>
            /// Gets or sets the serialization options.
            /// </summary>
            public IBsonSerializationOptions SerializationOptions { get; set; }
        }
    }
}