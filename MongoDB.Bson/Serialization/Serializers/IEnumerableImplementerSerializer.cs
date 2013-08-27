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
using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class IEnumerableImplementerSerializer<TValue> :
        IEnumerableSerializerBase<TValue>,
        IBsonSerializerWithConfigurableChildSerializer,
        IBsonSerializerWithItemSerializer
            where TValue : class, IList, new()
    {
        // constructors
        public IEnumerableImplementerSerializer()
        {
        }

        public IEnumerableImplementerSerializer(IBsonSerializer itemSerializer)
            : base(itemSerializer)
        {
        }

        // public methods
        public IEnumerableImplementerSerializer<TValue> WithItemSerializer(IBsonSerializer itemSerializer)
        {
            if (itemSerializer == ItemSerializer)
            {
                return this;
            }
            else
            {
                return new IEnumerableImplementerSerializer<TValue>(itemSerializer);
            }
        }

        // protected methods
        protected override object CreateAccumulator()
        {
            return new TValue();
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.ConfigurableChildSerializer
        {
            get { return ItemSerializer; }
        }

        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.WithReconfiguredChildSerializer(IBsonSerializer childSerializer)
        {
            return WithItemSerializer(childSerializer);
        }

        IBsonSerializer IBsonSerializerWithItemSerializer.WithItemSerializer(IBsonSerializer itemSerializer)
        {
            return WithItemSerializer(itemSerializer);
        }
    }

    public class IEnumerableImplementerSerializer<TValue, TItem> : 
        IEnumerableSerializerBase<TValue, TItem>,
        IBsonSerializerWithConfigurableChildSerializer,
        IBsonSerializerWithItemSerializer<IEnumerableImplementerSerializer<TValue, TItem>, TValue, TItem>
            where TValue : class, IEnumerable<TItem>
    {
        // constructors
        public IEnumerableImplementerSerializer()
        {
        }

        public IEnumerableImplementerSerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        {
        }

        // public methods
        public IEnumerableImplementerSerializer<TValue, TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            if (itemSerializer == ItemSerializer)
            {
                return this;
            }
            else
            {
                return new IEnumerableImplementerSerializer<TValue, TItem>(itemSerializer);
            }
        }

        // protected methods
        protected override object CreateAccumulator()
        {
            return new List<TItem>();
        }

        protected override TValue FinalizeResult(object accumulator)
        {
            // find and call a constructor that we can pass the accumulator to
            var accumulatorType = accumulator.GetType();
            foreach (var constructorInfo in typeof(TValue).GetConstructors())
            {
                var parameterInfos = constructorInfo.GetParameters();
                if (parameterInfos.Length == 1 && parameterInfos[0].ParameterType.IsAssignableFrom(accumulatorType))
                {
                    return (TValue)constructorInfo.Invoke(new object[] { accumulator });
                }
            }

            // otherwise try to find a no-argument constructor and an Add method
            var noArgumentConstructorInfo = typeof(TValue).GetConstructor(new Type[] { });
            var addMethodInfo = typeof(TValue).GetMethod("Add", new Type[] { typeof(TItem) });
            if (noArgumentConstructorInfo != null && addMethodInfo != null)
            {
                var value = (TValue)noArgumentConstructorInfo.Invoke(new Type[] { });
                foreach (var item in (IEnumerable<TItem>)accumulator)
                {
                    addMethodInfo.Invoke(value, new object[] { item });
                }
                return value;
            }

            var message = string.Format("Type '{0}' does not have a suitable constructor or Add method.", typeof(TValue).FullName);
            throw new BsonSerializationException(message);
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
