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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Interfaces.
    /// </summary>
    public class ImpliedImplementationInterfaceSerializer<TInterface, TImplementation> :
        BsonBaseSerializer<TInterface>,
        IBsonSerializerWithConfigurableChildSerializer
            where TImplementation : class, TInterface
    {
        // private fields
        private readonly IBsonSerializer<TImplementation> _implementationSerializer;

        // constructors
        public ImpliedImplementationInterfaceSerializer()
            : this(BsonSerializer.LookupSerializer<TImplementation>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImpliedImplementationInterfaceSerializer{TInterface, TImplementation}"/> class.
        /// </summary>
        /// <param name="implementationSerializer">The implementation serializer.</param>
        public ImpliedImplementationInterfaceSerializer(IBsonSerializer<TImplementation> implementationSerializer)
        {
            if (!typeof(TInterface).IsInterface)
            {
                var message = string.Format("{0} is not an interface.", typeof(TInterface).FullName);
                throw new ArgumentException(message, "<TInterface>");
            }

            _implementationSerializer = implementationSerializer;
        }

        // public properties
        public IBsonSerializer<TImplementation> ImplementationSerializer
        {
            get { return _implementationSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes a document from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <returns>
        /// A document.
        /// </returns>
        /// <exception cref="System.FormatException"></exception>
        public override TInterface Deserialize(DeserializationContext context)
        {
            var bsonReader = context.Reader;

            if (bsonReader.GetCurrentBsonType() == BsonType.Null)
            {
                bsonReader.ReadNull();
                return default(TInterface);
            }
            else
            {
                return _implementationSerializer.Deserialize(context);
            }
        }

        /// <summary>
        /// Serializes a document to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The document.</param>
        public override void Serialize(SerializationContext context, TInterface value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType == typeof(TImplementation))
                {
                    context.SerializeWithChildContext(_implementationSerializer, (TImplementation)value);
                }
                else
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    context.SerializeWithChildContext(serializer, value);
                }
            }
        }

        public ImpliedImplementationInterfaceSerializer<TInterface, TImplementation> WithImplementationSerializer(IBsonSerializer<TImplementation> implementationSerializer)
        {
            if (implementationSerializer == ImplementationSerializer)
            {
                return this;
            }
            else
            {
                return new ImpliedImplementationInterfaceSerializer<TInterface, TImplementation>(implementationSerializer);
            }
        }

        // explicit interface implementations
        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.ConfigurableChildSerializer
        {
            get { return _implementationSerializer; }
        }

        IBsonSerializer IBsonSerializerWithConfigurableChildSerializer.WithReconfiguredChildSerializer(IBsonSerializer childSerializer)
        {
            return WithImplementationSerializer((IBsonSerializer<TImplementation>)childSerializer);
        }
    }
}