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

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a dictionary serializer that has key and value serializers.
    /// </summary>
    public interface IBsonSerializerWithKeyValueSerializers
    {
        /// <summary>
        /// Gets the key serializer.
        /// </summary>
        /// <value>
        /// The key serializer.
        /// </value>
        IBsonSerializer KeySerializer { get; }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        IBsonSerializer ValueSerializer { get; }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified key serializer.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        IBsonSerializer WithKeySerializer(IBsonSerializer keySerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified value serializer.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        IBsonSerializer WithValueSerializer(IBsonSerializer valueSerializer);
    }

    /// <summary>
    /// Represents a dictionary serializer that has key and value serializers.
    /// </summary>
    /// <typeparam name="TSerializer">The type of the serializer.</typeparam>
    /// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IBsonSerializerWithKeyValueSerializers<TSerializer, TDictionary, TKey, TValue> :
        IBsonSerializerWithKeyValueSerializers
            where TSerializer : IBsonSerializer<TDictionary>
    {
        /// <summary>
        /// Gets the key serializer.
        /// </summary>
        /// <value>
        /// The key serializer.
        /// </value>
        new IBsonSerializer<TKey> KeySerializer { get; }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        new IBsonSerializer<TValue> ValueSerializer { get; }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified key serializer.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        TSerializer WithKeySerializer(IBsonSerializer<TKey> keySerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified value serializer.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        TSerializer WithValueSerializer(IBsonSerializer<TValue> valueSerializer);
    }
}
