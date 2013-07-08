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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents a Delete message.
    /// </summary>
    public sealed class DeleteMessage : RequestMessage
    {
        // private fields
        private readonly DeleteFlags _flags;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly object _selector;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessage" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public DeleteMessage(CollectionNamespace collectionNamespace, object selector, DeleteFlags flags, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Delete)
        {
            Ensure.IsNotNull("collectionNamespace", collectionNamespace);
            Ensure.IsNotNull("selector", selector);
            Ensure.IsNotNull("writerSettings", writerSettings);

            _collectionNamespace = collectionNamespace;
            _flags = flags;
            _selector = selector;
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
                // TODO: pass in a serializer for this guy?
                BsonSerializer.Serialize(bsonWriter, _selector.GetType(), _selector, null);
            }
        }
    }
}