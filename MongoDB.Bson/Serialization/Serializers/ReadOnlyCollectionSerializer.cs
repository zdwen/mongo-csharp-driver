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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class ReadOnlyCollectionSerializer<TItem> :
        IEnumerableSerializerBase<ReadOnlyCollection<TItem>, TItem>,
        IBsonSerializerWithItemSerializer<ReadOnlyCollectionSerializer<TItem>, ReadOnlyCollection<TItem>, TItem>
    {
        // constructors
        public ReadOnlyCollectionSerializer()
        {
        }

        public ReadOnlyCollectionSerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        {
        }

        // public methods
        public ReadOnlyCollectionSerializer<TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            if (itemSerializer == ItemSerializer)
            {
                return this;
            }
            else
            {
                return new ReadOnlyCollectionSerializer<TItem>(itemSerializer);
            }
        }

        // protected methods
        protected override object CreateAccumulator()
        {
            return new List<TItem>();
        }

        protected override ReadOnlyCollection<TItem> FinalizeResult(object accumulator)
        {
            return ((List<TItem>)accumulator).AsReadOnly();
        }

        // explicit interface implementations
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
