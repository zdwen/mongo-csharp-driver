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
            var traceManager = BuildTraceManager(settings);
            var events = BuildEventPublisher(settings);
            var streamFactory = BuildStreamFactory(events, traceManager, settings);
            var connectionFactory = BuildConnectionFactory(streamFactory, events, traceManager, settings);
            var channelProviderFactory = BuildChannelProviderFactory(connectionFactory, streamFactory, events, traceManager, settings);
            var clusterableServerFactory = BuildClusterableServerFactory(channelProviderFactory, connectionFactory, streamFactory, events, traceManager, settings);
            return BuildCluster(clusterableServerFactory, channelProviderFactory, connectionFactory, streamFactory, events, traceManager, settings);
        }

        protected virtual ITestSettings GetSettings()
        {
            return new EnvironmentVariableTestSettings();
        }

        protected virtual TraceManager BuildTraceManager(ITestSettings settings)
        {
            return new TraceManager();
        }

        protected virtual IEventPublisher BuildEventPublisher(ITestSettings settings)
        {
            return new EventPublisher();
        }

        protected virtual IStreamFactory BuildStreamFactory(IEventPublisher events, TraceManager traceManager, ITestSettings settings)
        {
            return new NetworkStreamFactory(
                NetworkStreamFactorySettings.Defaults,
                new DnsCache());
        }

        protected virtual IConnectionFactory BuildConnectionFactory(IStreamFactory streamFactory, IEventPublisher events, TraceManager traceManager, ITestSettings settings)
        {
            return new StreamConnectionFactory(
                streamFactory,
                events,
                traceManager);
        }

        protected virtual IChannelProviderFactory BuildChannelProviderFactory(IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events, TraceManager traceManager, ITestSettings settings)
        {
            return new ConnectionPoolChannelProviderFactory(
                new ConnectionPoolFactory(
                    ConnectionPoolSettings.Defaults,
                    connectionFactory,
                    events,
                    traceManager),
                events,
                traceManager);
        }

        protected virtual IClusterableServerFactory BuildClusterableServerFactory(IChannelProviderFactory channelProviderFactory, IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events, TraceManager traceManager, ITestSettings settings)
        {
            return new ClusterableServerFactory(
                false,
                ClusterableServerSettings.Defaults,
                channelProviderFactory,
                connectionFactory,
                events,
                traceManager);
        }

        protected virtual ICluster BuildCluster(IClusterableServerFactory clusterableServerFactory, IChannelProviderFactory channelProviderFactory, IConnectionFactory connectionFactory, IStreamFactory streamFactory, IEventPublisher events, TraceManager traceManager, ITestSettings settings)
        {
            var servers = settings.GetArrayValuesOrDefault("Cluster-DnsEndPoint", new[] { "localhost:27017" })
                .Select(x => x.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Length == 2 ? new DnsEndPoint(x[0], int.Parse(x[1])) : new DnsEndPoint(x[0], 27017))
                .ToList();

            if (servers.Count == 1)
            {
                return new SingleServerCluster(new DnsEndPoint("localhost", 27017), clusterableServerFactory);
            }
            else
            {
                return new ReplicaSetCluster(
                    ReplicaSetClusterSettings.Defaults,
                    servers,
                    clusterableServerFactory);
            }
        }
    }
}