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
        private OperationExceptionState _state;

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
            _state = new OperationExceptionState { Response = response };

            SerializeObjectState += (sender, e) => e.AddSerializedState(_state);
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
            _state = new OperationExceptionState { Response = response };

            SerializeObjectState += (sender, e) => e.AddSerializedState(_state);
        }

        // public properties
        /// <summary>
        /// Gets the response from the server.
        /// </summary>
        public BsonDocument Response
        {
            get { return _state.Response; }
        }

        // nested structs
        private struct OperationExceptionState : ISafeSerializationData
        {
            private BsonDocument _response;

            public BsonDocument Response
            {
                get { return _response; }
                set { _response = value; }
            }

            public void CompleteDeserialization(object deserialized)
            {
                var ex = deserialized as MongoOperationException;
                ex._state = this;
            }
        }

    }
}
