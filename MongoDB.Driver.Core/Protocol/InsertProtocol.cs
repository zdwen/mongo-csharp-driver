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
using System.Diagnostics;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents the insert protocol.
    /// </summary>
    public sealed class InsertProtocol : WriteProtocolBase<IEnumerable<WriteConcernResult>>
    {
        // private static fields
        private static readonly TraceSource _trace = MongoTraceSources.Operations;

        // private fields
        private readonly bool _checkInsertDocuments;
        private readonly Type _documentType;
        private readonly IEnumerable _documents;
        private readonly InsertFlags _flags;
        private readonly int _maxMessageSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertProtocol" /> class.
        /// </summary>
        /// <param name="checkInsertDocuments">if set to <c>true</c> [check insert documents].</param>
        /// <param name="collection">The collection.</param>
        /// <param name="documentType">Type of the document.</param>
        /// <param name="documents">The documents.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="maxMessageSize">Size of the max message.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <param name="writerSettings">The writer settings.</param>
        public InsertProtocol(bool checkInsertDocuments,
            CollectionNamespace collection,
            Type documentType,
            IEnumerable documents,
            InsertFlags flags,
            int maxMessageSize,
            BsonBinaryReaderSettings readerSettings,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
            : base(collection, readerSettings, writeConcern, writerSettings)
        {
            Ensure.IsNotNull("documentType", documentType);
            Ensure.IsNotNull("documents", documents);

            _checkInsertDocuments = checkInsertDocuments;
            _documentType = documentType;
            _documents = documents;
            _flags = flags;
            _maxMessageSize = maxMessageSize;
        }

        // public methods
        /// <summary>
        /// Executes the Insert protocol.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A list of WriteConcernResults (or null if WriteConcern is not enabled).</returns>
        public override IEnumerable<WriteConcernResult> Execute(IChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            var results = (WriteConcern.Enabled) ? new List<WriteConcernResult>() : null;

            var continueOnError = _flags.HasFlag(InsertFlags.ContinueOnError);
            Exception finalException = null;
            int batchNumber = -1;
            foreach (var batch in GetBatches())
            {
                batchNumber++;
                _trace.TraceVerbose("inserting batch#{0} with {1} documents into {2} with {3}.", batchNumber, batch.Count, Collection.FullName, channel);
                // Dispose of the Request as soon as possible to release the buffer(s)
                SendPacketWithWriteConcernResult sendBatchResult;
                using (batch.Packet)
                {
                    sendBatchResult = SendBatchWithWriteConcern(channel, batch, continueOnError);
                    batch.Packet = null;
                }

                WriteConcernResult writeConcernResult;
                try
                {
                    writeConcernResult = ReadWriteConcernResult(channel, sendBatchResult);
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
        private IEnumerable<Batch> GetBatches()
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
                        WriterSettings);

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
                            insertMessage.AddDocument(packet.Stream, _documentType, document);
                            if (insertMessage.MessageLength > _maxMessageSize)
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
                    yield return new Batch { Count = insertMessage.DocumentCount, Packet = packet, IsLast = overflowDocument == null };
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

        private SendPacketWithWriteConcernResult SendBatchWithWriteConcern(IChannel channel, Batch batch, bool continueOnError)
        {
            var writeConcern = WriteConcern;
            if (!writeConcern.Enabled && !continueOnError && !batch.IsLast)
            {
                writeConcern = WriteConcern.Acknowledged;
            }
            return SendPacketWithWriteConcern(channel, batch.Packet, writeConcern);
        }

        // nested classes
        private class Batch
        {
            public int Count;
            public BufferedRequestPacket Packet;
            public bool IsLast;
        }
    }
}