﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkInsertOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // fields
        private Action<object, IBsonSerializer> _assignId;
        private bool _checkElementNames = true;

        // constructors
        public BulkInsertOperationEmulator(
            string databaseName,
            string collectionName,
            IEnumerable<InsertRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, collectionName, requests, messageEncoderSettings)
        {
        }

        // properties
        public Action<object, IBsonSerializer> AssignId
        {
            get { return _assignId; }
            set { _assignId = value; }
        }

        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        //  methods
        protected override IWireProtocol<WriteConcernResult> CreateProtocol(IConnectionHandle connection, WriteRequest request)
        {
            var insertRequest = (InsertRequest)request;
            var document = insertRequest.Document;
            var serializer = insertRequest.Serializer;       
            if (_assignId != null)
            {
                _assignId(document, serializer);
            }
            var wrapper = new BsonDocumentWrapper(document, serializer);
            var documentSource = new BatchableSource<BsonDocument>(new[] { wrapper });

            return new InsertWireProtocol<BsonDocument>(
                DatabaseName,
                CollectionName,
                WriteConcern,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings,
                documentSource,
                connection.Description.MaxBatchCount,
                connection.Description.MaxMessageSize,
                continueOnError: false);               
        }
    }
}
