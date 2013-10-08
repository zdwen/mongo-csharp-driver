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
using System.Configuration;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// The root object to construct <see cref="ICluster"/>s.
    /// </summary>
    public class DbConfiguration
    {
        // private fields
        private ClusterSettings.Builder _clusterSettingsBuilder;
        private StreamConnectionSettings.Builder _connectionSettingsBuilder;
        private ConnectionPoolSettings.Builder _connectionPoolSettingsBuilder;
        private IEventPublisher _eventPublisher;
        private NetworkStreamSettings.Builder _networkStreamSettingsBuilder;
        private ClusterableServerSettings.Builder _serverSettingsBuilder;
        private Func<IStreamFactory, IStreamFactory> _streamFactoryWrapper;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfiguration" /> class.
        /// </summary>
        public DbConfiguration()
        {
            _clusterSettingsBuilder = new ClusterSettings.Builder();
            _connectionSettingsBuilder = new StreamConnectionSettings.Builder();
            _connectionPoolSettingsBuilder = new ConnectionPoolSettings.Builder();
            _eventPublisher = new NoOpEventPublisher();
            _networkStreamSettingsBuilder = new NetworkStreamSettings.Builder();
            _serverSettingsBuilder = new ClusterableServerSettings.Builder();
            _streamFactoryWrapper = inner => inner; // no op...
        }

        // public methods
        /// <summary>
        /// Builds the cluster.
        /// </summary>
        /// <returns>An <see cref="ICluster"/> implementation.</returns>
        public ICluster BuildCluster()
        {
            var streamFactory = _streamFactoryWrapper(new NetworkStreamFactory(_networkStreamSettingsBuilder.Build(), new DnsCache()));
            var connectionFactory = new StreamConnectionFactory(_connectionSettingsBuilder.Build(), streamFactory, _eventPublisher);
            var connectionPoolFactory = new ConnectionPoolFactory(_connectionPoolSettingsBuilder.Build(), connectionFactory, _eventPublisher);
            var channelProviderFactory = new ConnectionPoolChannelProviderFactory(connectionPoolFactory, _eventPublisher);
            var clusterableServerFactory = new ClusterableServerFactory(_serverSettingsBuilder.Build(), channelProviderFactory, connectionFactory);
            var clusterFactory = new ClusterFactory(_clusterSettingsBuilder.Build(), clusterableServerFactory);

            var cluster = clusterFactory.Create();
            cluster.Initialize();
            return cluster;
        }

        /// <summary>
        /// Configures cluster settings.
        /// </summary>
        /// <param name="builderCallback">The builder callback.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureCluster(Action<ClusterSettings.Builder> builderCallback)
        {
            Ensure.IsNotNull("builderCallback", builderCallback);

            builderCallback(_clusterSettingsBuilder);
            return this;
        }

        /// <summary>
        /// Configures connection settings.
        /// </summary>
        /// <param name="builderCallback">The builder callback.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureConnection(Action<StreamConnectionSettings.Builder> builderCallback)
        {
            Ensure.IsNotNull("builderCallback", builderCallback);

            builderCallback(_connectionSettingsBuilder);
            return this;
        }

        /// <summary>
        /// Configures connection pool settings.
        /// </summary>
        /// <param name="builderCallback">The builder callback.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureConnectionPool(Action<ConnectionPoolSettings.Builder> builderCallback)
        {
            Ensure.IsNotNull("builderCallback", builderCallback);

            builderCallback(_connectionPoolSettingsBuilder);
            return this;
        }

        /// <summary>
        /// Configures network stream settings.
        /// </summary>
        /// <param name="builderCallback">The builder callback.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureNetworkStream(Action<NetworkStreamSettings.Builder> builderCallback)
        {
            Ensure.IsNotNull("builderCallback", builderCallback);

            builderCallback(_networkStreamSettingsBuilder);
            return this;
        }

        /// <summary>
        /// Configures server settings.
        /// </summary>
        /// <param name="builderCallback">The builder callback.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureServer(Action<ClusterableServerSettings.Builder> builderCallback)
        {
            Ensure.IsNotNull("builderCallback", builderCallback);

            builderCallback(_serverSettingsBuilder);
            return this;
        }

        /// <summary>
        /// Configures settings using the connection string.  This can either be a connectionString key, an appSettings key,
        /// or an actual connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureWithConnectionString(string connectionString)
        {
            Ensure.IsNotNull("connectionString", connectionString);

            var connectionStringConfig = ConfigurationManager.ConnectionStrings[connectionString];
            if (connectionStringConfig != null)
            {
                return ConfigureWithConnectionString(new DbConnectionString(connectionStringConfig.ConnectionString));
            }

            var appSettingsConfig = ConfigurationManager.AppSettings[connectionString];
            if (appSettingsConfig != null)
            {
                return ConfigureWithConnectionString(new DbConnectionString(appSettingsConfig));
            }

            return ConfigureWithConnectionString(new DbConnectionString(connectionString));
        }

        /// <summary>
        /// Configures settings using the connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration ConfigureWithConnectionString(DbConnectionString connectionString)
        {
            Ensure.IsNotNull("connectionString", connectionString);

            ApplyConnectionString(connectionString);
            return this;
        }

        /// <summary>
        /// Registers the event listeners.  The listener objects must implement IEventListener{T}.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <param name="listeners">The listeners.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration RegisterEventListeners(object listener, params object[] listeners)
        {
            Ensure.IsNotNull("listener", listener);
            var publisher = new EventPublisher();
            publisher.Subscribe(listener);

            if (listeners != null)
            {
                foreach (var otherListener in listeners)
                {
                    publisher.Subscribe(otherListener);
                }
            }

            _eventPublisher = new EventPublisherPair(_eventPublisher, publisher);
            return this;
        }

        /// <summary>
        /// Registers the stream factory.  This func will recieve the inner <see cref="IStreamFactory"/> as 
        /// an argument.  It can choose whether or not to wrap it or ignore it.
        /// </summary>
        /// <param name="wrapper">The wrapper.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration RegisterStreamFactory(Func<IStreamFactory, IStreamFactory> wrapper)
        {
            Ensure.IsNotNull("wrapper", wrapper);

            _streamFactoryWrapper = inner => wrapper(_streamFactoryWrapper(inner));
            return this;
        }

        /// <summary>
        /// Configures the use of performance counters.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="install">if set to <c>true</c>, the performance counters will be installed.  This requires Adminstrator permissions.</param>
        /// <returns>The current configuration.</returns>
        public DbConfiguration UsePerformanceCounters(string applicationName, bool install = false)
        {
            if (install)
            {
                PerformanceCounterEventListeners.Install();
            }
            var perfCounters = new PerformanceCounterEventListeners(applicationName);
            return RegisterEventListeners(perfCounters);
        }

        // private methods
        private void ApplyConnectionString(DbConnectionString connectionString)
        {
            // Network
            if (connectionString.ConnectTimeout != null)
            {
                _networkStreamSettingsBuilder.SetConnectTimeout(connectionString.ConnectTimeout.Value);
            }
            if (connectionString.SocketTimeout != null)
            {
                _networkStreamSettingsBuilder.SetReadTimeout(connectionString.SocketTimeout.Value);
                _networkStreamSettingsBuilder.SetWriteTimeout(connectionString.SocketTimeout.Value);
            }
            if (connectionString.Ssl != null)
            {
                var settings = SslStreamSettings.Create(b =>
                {
                    if (connectionString.SslVerifyCertificate != null && !connectionString.SslVerifyCertificate.Value)
                    {
                        b.ValidateServerCertificateWith((obj, cert, chain, errors) => true);
                    }
                });
            }

            // Connection
            if(connectionString.Username != null)
            {
                var credential = MongoCredential.FromComponents(
                    connectionString.AuthMechanism,
                    connectionString.AuthSource,
                    connectionString.Username,
                    connectionString.Password);

                _connectionSettingsBuilder.AddCredential(credential);
            }

            // ConnectionPool
            if (connectionString.MaxPoolSize != null)
            {
                _connectionPoolSettingsBuilder.SetMaxSize(connectionString.MaxPoolSize.Value);
            }
            if (connectionString.MinPoolSize != null)
            {
                _connectionPoolSettingsBuilder.SetMinSize(connectionString.MinPoolSize.Value);
            }
            if (connectionString.MaxIdleTime != null)
            {
                _connectionPoolSettingsBuilder.SetConnectionMaxIdleTime(connectionString.MaxIdleTime.Value);
            }
            if (connectionString.MaxLifeTime != null)
            {
                _connectionPoolSettingsBuilder.SetConnectionMaxLifeTime(connectionString.MaxLifeTime.Value);
            }
            if (connectionString.WaitQueueMultiple != null)
            {
                _connectionPoolSettingsBuilder.SetWaitQueueMultiple(connectionString.WaitQueueMultiple.Value);
            }

            // Server
            // nothing to configure for server

            // Cluster
            if (connectionString.Hosts != null)
            {
                _clusterSettingsBuilder.AddHosts(connectionString.Hosts);
            }
            if (connectionString.ReplicaSet != null)
            {
                _clusterSettingsBuilder.SetReplicaSetName(connectionString.ReplicaSet);
            }
        }
    }
}