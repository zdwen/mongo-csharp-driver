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
using System.Globalization;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for SBytes.
    /// </summary>
    public class SByteSerializer : BsonBaseSerializer<sbyte>, IBsonSerializerWithRepresentation<SByteSerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        public SByteSerializer()
            : this(BsonType.Int32)
        {
        }

        public SByteSerializer(BsonType representation)
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
        public override sbyte Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;
            sbyte value;
            var lostData = false;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Binary:
                    var bytes = bsonReader.ReadBytes();
                    if (bytes.Length != 1)
                    {
                        throw new FileFormatException("Binary data for SByte must be exactly one byte long.");
                    }
                    value = (sbyte)bytes[0];
                    break;

                case BsonType.Int32:
                    var int32Value = bsonReader.ReadInt32();
                    value = (sbyte)int32Value;
                    lostData = (int)value != int32Value;
                    break;

                case BsonType.Int64:
                    var int64Value = bsonReader.ReadInt64();
                    value = (sbyte)int64Value;
                    lostData = (int)value != int64Value;
                    break;

                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if (s.Length == 1)
                    {
                        s = "0" + s;
                    }
                    value = (sbyte)byte.Parse(s, NumberStyles.HexNumber);
                    break;

                default:
                    var message = string.Format("Cannot deserialize SByte from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }

            if (lostData)
            {
                var message = string.Format("Data loss occurred when trying to convert from {0} to SByte.", bsonType);
                throw new FileFormatException(message);
            }

            return value;
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, sbyte value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Binary:
                    bsonWriter.WriteBytes(new byte[] { (byte)value });
                    break;

                case BsonType.Int32:
                    bsonWriter.WriteInt32(value);
                    break;

                case BsonType.Int64:
                    bsonWriter.WriteInt64(value);
                    break;

                case BsonType.String:
                    bsonWriter.WriteString(string.Format("{0:x2}", (byte)value));
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid SByte representation.", _representation);
                    throw new BsonSerializationException(message);
            }
        }

        public SByteSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new SByteSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithRepresentation.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
