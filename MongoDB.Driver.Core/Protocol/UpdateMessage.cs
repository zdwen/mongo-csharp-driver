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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents an Update message.
    /// </summary>
    public sealed class UpdateMessage : RequestMessage
    {
        // private fields
        private readonly bool _checkUpdateDocument;
        private readonly UpdateFlags _flags;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly object _selector;
        private readonly object _update;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMessage" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="selector">The query to select the document(s).</param>
        /// <param name="update">The update.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="checkUpdateDocument">Set to true if the update document should be checked for invalid element names.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public UpdateMessage(CollectionNamespace collectionNamespace, object selector, object update, UpdateFlags flags, bool checkUpdateDocument, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Update)
        {
            Ensure.IsNotNull("collectionNamespace", collectionNamespace);
            Ensure.IsNotNull("selector", selector);
            Ensure.IsNotNull("update", update);
            Ensure.IsNotNull("writerSettings", writerSettings);

            _collectionNamespace = collectionNamespace;
            _checkUpdateDocument = checkUpdateDocument;
            _flags = flags;
            _selector = selector;
            _update = update;
            _writerSettings = writerSettings;
        }

        // protected methods
        /// <summary>
        /// Writes the body of the message a stream.
        /// </summary>
        /// <param name="streamWriter">The stream.</param>
        protected override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(_collectionNamespace.FullName);
            streamWriter.WriteInt32((int)_flags);

            using (var bsonWriter = new BsonBinaryWriter(streamWriter.BaseStream, _writerSettings))
            {
                BsonSerializer.Serialize(bsonWriter, _selector.GetType(), _selector, DocumentSerializationOptions.SerializeIdFirstInstance);

                bsonWriter.CheckUpdateDocument = _checkUpdateDocument;
                BsonSerializer.Serialize(bsonWriter, _update.GetType(), _update, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
        }
    }
}