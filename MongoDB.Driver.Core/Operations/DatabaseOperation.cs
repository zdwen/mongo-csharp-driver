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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Base class for an operation.
    /// </summary>
    public abstract class DatabaseOperation
    {
        // private fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseOperation" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        protected DatabaseOperation(
            CollectionNamespace collectionNamespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings)
        {
            Ensure.IsNotNull("namespace", collectionNamespace);
            Ensure.IsNotNull("readerSettings", readerSettings);
            Ensure.IsNotNull("writerSettings", writerSettings);

            _collectionNamespace = collectionNamespace;
            _readerSettings = (BsonBinaryReaderSettings)readerSettings.FrozenCopy();
            _writerSettings = (BsonBinaryWriterSettings)writerSettings.FrozenCopy();
        }

        // protected properties
        /// <summary>
        /// Gets the collection namespace the operation will be performed against.
        /// </summary>
        protected CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Adjusts the reader settings based on server specific settings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>The adjusted reader settings</returns>
        protected BsonBinaryReaderSettings GetServerAdjustedReaderSettings(ServerDescription server)
        {
            Ensure.IsNotNull("server", server);

            var readerSettings = _readerSettings.Clone();
            readerSettings.MaxDocumentSize = server.MaxDocumentSize;
            return readerSettings;
        }

        // protected methods
        /// <summary>
        /// Adjusts the writer settings based on server specific settings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>The adjusted writer settings.</returns>
        protected BsonBinaryWriterSettings GetServerAdjustedWriterSettings(ServerDescription server)
        {
            Ensure.IsNotNull("server", server);

            var writerSettings = _writerSettings.Clone();
            writerSettings.MaxDocumentSize = server.MaxDocumentSize;
            return writerSettings;
        }
    }
}