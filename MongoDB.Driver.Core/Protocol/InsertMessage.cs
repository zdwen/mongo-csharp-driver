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

using System;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents an Insert message.
    /// </summary>
    public class InsertMessage : RequestMessage
    {
        // private fields
        private readonly bool _checkInsertDocuments;
        private readonly InsertFlags _flags;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BsonBinaryWriterSettings _writerSettings;
        private int _lastDocumentStartPosition;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertMessage" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="checkInsertDocuments">Set to true if the inserted document(s) should be checked for invalid element names.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public InsertMessage(CollectionNamespace collectionNamespace, InsertFlags flags, bool checkInsertDocuments, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Insert)
        {
            _collectionNamespace = collectionNamespace;
            _flags = flags;
            _checkInsertDocuments = checkInsertDocuments;
            _writerSettings = writerSettings;
        }

        // public methods
        /// <summary>
        /// Adds an already serialized document to the message.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="serializedDocument">The already serialized document.</param>
        public void AddDocument(Stream stream, byte[] serializedDocument)
        {
            _lastDocumentStartPosition = (int)stream.Position;
            stream.Write(serializedDocument, 0, serializedDocument.Length);
            BackpatchMessageLength(stream);
        }

        /// <summary>
        /// Adds a document to the message.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="nominalType">The nominal type of the document.</param>
        /// <param name="document">The document.</param>
        public void AddDocument(Stream stream, Type nominalType, object document)
        {
            _lastDocumentStartPosition = (int)stream.Position;
            using (var bsonWriter = new BsonBinaryWriter(stream, _writerSettings))
            {
                bsonWriter.CheckElementNames = _checkInsertDocuments;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength(stream);
        }

        /// <summary>
        /// Removes the last document that was added to the message.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The last document that was added to the message (in already serialized form).</returns>
        public byte[] RemoveLastDocument(Stream stream)
        {
            var streamReader = new BsonStreamReader(stream, _writerSettings.Encoding);
            var lastDocumentLength = (int)(stream.Position - _lastDocumentStartPosition);

            stream.Position = _lastDocumentStartPosition;
            var lastDocument = streamReader.ReadBytes(lastDocumentLength);
            stream.Position = _lastDocumentStartPosition;
            stream.SetLength(_lastDocumentStartPosition);
            BackpatchMessageLength(stream);

            return lastDocument;
        }

        // protected methods
        /// <summary>
        /// Writes the body of the message a stream.
        /// </summary>
        /// <param name="streamWriter">The stream.</param>
        protected override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32((int)_flags);
            streamWriter.WriteCString(_collectionNamespace.FullName);
        }
    }
}
