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
        private WriteConcernExceptionState _state;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoWriteConcernException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="result">The result.</param>
        public MongoWriteConcernException(string message, WriteConcernResult result) 
            : base(message, result.Response) 
        {
            _state = new WriteConcernExceptionState { Result = result };

            SerializeObjectState += (sender, e) => e.AddSerializedState(_state);
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
            _state = new WriteConcernExceptionState { Result = result };

            SerializeObjectState += (sender, e) => e.AddSerializedState(_state);
        }

        // public properties
        /// <summary>
        /// Gets the result.
        /// </summary>
        public WriteConcernResult Result
        {
            get { return _state.Result; }
        }

        // nested structs
        private struct WriteConcernExceptionState : ISafeSerializationData
        {
            private WriteConcernResult _result;

            public WriteConcernResult Result
            {
                get { return _result; }
                set { _result = value; }
            }

            public void CompleteDeserialization(object deserialized)
            {
                var ex = deserialized as MongoWriteConcernException;
                ex._state = this;
            }
        }
    }
}