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
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a base serializer for enumerable values.
    /// </summary>
    public abstract class EnumerableSerializerBase<TValue> : BsonBaseSerializer<TValue>, IBsonArraySerializer where TValue : class, IEnumerable
    {
        // private fields
        private readonly IBsonSerializer _itemSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableSerializerBase{TValue}"/> class.
        /// </summary>
        protected EnumerableSerializerBase()
            : this(BsonSerializer.LookupSerializer(typeof(object)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableSerializerBase{TValue}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        protected EnumerableSerializerBase(IBsonSerializer itemSerializer)
        {
            if (itemSerializer == null)
            {
                throw new ArgumentNullException("itemSerializer");
            }

            _itemSerializer = itemSerializer;
        }

        public IBsonSerializer ItemSerializer
        {
            get { return _itemSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <returns>An object.</returns>
        public override TValue Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var accumulator = CreateAccumulator();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var item = context.DeserializeWithChildContext(_itemSerializer);
                        AddItem(accumulator, item);
                    }
                    bsonReader.ReadEndArray();
                    return FinalizeResult(accumulator);

                default:
                    var message = string.Format("Can't deserialize a {0} from BsonType {1}.", typeof(TValue).FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            string elementName = null;
            return new BsonSerializationInfo(elementName, _itemSerializer, _itemSerializer.ValueType);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, TValue value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteStartArray();
                foreach (var item in EnumerateItemsInSerializationOrder(value))
                {
                    context.SerializeWithChildContext(_itemSerializer, item);
                }
                bsonWriter.WriteEndArray();
            }
        }

        // protected methods
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <param name="item">The item.</param>
        protected abstract void AddItem(object accumulator, object item);

        /// <summary>
        /// Creates the accumulator.
        /// </summary>
        /// <returns>The accumulator.</returns>
        protected abstract object CreateAccumulator();

        /// <summary>
        /// Enumerates the items in serialization order.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The items.</returns>
        protected abstract IEnumerable EnumerateItemsInSerializationOrder(TValue value);

        /// <summary>
        /// Finalizes the result.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <returns>The final result.</returns>
        protected abstract TValue FinalizeResult(object accumulator);
    }

    /// <summary>
    /// Represents a serializer for enumerable values.
    /// </summary>
    /// <typeparam name="TItem">The type of the elements.</typeparam>
    public abstract class EnumerableSerializerBase<TValue, TItem> : BsonBaseSerializer<TValue>, IBsonArraySerializer where TValue : class, IEnumerable<TItem>
    {
        // private fields
        private readonly IDiscriminatorConvention _discriminatorConvention = new ScalarDiscriminatorConvention("_t");
        private readonly IBsonSerializer<TItem> _itemSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableSerializerBase{TValue, TItem}"/> class.
        /// </summary>
        protected EnumerableSerializerBase()
            : this(BsonSerializer.LookupSerializer<TItem>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableSerializerBase{TValue, TItem}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        protected EnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
        {
            if (itemSerializer == null)
            {
                throw new ArgumentNullException("itemSerializer");
            }

            _itemSerializer = itemSerializer;
        }

        // public properties
        public IBsonSerializer<TItem> ItemSerializer
        {
            get { return _itemSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <returns>An object.</returns>
        public override TValue Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var accumulator = CreateAccumulator();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var item = context.DeserializeWithChildContext(_itemSerializer);
                        AddItem(accumulator, item);
                    }
                    bsonReader.ReadEndArray();
                    return FinalizeResult(accumulator);

                case BsonType.Document:
                    return DeserializeDiscriminatedWrapper(context, _discriminatorConvention);

                default:
                    var message = string.Format("Can't deserialize a {0} from BsonType {1}.", typeof(TValue).FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            string elementName = null;
            return new BsonSerializationInfo(elementName, _itemSerializer, _itemSerializer.ValueType);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, TValue value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType == context.NominalType)
                {
                    bsonWriter.WriteStartArray();
                    foreach (var item in EnumerateItemsInSerializationOrder(value))
                    {
                        context.SerializeWithChildContext(_itemSerializer, item);
                    }
                    bsonWriter.WriteEndArray();
                }
                else
                {
                    SerializeDiscriminatedWrapper(context, value, _discriminatorConvention);
                }
            }
        }

        // protected methods
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <param name="item">The item.</param>
        protected abstract void AddItem(object accumulator, TItem item);

        /// <summary>
        /// Creates the accumulator.
        /// </summary>
        /// <returns>The accumulator.</returns>
        protected abstract object CreateAccumulator();

        /// <summary>
        /// Enumerates the items in serialization order.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The items.</returns>
        protected abstract IEnumerable<TItem> EnumerateItemsInSerializationOrder(TValue value);

        /// <summary>
        /// Finalizes the result.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <returns>The result.</returns>
        protected abstract TValue FinalizeResult(object accumulator);
    }
}

