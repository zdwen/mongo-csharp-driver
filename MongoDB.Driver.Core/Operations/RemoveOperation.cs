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

using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Performs the removal of a document or documents.
    /// </summary>
    public class RemoveOperation : WriteOperation
    {
        // private fields
        private readonly DeleteFlags _flags;
        private readonly object _query;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveOperation" /> class.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <param name="query">The query.</param>
        /// <param name="flags">The flags.</param>
        public RemoveOperation(
            MongoNamespace @namespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern,
            object query,
            DeleteFlags flags)
            : base(@namespace, readerSettings, writerSettings, writeConcern)
        {
            _query = query;
            _flags = flags;
        }

        // public methods
        /// <summary>
        /// Executes the specified connection.
        /// </summary>
        /// <param name="channel">The connection.</param>
        /// <returns></returns>
        public WriteConcernResult Execute(IServerChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
            var writerSettings = GetServerAdjustedWriterSettings(channel.Server);

            var deleteMessage = new DeleteMessageBuilder(
                Namespace,
                _flags,
                _query,
                writerSettings);

            using (var request = new BsonBufferedRequestMessage())
            {
                deleteMessage.AddToRequest(request);
                return SendMessageWithWriteConcern(channel, request, readerSettings, writerSettings, WriteConcern);
            }
        }
    }
}