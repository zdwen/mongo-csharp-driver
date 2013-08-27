using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core
{
    public class ClusterBuilder
    {
        public ICluster BuildCluster()
        {
            var settings = GetSettings();
            var events = BuildEventPublisher(settings);
            var streamFactory = BuildStreamFactory(events, settings);
            var connectionFactory = BuildConnectionFactory(streamFactory, events, settings);
            var channelProviderFactory = BuildChannelProviderFactory(connectionFactory, streamFactory, events, settings);
            var clusterableServerFactory = BuildClusterableServerFactory(channelProviderFactory, connectionFactory, streamFactory, events, settings);
            return BuildCluster(clusterableServerFactory, channelProviderFactory, connectionFactory, streamFactory, events, settings);
        }

        protected virtual ITestSettings GetSettings()
        {
            return new EnvironmentVariableTestSettings();
        }

        protected virtual IEventPublisher BuildEventPublisher(ITestSettings settings)
        {
            return new EventPublisher();
        }

        protected virtual IStreamFactory BuildStreamFactory(IEventPublisher events,  ITestSettings settings)
        {
            return new NetworkStreamFactory(
                NetworkStreamSettings.Defaults,
                new DnsCache());
        }

        protected virtual IConnectionFactory BuildConnectionFactory(IStreamFactory streamFactory, IEventPublisher events,  ITestSettings settings)
        {
            return new StreamConnectionFactory(
                StreamConnectionSettings.Defaults,
                streamFactory,
                events);
        }

        protected virtual IChannelProviderFactory BuildChannelProviderFactory(IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events,  ITestSettings settings)
        {
            return new ConnectionPoolChannelProviderFactory(
                new ConnectionPoolFactory(
                    ConnectionPoolSettings.Defaults,
                    connectionFactory,
                    events),
                events);
        }

        protected virtual IClusterableServerFactory BuildClusterableServerFactory(IChannelProviderFactory channelProviderFactory, IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events,  ITestSettings settings)
        {
            return new ClusterableServerFactory(
                ClusterableServerSettings.Defaults,
                channelProviderFactory,
                connectionFactory,
                events);
        }

        protected virtual ICluster BuildCluster(IClusterableServerFactory clusterableServerFactory, IChannelProviderFactory channelProviderFactory, IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events,  ITestSettings settings)
        {
            var servers = settings.GetArrayValuesOrDefault("Cluster-DnsEndPoint", new[] { "localhost:27017" })
                .Select(x => x.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Length == 2 ? new DnsEndPoint(x[0], int.Parse(x[1])) : new DnsEndPoint(x[0], 27017))
                .ToList();

            var clusterSettings = ClusterSettings.Create(x =>
            {
                foreach (var server in servers)
                {
                    x.AddHost(server);
                }
            });

            return new ClusterFactory(clusterableServerFactory).Create(clusterSettings);
        }
    }
}