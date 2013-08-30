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
    /// Represents all the contextual information needed by a serializer to serialize a value.
    /// </summary>
    public class SerializationContext
    {
        // private fields
        private readonly Type _nominalType;
        private readonly SerializationContext _parent;
        private readonly bool _serializeAsNominalType;
        private readonly bool _serializeIdFirst;
        private readonly BsonWriter _writer;

        // constructors
        private SerializationContext(
            SerializationContext parent,
            BsonWriter writer,
            Type nominalType,
            bool serializeAsNominalType,
            bool serializeIdFirst)
        {
            _parent = parent;
            _writer = writer;
            _nominalType = nominalType;
            _serializeAsNominalType = serializeAsNominalType;
            _serializeIdFirst = serializeIdFirst;
        }

        // public properties
        /// <summary>
        /// Gets the nominal type.
        /// </summary>
        /// <value>
        /// The nominal type.
        /// </value>
        public Type NominalType
        {
            get { return _nominalType; }
        }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        /// <value>
        /// The parent context. The parent of the root context is null.
        /// </value>
        public SerializationContext Parent
        {
            get { return _parent; }
        }

        /// <summary>
        /// Gets a value indicating whether to serialize the value as if it were an instance of the nominal type.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the value should be serialized as if it were an instance of the nominal type; otherwise, <c>false</c>.
        /// </value>
        public bool SerializeAsNominalType
        {
            get { return _serializeAsNominalType; }
        }

        /// <summary>
        /// Gets a value indicating whether to serialize the id first.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the id should be serialized first; otherwise, <c>false</c>.
        /// </value>
        public bool SerializeIdFirst
        {
            get { return _serializeIdFirst; }
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        /// <value>
        /// The writer.
        /// </value>
        public BsonWriter Writer
        {
            get { return _writer; }
        }

        // public static methods
        /// <summary>
        /// Creates a root context.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type.</typeparam>
        /// <param name="writer">The writer.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A root context.
        /// </returns>
        public static SerializationContext CreateRoot<TNominalType>(
            BsonWriter writer,
            Action<Builder> configurator = null)
        {
            var builder = new Builder(null, writer, typeof(TNominalType));
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        /// <summary>
        /// Creates a root context.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A root context.
        /// </returns>
        public static SerializationContext CreateRoot(
            BsonWriter writer,
            Type nominalType,
            Action<Builder> configurator = null)
        {
            var builder = new Builder(null, writer, nominalType);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        // public methods
        /// <summary>
        /// Creates a child context.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type.</typeparam>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A child context.
        /// </returns>
        public SerializationContext CreateChild<TNominalType>(
            Action<Builder> configurator = null)
        {
            var builder = new Builder(this, _writer, typeof(TNominalType));
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        /// <summary>
        /// Creates a child context.
        /// </summary>
        /// <param name="nominalType">The nominal typel.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A child context.
        /// </returns>
        public SerializationContext CreateChild(
            Type nominalType,
            Action<Builder> configurator = null)
        {
            var builder = new Builder(this, _writer, nominalType);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        /// <summary>
        /// Creates a child context and calls the Serializer method of the serializer with it.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="configurator">The configurator.</param>
        public void SerializeWithChildContext<TNominalType>(
            IBsonSerializer<TNominalType> serializer,
            TNominalType value,
            Action<Builder> configurator = null)
        {
            var childContext = CreateChild<TNominalType>(configurator);
            serializer.Serialize(childContext, value);
        }

        /// <summary>
        /// Creates a child context and calls the Serializer method of the serializer with it.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="configurator">The configurator.</param>
        public void SerializeWithChildContext(
            IBsonSerializer serializer,
            object value,
            Action<Builder> configurator = null)
        {
            var childContext = CreateChild(serializer.ValueType, configurator);
            serializer.Serialize(childContext, value);
        }

        // nested classes
        /// <summary>
        /// Represents a builder for a SerializationContext.
        /// </summary>
        public class Builder
        {
            // private fields
            private Type _nominalType;
            private SerializationContext _parent;
            private bool _serializeAsNominalType;
            private bool _serializeIdFirst;
            private BsonWriter _writer;

            // constructors
            internal Builder(SerializationContext parent, BsonWriter writer, Type nominalType)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException("writer");
                }
                if (nominalType == null)
                {
                    throw new ArgumentNullException("nominalType");
                }

                _parent = parent;
                _writer = writer;
                _nominalType = nominalType;
            }

            // properties
            /// <summary>
            /// Gets the nominal type.
            /// </summary>
            /// <value>
            /// The nominal type.
            /// </value>
            public Type NominalType
            {
                get { return _nominalType; }
            }

            /// <summary>
            /// Gets the parent.
            /// </summary>
            /// <value>
            /// The parent.
            /// </value>
            public SerializationContext Parent
            {
                get { return _parent; }
            }

            /// <summary>
            /// Gets or sets a value indicating whether to serialize the value as if it were an instance of the nominal type.
            /// </summary>
            /// <value>
            ///   <c>true</c> if the value should be serialized as if it were an instance of the nominal type; otherwise, <c>false</c>.
            /// </value>
            public bool SerializeAsNominalType
            {
                get { return _serializeAsNominalType; }
                set { _serializeAsNominalType = value; }
            }

            /// <summary>
            /// Gets or sets a value indicating whether to serialize the id first.
            /// </summary>
            /// <value>
            ///   <c>true</c> if the id should be serialized first]; otherwise, <c>false</c>.
            /// </value>
            public bool SerializeIdFirst
            {
                get { return _serializeIdFirst; }
                set { _serializeIdFirst = value; }
            }

            /// <summary>
            /// Gets the writer.
            /// </summary>
            /// <value>
            /// The writer.
            /// </value>
            public BsonWriter Writer
            {
                get { return _writer; }
            }

            // public methods
            /// <summary>
            /// Builds the SerializationContext instance.
            /// </summary>
            /// <returns>A SerializationContext.</returns>
            public SerializationContext Build()
            {
                return new SerializationContext(_parent, _writer, _nominalType, _serializeAsNominalType, _serializeIdFirst);
            }
        }
    }
}
