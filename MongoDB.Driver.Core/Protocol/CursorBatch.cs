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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// The result of a protocol execution that returns a cursor.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class CursorBatch<TDocument>
    {
        // private fields
        private readonly long _cursorId;
        private readonly IEnumerable<TDocument> _documents;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CursorBatch{TDocument}" /> class.
        /// </summary>
        /// <param name="cursorId">The cursor id.</param>
        /// <param name="documents">The documents.</param>
        public CursorBatch(long cursorId, IEnumerable<TDocument> documents)
        {
            Ensure.IsNotNull("documents", documents);

            _cursorId = cursorId;
            _documents = documents;
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
        /// Gets the documents.
        /// </summary>
        public IEnumerable<TDocument> Documents
        {
            get { return _documents; }
        }
    }
}