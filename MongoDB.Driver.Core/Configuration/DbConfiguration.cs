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
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Configuration.Resolvers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// The root object to construct <see cref="ICluster"/>s and <see cref="ISession"/>s.
    /// </summary>
    public class DbConfiguration
    {
        // private fields
        private readonly List<IDbDependencyResolver> _resolvers;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfiguration" /> class.
        /// </summary>
        public DbConfiguration()
        {
            _resolvers = new List<IDbDependencyResolver> 
            {
                new InstanceDbDependencyResolver<IDbConfigurationPropertyProvider>(new DbConfigurationPropertyProvider()),
                new InstanceDbDependencyResolver<IEventPublisher>(new NoOpEventPublisher()),
                new DnsCacheResolver(),
                new NetworkStreamSettingsResolver(),
                new SslStreamSettingsResolver(),
                new ReflectedDbDependencyResolver<IStreamFactory, NetworkStreamFactory>(),
                new StreamConnectionSettingsResolver(),
                new ReflectedDbDependencyResolver<IConnectionFactory, StreamConnectionFactory>(),
                new ConnectionPoolSettingsResolver(),
                new ReflectedDbDependencyResolver<IConnectionPoolFactory, ConnectionPoolFactory>(),
                new ReflectedDbDependencyResolver<IChannelProviderFactory, ConnectionPoolChannelProviderFactory>(),
                new ClusterableServerSettingsResolver(),
                new ReflectedDbDependencyResolver<IClusterableServerFactory, ClusterableServerFactory>(),
                new ClusterSettingsResolver(),
                new ReflectedDbDependencyResolver<IClusterFactory, ClusterFactory>()
            };
        }

        // public methods
        /// <summary>
        /// Adds the dependency resolver.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>This instance.</returns>
        public DbConfiguration AddDependencyResolver(IDbDependencyResolver resolver)
        {
            Ensure.IsNotNull("resolver", resolver);

            _resolvers.Add(resolver);
            return this;
        }

        /// <summary>
        /// Registers the provided instance into the dependency tree.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="dependency">The dependency.</param>
        /// <returns>This instance.</returns>
        public DbConfiguration Register<T>(T dependency)
        {
            return AddDependencyResolver(new InstanceDbDependencyResolver<T>(dependency));
        }

        /// <summary>
        /// Registers the factory into the dependency tree.  When the dependency is requested, the
        /// factory will get invoked.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <returns>This instance.</returns>
        /// <returns></returns>
        public DbConfiguration Register<T>(Func<IDbConfigurationContainer, T> factory)
        {
            Ensure.IsNotNull("factory", factory);
            return AddDependencyResolver(new TransientDbDependencyResolver<T>(factory));
        }

        /// <summary>
        /// Registers the factory. When the dependency is requested, the previously registered
        /// <typeparamref name="T"/> will be resolved and the factory will get invoked.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <returns>This instance.</returns>
        public DbConfiguration Register<T>(Func<T, IDbConfigurationContainer, T> factory)
        {
            Ensure.IsNotNull("factory", factory);
            return AddDependencyResolver(new TransientWrappingDbDependencyResolver<T>(factory));
        }

        /// <summary>
        /// Builds the session factory.
        /// </summary>
        /// <returns>A session factory.</returns>
        public ISessionFactory BuildSessionFactory()
        {
            var resolver = new CompositeDbDependencyResolver(_resolvers.Reverse<IDbDependencyResolver>());
            var cachingResolver = new CachingDbDependencyResolver(resolver);
            var configuration = new ConfigurationContainer(cachingResolver);
            var clusterFactory = configuration.Resolve<IClusterFactory>();

            var cluster = clusterFactory.Create();
            cluster.Initialize();

            return new SessionFactory(configuration, cluster);
        }

        // nested classes
        private class ConfigurationContainer : IDbConfigurationContainer
        {
            private readonly IDbDependencyResolver _resolver;

            public ConfigurationContainer(IDbDependencyResolver resolver)
            {
                _resolver = resolver;
            }

            public object Resolve(Type type)
            {
                try
                {
                    var result = _resolver.Resolve(type, this);
                    if (result == null)
                    {
                        var message = string.Format("Unable to resolve {0}.", type);
                        throw new MongoConfigurationException(message);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    var message = string.Format("Unable to resolve {0}.", type);
                    throw new MongoConfigurationException(message, ex);
                }
            }
        }

        private sealed class SessionFactory : ISessionFactory
        {
            private readonly IDbConfigurationContainer _configuration;
            private readonly ICluster _cluster;
            private bool _disposed;

            public SessionFactory(IDbConfigurationContainer configuration, ICluster cluster)
            {
                _configuration = configuration;
                _cluster = cluster;
            }

            public IDbConfigurationContainer Configuration
            {
                get { return _configuration; }
            }

            public ICluster Cluster
            {
                get { return _cluster; }
            }

            public ISession BeginSession()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return new ClusterSession(_cluster);
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _cluster.Dispose();
                }
            }
        }
    }
}