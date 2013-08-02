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

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// The result of an aggregate command.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [Serializable]
    [BsonSerializer(typeof(AggregateCommandResultSerializer<>))]
    public class AggregateCommandResult<TResult> : CommandResult
    {
        // private fields
        private readonly long _cursorId;
        private readonly IEnumerable<TResult> _results;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateCommandResult{TResult}" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cursorId">The cursor id.</param>
        /// <param name="results">The values.</param>
        internal AggregateCommandResult(BsonDocument response, long cursorId, IEnumerable<TResult> results)
            : base(response)
        {
            _cursorId = cursorId;
            _results = results;
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
        /// Gets the values.
        /// </summary>
        public IEnumerable<TResult> Results
        {
            get { return _results; }
        }
    }
}