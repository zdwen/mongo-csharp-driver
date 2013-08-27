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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonInt64s.
    /// </summary>
    public class BsonInt64Serializer : BsonBaseSerializer<BsonInt64>
    {
        // private static fields
        private static BsonInt64Serializer __instance = new BsonInt64Serializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonInt64Serializer class.
        /// </summary>
        public BsonInt64Serializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonInt64Serializer class.
        /// </summary>
        public static BsonInt64Serializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <returns>An object.</returns>
        public override BsonInt64 Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Int64:
                    return new BsonInt64(bsonReader.ReadInt64());

                default:
                    var message = string.Format("Cannot deserialize BsonInt64 from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, BsonInt64 value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            bsonWriter.WriteInt64(value.Value);
        }
    }
}
