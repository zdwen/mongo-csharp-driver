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
using MongoDB.Driver.Core;

namespace MongoDB.Driver
{
    /// <summary>
    /// Thrown when an error was returned due to a failed write concern.
    /// </summary>
    [Serializable]
    public class MongoWriteConcernException : MongoOperationException
    {
        // private fields
        [NonSerialized]
        private WriteConcernResult _result;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoWriteConcernException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="result">The result.</param>
        public MongoWriteConcernException(string message, WriteConcernResult result) 
            : base(message, result.Response) 
        {
            _result = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoWriteConcernException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="result">The result.</param>
        /// <param name="inner">The inner.</param>
        public MongoWriteConcernException(string message, WriteConcernResult result, Exception inner)
            : base(message, result.Response, inner) 
        {
            _result = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoWriteConcernException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected MongoWriteConcernException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            _result = (WriteConcernResult)info.GetValue("_result", typeof(WriteConcernResult));
        }

        // public properties
        /// <summary>
        /// Gets the result.
        /// </summary>
        public WriteConcernResult Result
        {
            get { return _result; }
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
            info.AddValue("_result", _result);
        }
    }
}