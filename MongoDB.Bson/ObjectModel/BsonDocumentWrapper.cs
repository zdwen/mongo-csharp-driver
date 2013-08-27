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
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson
{
    // this class is a wrapper for an object that we intend to serialize as a BsonValue
    // it is a subclass of BsonValue so that it may be used where a BsonValue is expected
    // this class is mostly used by MongoCollection and MongoCursor when supporting generic query objects

    /// <summary>
    /// Represents a BsonDocument wrapper.
    /// </summary>
    public class BsonDocumentWrapper : BsonValue
    {
        // private fields
        private readonly object _wrapped;
        private readonly IBsonSerializer _serializer;
        private readonly bool _isUpdateDocument;

        // constructors
        public BsonDocumentWrapper(object value)
            : this(value, UndiscriminatedActualTypeSerializer.Instance)
        {
        }

        public BsonDocumentWrapper(object value, IBsonSerializer serializer)
            : this(value, serializer, false)
        {
        }

        public BsonDocumentWrapper(object value, IBsonSerializer serializer, bool isUpdateDocument)
            : base(BsonType.Document)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            _wrapped = value;
            _serializer = serializer;
            _isUpdateDocument = isUpdateDocument;
        }

        // public properties
        /// <summary>
        /// Gets whether the wrapped value is an update document.
        /// </summary>
        public bool IsUpdateDocument
        {
            get { return _isUpdateDocument; }
        }

        public IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public object Wrapped
        {
            get { return _wrapped; }
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped object.</typeparam>
        /// <param name="value">The wrapped object.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create<TNominalType>(TNominalType value)
        {
            return Create(typeof(TNominalType), value);
        }

        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped object.</typeparam>
        /// <param name="value">The wrapped object.</param>
        /// <param name="isUpdateDocument">Whether the wrapped object is an update document.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create<TNominalType>(TNominalType value, bool isUpdateDocument)
        {
            return Create(typeof(TNominalType), value, isUpdateDocument);
        }

        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="value">The wrapped object.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create(Type nominalType, object value)
        {
            return Create(nominalType, value, false); // isUpdateDocument = false
        }

        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="value">The wrapped object.</param>
        /// <param name="isUpdateDocument">Whether the wrapped object is an update document.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create(Type nominalType, object value, bool isUpdateDocument)
        {
            var serializer = BsonSerializer.LookupSerializer(nominalType);
            return new BsonDocumentWrapper(value, serializer, isUpdateDocument);
        }

        /// <summary>
        /// Creates a list of new instances of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped objects.</typeparam>
        /// <param name="values">A list of wrapped objects.</param>
        /// <returns>A list of BsonDocumentWrappers.</returns>
        public static IEnumerable<BsonDocumentWrapper> CreateMultiple<TNominalType>(IEnumerable<TNominalType> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializer = BsonSerializer.LookupSerializer(typeof(TNominalType));
            return values.Select(v => new BsonDocumentWrapper(v, serializer));
        }

        /// <summary>
        /// Creates a list of new instances of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="values">A list of wrapped objects.</param>
        /// <returns>A list of BsonDocumentWrappers.</returns>
        public static IEnumerable<BsonDocumentWrapper> CreateMultiple(Type nominalType, IEnumerable values)
        {
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializer = BsonSerializer.LookupSerializer(nominalType);
            return values.Cast<object>().Select(v => new BsonDocumentWrapper(v, serializer));
        }

        // public methods
        /// <summary>
        /// CompareTo is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="other">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public override int CompareTo(BsonValue other)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Equals is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="obj">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// GetHashCode is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <returns>Not applicable.</returns>
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a string representation of the wrapped value.
        /// </summary>
        /// <returns>A string representation of the wrapped value.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
