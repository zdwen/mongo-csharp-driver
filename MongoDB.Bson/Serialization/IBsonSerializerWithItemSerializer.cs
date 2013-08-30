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
    /// Represents a array serializer that has an item serializer.
    /// </summary>
    public interface IBsonSerializerWithItemSerializer
    {
        /// <summary>
        /// Gets the item serializer.
        /// </summary>
        /// <value>
        /// The item serializer.
        /// </value>
        IBsonSerializer ItemSerializer { get; }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        IBsonSerializer WithItemSerializer(IBsonSerializer itemSerializer);
    }

    /// <summary>
    /// Represents a array serializer that has an item serializer.
    /// </summary>
    /// <typeparam name="TSerializer">The type of the serializer.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public interface IBsonSerializerWithItemSerializer<TSerializer, TValue, TItem> : IBsonSerializerWithItemSerializer where TSerializer : IBsonSerializer<TValue>
    {
        /// <summary>
        /// Gets the item serializer.
        /// </summary>
        /// <value>
        /// The item serializer.
        /// </value>
        new IBsonSerializer<TItem> ItemSerializer { get; }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        TSerializer WithItemSerializer(IBsonSerializer<TItem> itemSerializer);
    }
}
