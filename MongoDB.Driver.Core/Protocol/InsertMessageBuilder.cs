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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Builds a <see cref="BsonBufferedRequestMessage"/> to insert documents.
    /// </summary>
    public class InsertMessageBuilder : BsonBufferedRequestMessageBuilder
    {
        // private fields
        private readonly bool _checkElementNames;
        private readonly InsertFlags _flags;
        private readonly MongoNamespace _namespace;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertMessageBuilder" /> class.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="checkElementNames">if set to <c>true</c> [check element names].</param>
        /// <param name="writerSettings">The writer settings.</param>
        public InsertMessageBuilder(MongoNamespace @namespace, InsertFlags flags, bool checkElementNames, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Insert)
        {
            _namespace = @namespace;
            _checkElementNames = checkElementNames;
            _writerSettings = writerSettings;
        }

        // public methods
        /// <summary>
        /// Adds the document.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="nominalType">Type of the nominal.</param>
        /// <param name="document">The document.</param>
        public void AddDocument(BsonBufferedRequestMessage request, Type nominalType, object document)
        {
            int currentPosition = request.Buffer.Position;
            using (var writer = new BsonBinaryWriter(request.Buffer, false, _writerSettings))
            {
                BsonSerializer.Serialize(writer, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            int newPosition = request.Buffer.Position;
            ChangeMessageLength(request, MessageLength + newPosition - currentPosition);
        }

        /// <summary>
        /// Adds the rollover document.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="bytes">The bytes.</param>
        public void AddRolloverDocument(BsonBufferedRequestMessage request, byte[] bytes)
        {
            request.Buffer.WriteBytes(bytes);
            ChangeMessageLength(request, MessageLength + bytes.Length);
        }

        // protected methods
        /// <summary>
        /// Writes the message to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected override void Write(BsonBuffer buffer)
        {
            buffer.WriteInt32((int)_flags);
            buffer.WriteCString(__encoding, _namespace.FullName);
        }
    }
}