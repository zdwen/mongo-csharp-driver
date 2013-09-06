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
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// An exception thrown during login and privilege negotiation.
    /// </summary>
    [Serializable]
    public class MongoAuthenticationException : MongoOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuthenticationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="response">The response.</param>
        public MongoAuthenticationException(string message, BsonDocument response) 
            : base(message, response) 
        { 
        }
    }
}
