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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Driver.Communication;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a client to MongoDB.
    /// </summary>
    public class MongoClient : IMongoClient
    {
        // private fields
        private readonly ICluster _cluster;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoClientSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        public MongoClient()
            : this(new MongoClientSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public MongoClient(MongoClientSettings settings)
        {
            _settings = settings.FrozenCopy();
            _cluster = ClusterRegistry.Instance.GetOrCreateCluster(_settings);
            _operationExecutor = new OperationExecutor();
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="url">The URL.</param>
        public MongoClient(MongoUrl url)
            : this(MongoClientSettings.FromUrl(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public MongoClient(string connectionString)
            : this(ParseConnectionString(connectionString))
        {
        }

        internal MongoClient(IOperationExecutor operationExecutor)
            : this()
        {
            _operationExecutor = operationExecutor;
        }

        // public properties
        /// <summary>
        /// Gets the cluster.
        /// </summary>
        internal ICluster Cluster
        {
            get { return _cluster; }
        }

        /// <summary>
        /// Gets the client settings.
        /// </summary>
        public MongoClientSettings Settings
        {
            get { return _settings; }
        }

        // private static methods
        private static MongoClientSettings ParseConnectionString(string connectionString)
        {
            var url = new MongoUrl(connectionString);
            return MongoClientSettings.FromUrl(url);
        }

        // public methods
        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>An implementation of a database.</returns>
        public IMongoDatabase GetDatabase(string name)
        {
            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(_settings);
            return GetDatabase(name, settings);
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An implementation of a database.</returns>
        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings)
        {
            return new MongoDatabaseImpl(name, settings, _cluster, _operationExecutor);
        }

        /// <summary>
        /// Gets the database names.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of the database on the server.</returns>
        public async Task<IReadOnlyList<string>> GetDatabaseNamesAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListDatabaseNamesOperation(messageEncoderSettings);

            using(var binding = new ReadPreferenceBinding(_cluster, _settings.ReadPreference.ToCore()))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, timeout ?? _settings.OperationTimeout, cancellationToken);
            }
        }

        /// <summary>
        /// Gets a MongoServer object using this client's settings.
        /// </summary>
        /// <returns>A MongoServer.</returns>
        public MongoServer GetServer()
        {
            var serverSettings = MongoServerSettings.FromClientSettings(_settings);
#pragma warning disable 618
            return MongoServer.Create(serverSettings);
#pragma warning restore
        }

        // private methods
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
