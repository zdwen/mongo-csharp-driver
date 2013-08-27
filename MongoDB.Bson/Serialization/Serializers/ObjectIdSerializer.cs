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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for ObjectIds.
    /// </summary>
    public class ObjectIdSerializer : BsonBaseSerializer<ObjectId>, IBsonSerializerWithRepresentation<ObjectIdSerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        public ObjectIdSerializer()
            : this(BsonType.ObjectId)
        {
        }

        public ObjectIdSerializer(BsonType representation)
        {
            _representation = representation;
        }

        // public properties
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <returns>An object.</returns>
        public override ObjectId Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.ObjectId:
                    return bsonReader.ReadObjectId();

                case BsonType.String:
                    return ObjectId.Parse(bsonReader.ReadString());

                default:
                    var message = string.Format("Cannot deserialize ObjectId from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, ObjectId value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.ObjectId:
                    bsonWriter.WriteObjectId(value);
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(value.ToString());
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid ObjectId representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        public ObjectIdSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new ObjectIdSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithRepresentation.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
