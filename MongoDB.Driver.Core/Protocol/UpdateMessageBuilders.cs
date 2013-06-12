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
    /// Builds a <see cref="BsonBufferedRequestMessage"/> to update one or more documents.
    /// </summary>
    public sealed class UpdateMessageBuilders : BsonBufferedRequestMessageBuilder
    {
        // private fields
        private readonly UpdateFlags _flags;
        private readonly MongoNamespace _namespace;
        private readonly object _selector;
        private readonly object _update;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMessageBuilders" /> class.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="update">The update.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public UpdateMessageBuilders(MongoNamespace @namespace, UpdateFlags flags, object selector, object update, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Update)
        {
            Ensure.IsNotNull("@namespace", @namespace);
            Ensure.IsNotNull("selector", selector);
            Ensure.IsNotNull("update", update);
            Ensure.IsNotNull("writerSettings", writerSettings);

            _namespace = @namespace;
            _flags = flags;
            _selector = selector;
            _update = update;
            _writerSettings = writerSettings;
        }

        // protected methods
        /// <summary>
        /// Writes the message to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected override void Write(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // ZERO
            buffer.WriteCString(__encoding, _namespace.FullName); // fullCollectionName
            buffer.WriteInt32((int)_flags); // flags

            using (var writer = new BsonBinaryWriter(buffer, false, _writerSettings))
            {
                BsonSerializer.Serialize(writer, _selector.GetType(), _selector, DocumentSerializationOptions.SerializeIdFirstInstance);
                BsonSerializer.Serialize(writer, _update.GetType(), _update, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
        }
    }
}