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
using System.Configuration;
using System.Net;
using MongoDB.Driver.Core.Configuration.Resolvers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Extensions for <see cref="DbConfiguration"/>.
    /// </summary>
    public static class DbConfigurationExtensions
    {
        /// <summary>
        /// Configures a replica set cluster.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="replicaSetName">Name of the replica set.</param>
        /// <param name="hosts">The hosts.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ConnectToReplicaSet(this DbConfiguration @this, string replicaSetName, params DnsEndPoint[] hosts)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("replicaSetName", replicaSetName);
            Ensure.IsNotNull("hosts", hosts);

            var properties = new DbConfigurationPropertyProvider();
            properties.SetValue(DbConfigurationProperties.Cluster.ClusterType, ClusterType.ReplicaSet);
            properties.SetValue(DbConfigurationProperties.Cluster.Hosts, new[] { hosts });

            return @this.WithProperties(properties);
        }

        /// <summary>
        /// Configures a replica set cluster.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="hosts">The hosts.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ConnectToShards(this DbConfiguration @this, params DnsEndPoint[] hosts)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("hosts", hosts);

            var properties = new DbConfigurationPropertyProvider();
            properties.SetValue(DbConfigurationProperties.Cluster.ClusterType, ClusterType.Sharded);
            properties.SetValue(DbConfigurationProperties.Cluster.Hosts, new[] { hosts });

            return @this.WithProperties(properties);
        }

        /// <summary>
        /// Configures a standalone cluster.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="hostname">The hostname.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ConnectToStandalone(this DbConfiguration @this, string hostname)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("hostname", hostname);

            return @this.ConnectToStandalone(hostname, 27017);
        }

        /// <summary>
        /// Configures a standalone cluster.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ConnectToStandalone(this DbConfiguration @this, string hostname, int port)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("hostname", hostname);

            return @this.ConnectToStandalone(new DnsEndPoint(hostname, port));
        }

        /// <summary>
        /// Configures a standalone cluster.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="host">The host.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ConnectToStandalone(this DbConfiguration @this, DnsEndPoint host)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("host", host);

            var properties = new DbConfigurationPropertyProvider();
            properties.SetValue(DbConfigurationProperties.Cluster.ClusterType, ClusterType.StandAlone);
            properties.SetValue(DbConfigurationProperties.Cluster.Hosts, new[] { host });

            return @this.WithProperties(properties);
        }

        /// <summary>
        /// Includes performance counter listeners.
        /// </summary>
        /// <param name="this">The this.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="install">if set to <c>true</c> [install].</param>
        /// <returns></returns>
        public static DbConfiguration IncludePerformanceCounters(this DbConfiguration @this, string applicationName, bool install)
        {
            if (install)
            {
                PerformanceCounterPackage.Install();
            }

            return @this.ListenToEventsWith(new PerformanceCounterEventListeners(applicationName));
        }

        /// <summary>
        /// Adds the listeners into the configuration.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="listeners">The listeners.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration ListenToEventsWith(this DbConfiguration @this, params object[] listeners)
        {
            var publisher = new EventPublisher();
            foreach (var listener in listeners)
            {
                publisher.Subscribe(listener);
            }

            return @this.Register<IEventPublisher>((inner, container) => new EventPublisherPair(publisher, inner));
        }

        /// <summary>
        /// Configures the container to use the connection string.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The DbConfiguration.</returns>
        /// <remarks>
        /// This will attempt to get the connection string by name from the 
        /// ConnectionStrings configuration section, then from the AppSettings configuration
        /// section, and then finally will use it as a connection string.
        /// </remarks>
        public static DbConfiguration UseConnectionString(this DbConfiguration @this, string connectionString)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("connectionString", connectionString);

            var connectionStringConfig = ConfigurationManager.ConnectionStrings[connectionString];
            if (connectionStringConfig != null)
            {
                return @this.UseConnectionString(new DbConnectionString(connectionStringConfig.ConnectionString));
            }

            var appSettingsConfig = ConfigurationManager.AppSettings[connectionString];
            if (appSettingsConfig != null)
            {
                return @this.UseConnectionString(new DbConnectionString(appSettingsConfig));
            }

            return @this.UseConnectionString(new DbConnectionString(connectionString));
        }

        /// <summary>
        /// Configures the container to use the connection string.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UseConnectionString(this DbConfiguration @this, DbConnectionString connectionString)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("connectionString", connectionString);

            @this.Register<DbConnectionString>(connectionString);
            if (connectionString.Ssl.HasValue && connectionString.Ssl.Value)
            {
                @this.UseSsl();
            }

            var adapter = new DbConnectionStringConfigurationPropertiesAdapter(connectionString);
            return @this.WithProperties(adapter);
        }

        /// <summary>
        /// Configures the use of a pipelining channel factory.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="numberOfConcurrentConnections">The number of concurrent connections.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UsePipelinedChannels(this DbConfiguration @this, int numberOfConcurrentConnections = 5)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsGreaterThan("numberOfConcurrentConnections", numberOfConcurrentConnections, 0);

            return @this.Register<IChannelProviderFactory>(container =>
            {
                var connectionFactory = container.Resolve<IConnectionFactory>();
                return new PipelinedChannelProviderFactory(connectionFactory, numberOfConcurrentConnections);
            });
        }

        /// <summary>
        /// Configures the use of a Socks Proxy.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="host">The host.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UseSocksProxy(this DbConfiguration @this, DnsEndPoint host)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("host", host);

            return @this.Register<IStreamFactory>((inner, container) =>
            {
                return new Socks5StreamProxy(host, inner);
            });
        }

        /// <summary>
        /// Configures SSL.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UseSsl(this DbConfiguration @this)
        {
            Ensure.IsNotNull("@this", @this);

            return UseSsl(@this, SslStreamSettings.Defaults);
        }

        /// <summary>
        /// Configures SSL.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="sslStreamSettings">The settings.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UseSsl(this DbConfiguration @this, SslStreamSettings sslStreamSettings)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("sslStreamSettings", sslStreamSettings);

            @this.Register<SslStreamSettings>(SslStreamSettings.Defaults);
            return @this.Register<IStreamFactory>((inner, container) =>
            {
                var settings = container.Resolve<SslStreamSettings>();
                return new SslStreamFactory(settings, inner);
            });
        }

        /// <summary>
        /// Configures SSL.
        /// </summary>
        /// <param name="this">The DbConfiguration to configure.</param>
        /// <param name="callback">The settings.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration UseSsl(this DbConfiguration @this, Action<SslStreamSettings.Builder> callback)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("callback", callback);

            @this.Register<SslStreamSettings>(SslStreamSettings.Create(callback));
            return @this.Register<IStreamFactory>((inner, container) =>
            {
                var settings = container.Resolve<SslStreamSettings>();
                return new SslStreamFactory(settings, inner);
            });
        }

        /// <summary>
        /// Configures the container to use the specified property.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration WithProperty(this DbConfiguration @this, string name, object value)
        {
            var properties = new DbConfigurationPropertyProvider();
            properties.SetValue(name, value);

            return @this.WithProperties(properties);
        }

        /// <summary>
        /// Configures the container to use the specified property.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration WithProperty(this DbConfiguration @this, DbConfigurationProperty property, object value)
        {
            var properties = new DbConfigurationPropertyProvider();
            properties.SetValue(property, value);

            return @this.WithProperties(properties);
        }

        /// <summary>
        /// Configures the container to use the specified properties.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration WithProperties(this DbConfiguration @this, IDbConfigurationPropertyProvider properties)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("properties", properties);

            return @this.Register<IDbConfigurationPropertyProvider>((inner, container) => new DbConfigurationPropertiesPair(properties, inner));
        }

        /// <summary>
        /// Configures the container to use the properties specified in the callback.
        /// </summary>
        /// <param name="this">The DbConfiguration.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The DbConfiguration.</returns>
        public static DbConfiguration WithProperties(this DbConfiguration @this, Action<DbConfigurationPropertyProvider> callback)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("callback", callback);

            var props = new DbConfigurationPropertyProvider();
            callback(props);
            return @this.Register<IDbConfigurationPropertyProvider>((inner, container) => new DbConfigurationPropertiesPair(props, inner));
        }
    }
}