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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents a Query message.
    /// </summary>
    public sealed class QueryMessage : RequestMessage
    {
        // private fields
        private readonly QueryFlags _flags;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly int _numberToReturn;
        private readonly int _numberToSkip;
        private readonly object _query;
        private readonly object _returnFieldSelector;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryMessage" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The namespace.</param>
        /// <param name="query">The query.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="numberToSkip">The number to skip.</param>
        /// <param name="numberToReturn">The number to return.</param>
        /// <param name="returnFieldSelector">The return field selector.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public QueryMessage(CollectionNamespace collectionNamespace, object query, QueryFlags flags, int numberToSkip, int numberToReturn, object returnFieldSelector, BsonBinaryWriterSettings writerSettings)
            : base(OpCode.Query)
        {
            Ensure.IsNotNull("collectionNamespace", collectionNamespace);
            Ensure.IsNotNull("query", query);
            Ensure.IsNotNull("writerSettings", writerSettings);
            // NOTE: returnFieldSelector is allowed to be null as it is not required by the protocol

            _collectionNamespace = collectionNamespace;
            _flags = flags;
            _numberToSkip = numberToSkip;
            _numberToReturn = numberToReturn;
            _returnFieldSelector = returnFieldSelector;
            _query = query;
            _writerSettings = writerSettings;
        }

        // protected methods
        /// <summary>
        /// Writes the body of the message a stream.
        /// </summary>
        /// <param name="streamWriter">The stream.</param>
        protected override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32((int)_flags);
            streamWriter.WriteCString(_collectionNamespace.FullName);
            streamWriter.WriteInt32(_numberToSkip);
            streamWriter.WriteInt32(_numberToReturn);

            using (var bsonWriter = new BsonBinaryWriter(streamWriter.BaseStream, _writerSettings))
            {
                // TODO: pass in a serializer?
                BsonSerializer.Serialize(bsonWriter, _query.GetType(), _query, null);

                if (_returnFieldSelector != null)
                {
                    // TODO: pass in a serializer?
                    BsonSerializer.Serialize(bsonWriter, _returnFieldSelector.GetType(), _returnFieldSelector, null); // returnFieldSelector
                }
            }
        }
    }
}