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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    internal sealed class MongoCollectionImpl<T> : IMongoCollection<T>
    {
        // fields
        private readonly ICluster _cluster;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(string databaseName, string collectionName, MongoCollectionSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _settings = Ensure.IsNotNull(settings, "settings");
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public Task<long> CountAsync(CountModel model, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var operation = new CountOperation(
                _databaseName,
                _collectionName,
                GetMessageEncoderSettings())
            {
                Filter = ConvertToBsonDocument(model.Filter),
                Hint = model.Hint is string ? BsonValue.Create((string)model.Hint) : ConvertToBsonDocument(model.Hint),
                Limit = model.Limit,
                MaxTime = model.MaxTime,
                Skip = model.Skip
            };

            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        private BsonDocument ConvertToBsonDocument(object document)
        {
            if(document == null)
            {
                return null;
            }

            var bsonDocument = document as BsonDocument;
            if(bsonDocument != null)
            {
                return bsonDocument;
            }

            if(document is string)
            {
                return BsonDocument.Parse((string)document);
            }

            var serializer = _settings.SerializerRegistry.GetSerializer(document.GetType());
            return new BsonDocumentWrapper(document, serializer);
        }

        private async Task<TResult> ExecuteReadOperation<TResult>(IReadOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference.ToCore()))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteWriteOperation<TResult>(IWriteOperation<TResult> operation, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Helper.StrictUtf8Encoding },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Helper.StrictUtf8Encoding }
            };
        }
    }
}