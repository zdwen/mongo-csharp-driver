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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer for TClass (a subclass of BsonDocumentBackedClass).
    /// </summary>
    /// <typeparam name="TClass">The subclass of BsonDocumentBackedClass.</typeparam>
    public abstract class BsonDocumentBackedClassSerializer<TClass> : BsonBaseSerializer<TClass>, IBsonDocumentSerializer
        where TClass : BsonDocumentBackedClass
    {
        // private fields
        private readonly Dictionary<string, BsonSerializationInfo> _memberSerializationInfo;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentBackedClassSerializer&lt;TClass&gt;"/> class.
        /// </summary>
        protected BsonDocumentBackedClassSerializer()
        {
            _memberSerializationInfo = new Dictionary<string, BsonSerializationInfo>();
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <returns>An object.</returns>
        public override TClass Deserialize(DeserializationContext context)
        {
            var backingDocument = BsonDocumentSerializer.Instance.Deserialize(context);
            return CreateInstance(backingDocument);
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>
        /// The serialization info for the member.
        /// </returns>
        public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            BsonSerializationInfo info;
            if (!_memberSerializationInfo.TryGetValue(memberName, out info))
            {
                var message = string.Format("{0} is not a member of {1}.", memberName, typeof(TClass));
                throw new ArgumentOutOfRangeException("memberName", message);
            }

            return info;
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(SerializationContext context, TClass value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var backingDocument = ((BsonDocumentBackedClass)value).BackingDocument;
                context.SerializeWithChildContext(BsonDocumentSerializer.Instance, backingDocument);
            }
        }

        // protected methods
        /// <summary>
        /// Registers a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <param name="elementName">The element name.</param>
        /// <param name="serializer">The serializer.</param>
        protected void RegisterMember(string memberName, string elementName, IBsonSerializer serializer)
        {
            if (memberName == null)
            {
                throw new ArgumentNullException("memberName");
            }
            if (elementName == null)
            {
                throw new ArgumentNullException("elementName");
            }
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            var info = new BsonSerializationInfo(elementName, serializer, serializer.ValueType);
            _memberSerializationInfo.Add(memberName, info);
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <returns>An instance of TClass.</returns>
        protected abstract TClass CreateInstance(BsonDocument backingDocument);
    }
}
