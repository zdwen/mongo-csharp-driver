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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Sessions;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Executes an insert.
    /// </summary>
    public sealed class InsertOperation : WriteOperationBase<IEnumerable<WriteConcernResult>>
    {
        // private fields
        private bool _assignIdOnInsert;
        private bool _checkInsertDocuments;
        private Type _documentType;
        private IEnumerable _documents;
        private InsertFlags _flags;
        private int _maxMessageSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertOperation" /> class.
        /// </summary>
        public InsertOperation()
        {
            _assignIdOnInsert = true;
            _checkInsertDocuments = true;
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to assign an id to documents missing them.
        /// </summary>
        public bool AssignIdOnInsert
        {
            get { return _assignIdOnInsert; }
            set { _assignIdOnInsert = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to check the documents.  What does this mean?
        /// </summary>
        public bool CheckInsertDocuments
        {
            get { return _checkInsertDocuments; }
            set { _checkInsertDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public Type DocumentType
        {
            get { return _documentType; }
            set { _documentType = value; }
        }

        /// <summary>
        /// Gets or sets the documents.
        /// </summary>
        public IEnumerable Documents
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
            using (var channelProvider = CreateServerChannelProvider(WritableServerSelector.Instance, false))
            {
                var protocol = new InsertProtocol(
                    checkInsertDocuments: _checkInsertDocuments,
                    collection: Collection,
                    documentType: _documentType,
                    documents: PrepareDocuments(),
                    flags: _flags,
                    maxMessageSize: (_maxMessageSize != 0) ? _maxMessageSize : channelProvider.Server.MaxMessageSize,
                    readerSettings: GetServerAdjustedReaderSettings(channelProvider.Server),
                    writeConcern: WriteConcern,
                    writerSettings: GetServerAdjustedWriterSettings(channelProvider.Server));

                using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
                {
                    return protocol.Execute(channel);
                }
            }
        }

        // private methods
        private IEnumerable PrepareDocuments()
        {
            foreach (var document in _documents)
            {
                PrepareDocument(document);
                yield return document;
            }
        }

        private void PrepareDocument(object document)
        {
            // Perhaps the caller should pass in a delegate to prepare the documents in any way they see fit?
            // this code would then move to the delegate provided by the caller
            if (_assignIdOnInsert)
            {
                var serializer = BsonSerializer.LookupSerializer(document.GetType());
                var idProvider = serializer as IBsonIdProvider;
                if (idProvider != null)
                {
                    object id;
                    Type idNominalType;
                    IIdGenerator idGenerator;
                    if (idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator))
                    {
                        if (idGenerator != null && idGenerator.IsEmpty(id))
                        {
                            id = idGenerator.GenerateId(this, document);
                            idProvider.SetDocumentId(document, id);
                        }
                    }
                }
            }
        }
    }
}
