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
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for ByteArrays.
    /// </summary>
    public class ByteArraySerializer : BsonBaseSerializer<byte[]>, IBsonSerializerWithRepresentation<ByteArraySerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArraySerializer"/> class.
        /// </summary>
        public ByteArraySerializer()
            : this(BsonType.Binary)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArraySerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public ByteArraySerializer(BsonType representation)
        {
            _representation = representation;
        }

        // public properties
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <returns>An object.</returns>
        public override byte[] Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Binary:
                    return bsonReader.ReadBytes();

                case BsonType.String:
                    var s = bsonReader.ReadString();
                    if ((s.Length % 2) != 0)
                    {
                        s = "0" + s; // prepend a zero to make length even
                    }
                    var bytes = new byte[s.Length / 2];
                    for (int i = 0; i < s.Length; i += 2)
                    {
                        var hex = s.Substring(i, 2);
                        var b = byte.Parse(hex, NumberStyles.HexNumber);
                        bytes[i / 2] = b;
                    }
                    return bytes;

                default:
                    var message = string.Format("Cannot deserialize Byte[] from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }
#pragma warning restore 618

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, byte[] value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                switch (_representation)
                {
                    case BsonType.Binary:
                        bsonWriter.WriteBytes(value);
                        break;

                    case BsonType.String:
                        var sb = new StringBuilder(value.Length * 2);
                        for (int i = 0; i < value.Length; i++)
                        {
                            sb.Append(string.Format("{0:x2}", value[i]));
                        }
                        bsonWriter.WriteString(sb.ToString());
                        break;

                    default:
                        var message = string.Format("'{0}' is not a valid Byte[] representation.", _representation);
                        throw new BsonSerializationException(message);
                }
            }
        }

        public ByteArraySerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new ByteArraySerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithRepresentation.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
