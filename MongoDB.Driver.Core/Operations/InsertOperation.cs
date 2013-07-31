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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a batch Insert operation.
    /// </summary>
    public class InsertOperation : WriteOperation<IEnumerable<WriteConcernResult>>
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
        /// Executes the Insert operation.
        /// </summary>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is not enabled).</returns>
        public override IEnumerable<WriteConcernResult> Execute()
        {
            ValidateRequiredProperties();

            using (var channelProvider = CreateServerChannelProvider(WritableServerSelector.Instance, false))
            using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
            {
                var maxMessageSize = (_maxMessageSize != 0) ? _maxMessageSize : channelProvider.Server.MaxMessageSize;
                var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
                var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);

                List<WriteConcernResult> results = (WriteConcern.Enabled) ? new List<WriteConcernResult>() : null;

                var continueOnError = (_flags & InsertFlags.ContinueOnError) != 0;
                Exception finalException = null;
                foreach (var batch in GetBatches(maxMessageSize, writerSettings))
                {
                    // Dispose of the Request as soon as possible to release the buffer(s)
                    SendPacketWithWriteConcernResult sendBatchResult;
                    using (batch.Packet)
                    {
                        sendBatchResult = SendBatchWithWriteConcern(channel, batch, continueOnError, writerSettings);
                        batch.Packet = null;
                    }

                    WriteConcernResult writeConcernResult;
                    try
                    {
                        writeConcernResult = ReadWriteConcernResult(channel, sendBatchResult, readerSettings);
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        writeConcernResult = ex.Result;
                        if (continueOnError)
                        {
                            finalException = ex;
                        }
                        else if (WriteConcern.Enabled)
                        {
                            results.Add(writeConcernResult);
                            ex.Data["results"] = results;
                            throw;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    if (WriteConcern.Enabled)
                    {
                        results.Add(writeConcernResult);
                    }
                }

                if (WriteConcern.Enabled && finalException != null)
                {
                    finalException.Data["results"] = results;
                    throw finalException;
                }

                return results;
            }
        }

        // protected methods
        /// <summary>
        /// Validates the required properties.
        /// </summary>
        protected override void ValidateRequiredProperties()
        {
            base.ValidateRequiredProperties();
            Ensure.IsNotNull("Documents", _documents);
            Ensure.IsNotNull("DocumentType", _documentType);
        }

        // private methods
        private IEnumerable<Batch> GetBatches(int maxMessageSize, BsonBinaryWriterSettings writerSettings)
        {
            var enumerator = _documents.GetEnumerator();
            try
            {
                byte[] overflowDocument = null;
                do
                {
                    var insertMessage = new InsertMessage(
                        Collection,
                        _flags,
                        _checkInsertDocuments,
                        writerSettings);

                    var packet = new BufferedRequestPacket();
                    try
                    {
                        packet.AddMessage(insertMessage);

                        if (overflowDocument != null)
                        {
                            insertMessage.AddDocument(packet.Stream, overflowDocument);
                            overflowDocument = null;
                        }

                        while (enumerator.MoveNext())
                        {
                            var document = enumerator.Current;
                            PrepareDocument(document);

                            insertMessage.AddDocument(packet.Stream, _documentType, document);
                            if (insertMessage.MessageLength > maxMessageSize)
                            {
                                overflowDocument = insertMessage.RemoveLastDocument(packet.Stream);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        packet.Dispose();
                        throw;
                    }

                    // ownership of the Request transfers to the caller and the caller must call Dispose on the Request
                    yield return new Batch { Packet = packet, IsLast = overflowDocument == null };
                }
                while (overflowDocument != null);
            }
            finally
            {
                var disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
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

        private SendPacketWithWriteConcernResult SendBatchWithWriteConcern(IChannel channel, Batch batch, bool continueOnError, BsonBinaryWriterSettings writerSettings)
        {
            var writeConcern = WriteConcern;
            if (!writeConcern.Enabled && !continueOnError && !batch.IsLast)
            {
                writeConcern = WriteConcern.Acknowledged;
            }
            return SendPacketWithWriteConcern(channel, batch.Packet, writeConcern, writerSettings);
        }

        // nested classes
        private class Batch
        {
            public BufferedRequestPacket Packet;
            public bool IsLast;
        }
    }
}