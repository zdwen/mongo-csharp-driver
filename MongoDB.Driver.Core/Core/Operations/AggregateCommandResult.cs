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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Operations.Serializers;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// The result of an aggregate command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Serializable]
    [BsonSerializer(typeof(AggregateCommandResultSerializer<>))]
    public class AggregateCommandResult<TDocument> : CommandResult
    {
        // private fields
        private readonly long _cursorId;
        private readonly IEnumerable<TDocument> _firstBatch;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateCommandResult{TDocument}" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cursorId">The cursor id.</param>
        /// <param name="firstBatch">The values.</param>
        public AggregateCommandResult(BsonDocument response, long cursorId, IEnumerable<TDocument> firstBatch)
            : base(response)
        {
            _cursorId = cursorId;
            _firstBatch = firstBatch;
        }

        // public properties
        /// <summary>
        /// Gets the cursor id.
        /// </summary>
        public long CursorId
        {
            get { return _cursorId; }
        }

        /// <summary>
        /// Gets the first batch.
        /// </summary>
        public IEnumerable<TDocument> FirstBatch
        {
            get { return _firstBatch; }
        }
    }
}