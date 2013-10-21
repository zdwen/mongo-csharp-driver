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

using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Base class for operations that issue a query against the database.
    /// </summary>
    /// <typeparam name="TOperationResult">The type of the result.</typeparam>
    public abstract class QueryOperationBase<TOperationResult> : OperationBase<TOperationResult>
    {
        // protected methods
        /// <summary>
        /// Wraps the query.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>The query (possibly modified).</returns>
        protected BsonDocument WrapQuery(ServerDescription server, BsonDocument query, BsonDocument options, ReadPreference readPreference)
        {
            Ensure.IsNotNull("server", server);

            BsonDocument formattedReadPreference = null;
            if (server.Type == ServerType.ShardRouter && readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                BsonArray tagSetsArray = null;
                if (readPreference.TagSets != null)
                {
                    tagSetsArray = new BsonArray();
                    foreach (var tagSet in readPreference.TagSets)
                    {
                        var tagSetDocument = new BsonDocument();
                        foreach (var tag in tagSet)
                        {
                            tagSetDocument.Add(tag.Name, tag.Value);
                        }
                        tagSetsArray.Add(tagSetDocument);
                    }
                }

                if (tagSetsArray != null || readPreference.ReadPreferenceMode != ReadPreferenceMode.SecondaryPreferred)
                {
                    formattedReadPreference = new BsonDocument
                    {
                        { "mode", MongoUtils.ToCamelCase(readPreference.ReadPreferenceMode.ToString()) },
                        { "tags", tagSetsArray, tagSetsArray != null } // optional
                    };
                }
            }

            if (options == null && formattedReadPreference == null)
            {
                return query;
            }
            else
            {
                var wrappedQuery = new BsonDocument
                {
                    { "$query", query ?? new BsonDocument() },
                    { "$readPreference", formattedReadPreference, formattedReadPreference != null }, // only if sending query to a mongos
                };
                wrappedQuery.Merge(options);
                return wrappedQuery;
            }
        }
    }
}