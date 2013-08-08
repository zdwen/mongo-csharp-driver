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
using System.Linq;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Base class for write operations.
    /// </summary>
    /// <typeparam name="TOperationResult">The type of the result.</typeparam>
    public abstract class WriteOperationBase<TOperationResult> : OperationBase<TOperationResult>
    {
        // private static fields
        private static readonly int[] __duplicateKeyCodes = new[] { 11000, 11001, 12582 };

        // private fields
        private CollectionNamespace _collection;
        private WriteConcern _writeConcern;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOperationBase{TOperationResult}" /> class.
        /// </summary>
        public WriteOperationBase()
        {
            _writeConcern = WriteConcern.Acknowledged;
        }

        // public properties
        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        public CollectionNamespace Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        // protected methods
        /// <summary>
        /// Ensures that required properties have been set or provides intelligent defaults.
        /// </summary>
        protected override void EnsureRequiredProperties()
        {
            base.EnsureRequiredProperties();
            Ensure.IsNotNull("Collection", _collection);
            Ensure.IsNotNull("WriteConcern", _writeConcern);
        }

        /// <summary>
        /// Maps the exception to a more specific version if it matches.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="newException">The new exception.</param>
        /// <returns><c>true></c> if the exception was mapped; otherwise <c>false</c></returns>
        protected bool TryMapException(MongoWriteConcernException exception, out Exception newException)
        {
            if (exception.Result.Code.HasValue && __duplicateKeyCodes.Contains(exception.Result.Code.Value))
            {
                newException = new MongoDuplicateKeyException(
                    "An attempt was made to insert a record with a duplicate key.", 
                    exception.Result, 
                    exception);
                return true;
            }

            newException = null;
            return false;
        }
    }
}