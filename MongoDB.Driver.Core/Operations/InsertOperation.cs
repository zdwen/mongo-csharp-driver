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
using System.Diagnostics;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Executes an insert.
    /// </summary>
    public sealed class InsertOperation<TDocument> : WriteOperationBase<IEnumerable<WriteConcernResult>>
    {
        // private fields
        private bool _checkInsertDocuments;
        private IEnumerable<TDocument> _documents;
        private InsertFlags _flags;
        private int _maxMessageSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertOperation{TDocument}" /> class.
        /// </summary>
        public InsertOperation()
        {
            _checkInsertDocuments = true;
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to check the documents.  What does this mean?
        /// </summary>
        public bool CheckInsertDocuments
        {
            get { return _checkInsertDocuments; }
            set { _checkInsertDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the documents.
        /// </summary>
        public IEnumerable<TDocument> Documents
        {
            get { return _documents; }
            set { _documents = value; }
        }

        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        public InsertFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Gets or sets the maximum ,essage size for each batch.
        /// </summary>
        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = value; }
        }

        // public methods
        /// <summary>
        /// Executes the insert.
        /// </summary>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is not enabled).</returns>
        public override IEnumerable<WriteConcernResult> Execute()
        {
            EnsureRequiredProperties();
            using (__trace.TraceActivity("InsertOperation"))
            using (var channelProvider = CreateServerChannelProvider(WritableServerSelector.Instance, false))
            {
                if (__trace.Switch.ShouldTrace(TraceEventType.Verbose))
                {
                    __trace.TraceVerbose("inserting into collection {0} at {1}.", Collection.FullName, channelProvider.Server.DnsEndPoint);
                }

                var protocol = new InsertProtocol<TDocument>(
                    checkInsertDocuments: _checkInsertDocuments,
                    collection: Collection,
                    documents: _documents,
                    flags: _flags,
                    maxMessageSize: (_maxMessageSize != 0) ? _maxMessageSize : channelProvider.Server.MaxMessageSize,
                    readerSettings: GetServerAdjustedReaderSettings(channelProvider.Server),
                    writeConcern: WriteConcern,
                    writerSettings: GetServerAdjustedWriterSettings(channelProvider.Server));

                using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
                {
                    try
                    {
                        return protocol.Execute(channel);
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        Exception newException;
                        if(TryMapException(ex, out newException))
                        {
                            throw newException;
                        }

                        throw;
                    }
                }
            }
        }
    }
}
