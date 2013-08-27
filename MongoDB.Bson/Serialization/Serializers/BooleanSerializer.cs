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
using System.Xml;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Booleans.
    /// </summary>
    public class BooleanSerializer : BsonBaseSerializer<bool>, IBsonSerializerWithRepresentation<BooleanSerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanSerializer"/> class.
        /// </summary>
        public BooleanSerializer()
            : this(BsonType.Boolean)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public BooleanSerializer(BsonType representation)
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
        /// <param name="actualType">The actual type of the object.</param>
        /// <returns>An object.</returns>
        public override bool Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Boolean:
                    return bsonReader.ReadBoolean();

                case BsonType.Double:
                    return bsonReader.ReadDouble() != 0.0;

                case BsonType.Int32:
                    return bsonReader.ReadInt32() != 0;

                case BsonType.Int64:
                    return bsonReader.ReadInt64() != 0;

                case BsonType.Null:
                    bsonReader.ReadNull();
                    return false;

                case BsonType.String:
                    return XmlConvert.ToBoolean(bsonReader.ReadString().ToLower());

                default:
                    var message = string.Format("Cannot deserialize Boolean from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, bool value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Boolean:
                    bsonWriter.WriteBoolean(value);
                    break;

                case BsonType.Double:
                    bsonWriter.WriteDouble(value ? 1.0 : 0.0);
                    break;

                case BsonType.Int32:
                    bsonWriter.WriteInt32(value ? 1 : 0);
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(value ? 1 : 0);
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(XmlConvert.ToString(value));
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid Boolean representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        public BooleanSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new BooleanSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithRepresentation.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
