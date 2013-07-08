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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a batch Insert operation.
    /// </summary>
    public class InsertOperation : WriteOperation
    {
        // private fields
        private readonly bool _assignIdOnInsert;
        private readonly bool _checkInsertDocuments;
        private readonly Type _documentType;
        private readonly IEnumerable _documents;
        private readonly InsertFlags _flags;
        private readonly int _maxMessageSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertOperation" /> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <param name="assignIdOnInsert">if set to <c>true</c> [assign id on insert].</param>
        /// <param name="checkInsertDocuments">if set to <c>true</c> [check element names].</param>
        /// <param name="documentType">Type of the document.</param>
        /// <param name="documents">The documents.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="maxMessageSize">The max message size for each batch.</param>
        public InsertOperation(
            CollectionNamespace collectionNamespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern,
            bool assignIdOnInsert,
            bool checkInsertDocuments,
            Type documentType,
            IEnumerable documents,
            InsertFlags flags,
            int maxMessageSize)
            : base(collectionNamespace, readerSettings, writerSettings, writeConcern)
        {
            Ensure.IsNotNull("documentType", documentType);
            Ensure.IsNotNull("documents", documents);

            _assignIdOnInsert = assignIdOnInsert;
            _checkInsertDocuments = checkInsertDocuments;
            _documentType = documentType;
            _documents = documents;
            _flags = flags;
            _maxMessageSize = maxMessageSize;
        }

        // public methods
        /// <summary>
        /// Executes the Insert operation.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is not enabled).</returns>
        public IEnumerable<WriteConcernResult> Execute(IServerChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            var maxMessageSize = (_maxMessageSize != 0) ? _maxMessageSize : channel.Server.MaxMessageSize;
            var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
            var writerSettings = GetServerAdjustedWriterSettings(channel.Server);

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
                        CollectionNamespace,
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
        
        private SendPacketWithWriteConcernResult SendBatchWithWriteConcern(IServerChannel channel, Batch batch, bool continueOnError, BsonBinaryWriterSettings writerSettings)
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