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

using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class IDictionaryImplementerSerializer<TDictionary> :
        DictionarySerializerBase<TDictionary>,
        IBsonSerializerWithConfigurableChildSerializer,
        IBsonSerializerWithDictionaryRepresentation,
        IBsonSerializerWithKeyValueSerializers
            where TDictionary : class, IDictionary, new()
    {
        public IDictionaryImplementerSerializer()
        {
        }

        public IDictionaryImplementerSerializer(DictionaryRepresentation dictionaryRepresentation)
            : base(dictionaryRepresentation)
        {
        }

        public IDictionaryImplementerSerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
            : base(dictionaryRepresentation, keySerializer, valueSerializer)
        {
        }

        // public methods
        public IDictionaryImplementerSerializer<TDictionary> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation)
        {
            if (dictionaryRepresentation == DictionaryRepresentation)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary>(dictionaryRepresentation, KeySerializer, ValueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            if (dictionaryRepresentation == DictionaryRepresentation && keySerializer == KeySerializer && valueSerializer == ValueSerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary>(dictionaryRepresentation, keySerializer, valueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary> WithKeySerializer(IBsonSerializer keySerializer)
        {
            if (keySerializer == KeySerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary>(DictionaryRepresentation, keySerializer, ValueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary> WithValueSerializer(IBsonSerializer valueSerializer)
        {
            if (valueSerializer == ValueSerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary>(DictionaryRepresentation, KeySerializer, valueSerializer);
            }
        }

        // protected methods
        protected override TDictionary CreateInstance()
        {
            return new TDictionary();
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.ConfigurableChildSerializer
        {
            get { return ValueSerializer; }
        }

        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.WithReconfiguredChildSerializer(IBsonSerializer childSerializer)
        {
            return WithValueSerializer(childSerializer);
        }

        IBsonSerializer IBsonSerializerWithDictionaryRepresentation.WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation)
        {
            return WithDictionaryRepresentation(dictionaryRepresentation);
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.WithKeySerializer(IBsonSerializer keySerializer)
        {
            return WithKeySerializer(keySerializer);
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.WithValueSerializer(IBsonSerializer valueSerializer)
        {
            return WithValueSerializer(valueSerializer);
        }
    }

    public class IDictionaryImplementerSerializer<TDictionary, TKey, TValue> :
        DictionarySerializerBase<TDictionary, TKey, TValue>,
        IBsonSerializerWithConfigurableChildSerializer,
        IBsonSerializerWithDictionaryRepresentation<IDictionaryImplementerSerializer<TDictionary, TKey, TValue>>,
        IBsonSerializerWithKeyValueSerializers<IDictionaryImplementerSerializer<TDictionary, TKey, TValue>, TDictionary, TKey, TValue>
            where TDictionary : class, IDictionary<TKey, TValue>, new()
    {
        public IDictionaryImplementerSerializer()
        {
        }

        public IDictionaryImplementerSerializer(DictionaryRepresentation dictionaryRepresentation)
            : base(dictionaryRepresentation)
        {
        }

        public IDictionaryImplementerSerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
            : base(dictionaryRepresentation, keySerializer, valueSerializer)
        {
        }

        // public methods
        public IDictionaryImplementerSerializer<TDictionary, TKey, TValue> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation)
        {
            if (dictionaryRepresentation == DictionaryRepresentation)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary, TKey, TValue>(dictionaryRepresentation, KeySerializer, ValueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary, TKey, TValue> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
        {
            if (dictionaryRepresentation == DictionaryRepresentation && keySerializer == KeySerializer && valueSerializer == ValueSerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary, TKey, TValue>(dictionaryRepresentation, keySerializer, valueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary, TKey, TValue> WithKeySerializer(IBsonSerializer<TKey> keySerializer)
        {
            if (keySerializer == KeySerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary, TKey, TValue>(DictionaryRepresentation, keySerializer, ValueSerializer);
            }
        }

        public IDictionaryImplementerSerializer<TDictionary, TKey, TValue> WithValueSerializer(IBsonSerializer<TValue> valueSerializer)
        {
            if (valueSerializer == ValueSerializer)
            {
                return this;
            }
            else
            {
                return new IDictionaryImplementerSerializer<TDictionary, TKey, TValue>(DictionaryRepresentation, KeySerializer, valueSerializer);
            }
        }

        // protected methods
        protected override TDictionary CreateInstance()
        {
            return new TDictionary();
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.ConfigurableChildSerializer
        {
            get { return ValueSerializer; }
        }

        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.WithReconfiguredChildSerializer(IBsonSerializer childSerializer)
        {
            return WithValueSerializer((IBsonSerializer<TValue>)childSerializer);
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.KeySerializer
        {
            get { return KeySerializer; }
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.ValueSerializer
        {
            get { return ValueSerializer; }
        }

        IBsonSerializer IBsonSerializerWithDictionaryRepresentation.WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation)
        {
            return WithDictionaryRepresentation(dictionaryRepresentation);
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.WithKeySerializer(IBsonSerializer keySerializer)
        {
            return WithKeySerializer((IBsonSerializer<TKey>)keySerializer);
        }

        IBsonSerializer IBsonSerializerWithKeyValueSerializers.WithValueSerializer(IBsonSerializer valueSerializer)
        {
            return WithValueSerializer((IBsonSerializer<TValue>)valueSerializer);
        }
    }
}
