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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations.Serializers
{
    /// <summary>
    /// Represents a serializer for a <see cref="AggregateCommandResult{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class AggregateCommandResultSerializer<TDocument> : BsonBaseSerializer
    {
        // private fields
        private readonly IBsonSerializer _documentSerializer;
        private readonly IBsonSerializationOptions _documentSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateCommandResultSerializer{TDocument}" /> class.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="documentSerializationOptions">The document serialization options.</param>
        public AggregateCommandResultSerializer(IBsonSerializer documentSerializer, IBsonSerializationOptions documentSerializationOptions)
        {
            Ensure.IsNotNull("documentSerializer", documentSerializer);
            Ensure.IsNotNull("documentSerializationOptions", documentSerializationOptions);

            _documentSerializer = documentSerializer;
            _documentSerializationOptions = documentSerializationOptions;
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
                switch (name)
                {
                    case "result":
                        firstBatch = DeserializeFirstBatch(bsonReader);
                        break;
                    case "cursor":
                        var cursorResponse = DeserializeCursorResponse(bsonReader);
                        cursorId = cursorResponse.CursorId;
                        firstBatch = cursorResponse.FirstBatch;
                        response.Add("cursor", cursorResponse.Response);
                        break;
                    default:
                        var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
                        response.Add(name, value);
                        break;
                }
            }
            bsonReader.ReadEndDocument();

            return new AggregateCommandResult<TDocument>(response, cursorId, firstBatch);
        }

        // private methods
        private CursorResponse DeserializeCursorResponse(BsonReader bsonReader)
        {
            var response = new BsonDocument();
            long cursorId = 0;
            IEnumerable<TDocument> firstBatch = null;

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                switch (name)
                {
                    case "id":
                        cursorId = bsonReader.ReadInt64();
                        response.Add("id", cursorId);
                        break;
                    case "firstBatch":
                        firstBatch = DeserializeFirstBatch(bsonReader);
                        break;
                    default:
                        var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
                        response.Add(name, value);
                        break;
                }
            }
            bsonReader.ReadEndDocument();

            return new CursorResponse { Response = response, CursorId = cursorId, FirstBatch = firstBatch };
        }

        private IEnumerable<TDocument> DeserializeFirstBatch(BsonReader bsonReader)
        {
            var firstBatch = new List<TDocument>();

            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                firstBatch.Add((TDocument)_documentSerializer.Deserialize(bsonReader, typeof(TDocument), _documentSerializationOptions));
            }
            bsonReader.ReadEndArray();

            return firstBatch;
        }

        // nested classes
        private class CursorResponse
        {
            public BsonDocument Response { get; set; }
            public long CursorId { get; set; }
            public IEnumerable<TDocument> FirstBatch { get; set; }
        }
    }
}