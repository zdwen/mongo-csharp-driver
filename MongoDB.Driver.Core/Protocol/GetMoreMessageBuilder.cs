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

using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Builds a <see cref="BsonBufferedRequestMessage" /> to get more documents.
    /// </summary>
    public sealed class GetMoreMessageBuilder : BsonBufferedRequestMessageBuilder
    {
        // private fields
        private readonly long _cursorId;
        private readonly MongoNamespace _namespace;
        private readonly int _numberToReturn;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMoreMessageBuilder" /> class.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="numberToReturn">The number to return.</param>
        /// <param name="cursorId">The cursor id.</param>
        public GetMoreMessageBuilder(MongoNamespace @namespace, int numberToReturn, long cursorId)
            : base(OpCode.GetMore)
        {
            _namespace = @namespace;
            _numberToReturn = numberToReturn;
            _cursorId = cursorId;
        }

        // protected methods
        /// <summary>
        /// Writes the message to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected override void Write(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // ZERO
            buffer.WriteCString(__encoding, _namespace.FullName); // fullCollectionName
            buffer.WriteInt32(_numberToReturn); // numberToReturn
            buffer.WriteInt64(_cursorId); // cursorID
        }
    }
}