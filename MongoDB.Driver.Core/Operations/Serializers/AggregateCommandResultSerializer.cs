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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core.Operations.Serializers
{
    /// <summary>
    /// Represents a serializer for the <see cref="AggregationCommandResult{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class AggregateCommandResultSerializer<TDocument> : BsonBaseSerializer
    {
        // private fields
        private readonly IBsonSerializer _resultSerializer;
        private readonly IBsonSerializationOptions _resultSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateCommandResultSerializer{TDocument}" /> class.
        /// </summary>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="resultSerializationOptions">The result serialization options.</param>
        public AggregateCommandResultSerializer(IBsonSerializer resultSerializer, IBsonSerializationOptions resultSerializationOptions)
        {
            _resultSerializer = resultSerializer;
            _resultSerializationOptions = resultSerializationOptions;
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            var response = new BsonDocument();
            long cursorId = 0;
            IEnumerable<TDocument> firstBatch = null;

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                if (name == "result")
                {
                    firstBatch = DeserializeResults(bsonReader);
                }
                else if (name == "cursor")
                {
                    var cursorDocument = new BsonDocument();
                    var cursorBatch = DeserializeCursor(cursorDocument, bsonReader);
                    cursorId = cursorBatch.CursorId;
                    firstBatch = cursorBatch.Documents;
                    response.Add("cursor", cursorDocument);
                }
                else
                {
                    var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
                    response.Add(name, value);
                }
            }
            bsonReader.ReadEndDocument();

            return new AggregateCommandResult<TDocument>(response, cursorId, firstBatch);
        }

        // private methods
        private CursorBatch<TDocument> DeserializeCursor(BsonDocument cursorDocument, BsonReader bsonReader)
        {
            long cursorId = 0;
            IEnumerable<TDocument> firstBatch = null;
            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                if (name == "id")
                {
                    cursorId = bsonReader.ReadInt64();
                    cursorDocument.Add("id", cursorId);
                }
                else if (name == "firstBatch")
                {
                    firstBatch = DeserializeResults(bsonReader);
                }
                else
                {
                    var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
                    cursorDocument.Add(name, value);
                }
            }
            bsonReader.ReadEndDocument();

            return new CursorBatch<TDocument>(cursorId, firstBatch);
        }

        private IEnumerable<TDocument> DeserializeResults(BsonReader bsonReader)
        {
            var values = new List<TDocument>();

            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                values.Add((TDocument)_resultSerializer.Deserialize(bsonReader, typeof(TDocument), _resultSerializationOptions));
            }
            bsonReader.ReadEndArray();

            return values;
        }
    }
}