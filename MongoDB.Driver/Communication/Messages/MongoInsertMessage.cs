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
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal
{
    internal class MongoInsertMessage : MongoRequestMessage
    {
        // private fields
        private string _collectionFullName;
        private bool _checkElementNames;
        private InsertFlags _flags;
        private int _firstDocumentStartPosition;
        private int _lastDocumentStartPosition;

        // constructors
        internal MongoInsertMessage(
            BsonBinaryWriterSettings writerSettings,
            string collectionFullName,
            bool checkElementNames,
            InsertFlags flags)
            : base(MessageOpcode.Insert, writerSettings)
        {
            _collectionFullName = collectionFullName;
            _checkElementNames = checkElementNames;
            _flags = flags;
        }

        // internal methods
        internal void AddDocument(Stream stream, Type nominalType, object document)
        {
            _lastDocumentStartPosition = (int)stream.Position;
            using (var bsonWriter = new BsonBinaryWriter(stream, WriterSettings))
            {
                bsonWriter.CheckElementNames = _checkElementNames;
                BsonSerializer.Serialize(bsonWriter, nominalType, document, DocumentSerializationOptions.SerializeIdFirstInstance);
            }
            BackpatchMessageLength(stream);
        }

        internal byte[] RemoveLastDocument(Stream stream)
        {
            var streamReader = new BsonStreamReader(stream, WriterSettings.Encoding);
            var lastDocumentLength = (int)(stream.Position - _lastDocumentStartPosition);
            stream.Position = _lastDocumentStartPosition;
            var lastDocument = streamReader.ReadBytes(lastDocumentLength);
            stream.Position = _lastDocumentStartPosition;
            stream.SetLength(_lastDocumentStartPosition);
            BackpatchMessageLength(stream);
            return lastDocument;
        }

        internal void ResetBatch(Stream stream, byte[] lastDocument)
        {
            stream.Position = _firstDocumentStartPosition;
            stream.SetLength(_firstDocumentStartPosition);
            stream.Write(lastDocument, 0, lastDocument.Length);
            BackpatchMessageLength(stream);
        }

        // protected methods
        protected override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32((int)_flags);
            streamWriter.WriteCString(_collectionFullName);
            _firstDocumentStartPosition = (int)streamWriter.Position;
            // documents to be added later by calling AddDocument
        }
    }
}
