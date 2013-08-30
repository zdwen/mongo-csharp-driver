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

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// An interface implemented by a serializer.
    /// </summary>
    public interface IBsonSerializer
    {
        // properties
        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        /// <value>
        /// The type of the value.
        /// </value>
        Type ValueType { get; }

        // methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        object Deserialize(DeserializationContext context);

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        void Serialize(SerializationContext context, object value);
    }

    /// <summary>
    /// An interface implemented by a serializer for values of type TValue.
    /// </summary>
    /// <typeparam name="TValue">The type that this serializer knows how to serialize.</typeparam>
    public interface IBsonSerializer<TValue> : IBsonSerializer
    {
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>A T.</returns>
        new TValue Deserialize(DeserializationContext context);

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        void Serialize(SerializationContext context, TValue value);
    }
}
