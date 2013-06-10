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
    /// Performs the insert of multiple documents.
    /// </summary>
    public class InsertOperation : WriteOperation
    {
        // private fields
        private readonly bool _assignIdOnInsert;
        private readonly bool _checkElementNames;
        private readonly Type _documentType;
        private readonly IEnumerable _documents;
        private readonly InsertFlags _flags;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertOperation" /> class.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="writeConcern">The write concern.</param>
        /// <param name="assignIdOnInsert">if set to <c>true</c> [assign id on insert].</param>
        /// <param name="checkElementNames">if set to <c>true</c> [check element names].</param>
        /// <param name="documentType">Type of the document.</param>
        /// <param name="documents">The documents.</param>
        /// <param name="flags">The flags.</param>
        public InsertOperation(
            MongoNamespace @namespace,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern,
            bool assignIdOnInsert,
            bool checkElementNames,
            Type documentType,
            IEnumerable documents,
            InsertFlags flags)
            : base(@namespace, readerSettings, writerSettings, writeConcern)
        {
            Ensure.IsNotNull("documentType", documentType);
            Ensure.IsNotNull("documents", documents);

            _assignIdOnInsert = assignIdOnInsert;
            _checkElementNames = checkElementNames;
            _documentType = documentType;
            _documents = documents;
            _flags = flags;
        }

        // public methods
        /// <summary>
        /// Executes the insert operation.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public IEnumerable<WriteConcernResult> Execute(IServerChannel channel)
        {
            Ensure.IsNotNull("channel", channel);

            var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
            var writerSettings = GetServerAdjustedWriterSettings(channel.Server);

            List<WriteConcernResult> results = (WriteConcern.Enabled) ? new List<WriteConcernResult>() : null;

            var writeConcernEnabled = WriteConcern.Enabled;
            var continueOnError = (_flags & InsertFlags.ContinueOnError) != 0;
            Exception finalException = null;
            foreach (var request in GetRequests(channel, writerSettings))
            {
                if (writeConcernEnabled && !continueOnError)
                {
                    try
                    {
                        var result = SendMessageWithWriteConcern(channel, request.Request, readerSettings, writerSettings, WriteConcern);
                        results.Add(result);
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        results.Add(ex.Result);
                        ex.Data["results"] = results;
                        throw;
                    }
                }
                else if (writeConcernEnabled && continueOnError)
                {
                    try
                    {
                        var result = SendMessageWithWriteConcern(channel, request.Request, readerSettings, writerSettings, WriteConcern);
                        results.Add(result);
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        finalException = ex;
                        ex.Data["results"] = results;
                        results.Add(ex.Result);
                    }
                }
                else if (!writeConcernEnabled && !continueOnError)
                {
                    try
                    {
                        // Do NOT send write concern on the last batch...
                        var writeConcern = request.IsLast ? WriteConcern.Unacknowledged : WriteConcern.Acknowledged;
                        SendMessageWithWriteConcern(channel, request.Request, readerSettings, writerSettings, writeConcern);
                    }
                    catch (MongoWriteConcernException)
                    {
                        return null;
                    }
                }
                else if (!writeConcernEnabled && continueOnError)
                {
                    SendMessageWithWriteConcern(channel, request.Request, readerSettings, writerSettings, WriteConcern.Unacknowledged);
                }
            }

            return results;
        }

        private IEnumerable<InsertRequest> GetRequests(IServerChannel channel, BsonBinaryWriterSettings writerSettings)
        {
            int maxMessageLength = channel.Server.MaxMessageSize;
            IEnumerator enumerator = null;
            try
            {
                enumerator = PrepareDocuments().GetEnumerator();
                byte[] rolloverDocument = null;
                do
                {
                    using (var request = new BsonBufferedRequestMessage())
                    {
                        var insertMessage = new InsertMessageBuilder(
                            Namespace,
                            _flags,
                            _checkElementNames,
                            writerSettings);

                        insertMessage.AddToRequest(request);

                        if (rolloverDocument != null)
                        {
                            insertMessage.AddRolloverDocument(request, rolloverDocument);
                            rolloverDocument = null;
                        }

                        while (enumerator.MoveNext())
                        {
                            int lastLength = insertMessage.MessageLength;
                            insertMessage.AddDocument(request, _documentType, enumerator.Current);
                            if (insertMessage.MessageLength > maxMessageLength)
                            {
                                rolloverDocument = insertMessage.RemoveFromRequest(request, insertMessage.MessageLength - lastLength);
                                break;
                            }
                        }

                        yield return new InsertRequest { Request = request, IsLast = rolloverDocument == null };
                    }
                }
                while (rolloverDocument != null);
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

        private IEnumerable PrepareDocuments()
        {
            foreach (var document in _documents)
            {
                // I'm not convinced that this should be done here.  Rather, it should exist in the guy
                // calling this.
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

                yield return document;
            }
        }

        private class InsertRequest
        {
            public BsonBufferedRequestMessage Request;
            public bool IsLast;
        }
    }
}