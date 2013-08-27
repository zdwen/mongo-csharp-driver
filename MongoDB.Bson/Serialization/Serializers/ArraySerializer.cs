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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for one-dimensional arrays.
    /// </summary>
    /// <typeparam name="TItem">The type of the elements.</typeparam>
    public class ArraySerializer<TItem> :
        EnumerableSerializerBase<TItem[], TItem>,
        IBsonSerializerWithConfigurableChildSerializer,
        IBsonSerializerWithItemSerializer<ArraySerializer<TItem>, TItem[], TItem>, IBsonArraySerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySerializer{TItem}"/> class.
        /// </summary>
        public ArraySerializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySerializer{TItem}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public ArraySerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        {
        }

        // public methods
        public ArraySerializer<TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            if (itemSerializer == ItemSerializer)
            {
                return this;
            }
            else
            {
                return new ArraySerializer<TItem>(itemSerializer);
            }
        }

        // protected methods
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <param name="item">The item.</param>
        protected override void AddItem(object accumulator, TItem item)
        {
            ((List<TItem>)accumulator).Add(item);
        }

        /// <summary>
        /// Creates the accumulator.
        /// </summary>
        /// <returns>The accumulator.</returns>
        protected override object CreateAccumulator()
        {
            return new List<TItem>();
        }

        /// <summary>
        /// Enumerates the items in serialization order.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The items.</returns>
        protected override IEnumerable<TItem> EnumerateItemsInSerializationOrder(TItem[] value)
        {
            return (IEnumerable<TItem>)value;
        }

        /// <summary>
        /// Finalizes the result.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <returns>The result.</returns>
        protected override TItem[] FinalizeResult(object accumulator)
        {
            return ((List<TItem>)accumulator).ToArray();
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.ConfigurableChildSerializer
        {
            get { return ItemSerializer; }
        }

        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.WithReconfiguredChildSerializer(IBsonSerializer childSerializer)
        {
            return WithItemSerializer((IBsonSerializer<TItem>)childSerializer);
        }

        IBsonSerializer IBsonSerializerWithItemSerializer.ItemSerializer
        {
            get { return ItemSerializer; }
        }

        IBsonSerializer IBsonSerializerWithItemSerializer.WithItemSerializer(IBsonSerializer itemSerializer)
        {
            return WithItemSerializer((IBsonSerializer<TItem>)itemSerializer);
        }
    }
}
