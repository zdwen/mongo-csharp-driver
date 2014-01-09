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
using System.Runtime.Serialization;
using System.Security;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// An exception representing a server error.
    /// </summary>
    [Serializable]
    public class MongoOperationException : MongoException
    {
        // private fields
        [NonSerialized]
        private BsonDocument _response;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoOperationException" /> class.
        /// </summary>
        public MongoOperationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoOperationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="response">The response.</param>
        public MongoOperationException(string message, BsonDocument response) 
            : base(message) 
        {
            _response = response;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoOperationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="response">The response.</param>
        /// <param name="inner">The inner.</param>
        public MongoOperationException(string message, BsonDocument response, Exception inner) 
            : base(message, inner) 
        {
            _response = response;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoOperationException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected MongoOperationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            _response = (BsonDocument)info.GetValue("_response", typeof(BsonDocument));
        }

        // public properties
        /// <summary>
        /// Gets the response from the server.
        /// </summary>
        public BsonDocument Response
        {
            get { return _response; }
        }

        // public methods
        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        ///   </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_response", _response);
        }
    }
}