using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Mocks
{
    public class MockClusterableServerFactory : IClusterableServerFactory
    {
        private readonly ConcurrentBag<MockServer> _servers;

        public MockClusterableServerFactory()
        {
            _servers = new ConcurrentBag<MockServer>();
        }

        public IClusterableServer Create(DnsEndPoint dnsEndPoint)
        {
            var server = new MockServer(dnsEndPoint);
            _servers.Add(server);
            return server;
        }

        public void ChangeDescription(DnsEndPoint dnsEndPoint, ServerDescription description)
        {
            foreach (var server in _servers.Where(x => x.DnsEndPoint.Equals(dnsEndPoint) && x.Description.Status != ServerStatus.Disposed))
            {
                server.SetDescription(description);
            }
        }

        public void MakePrimary(DnsEndPoint dnsEndPoint, string replicaSetName, params DnsEndPoint[] secondaries)
        {
            var description = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.ReplicaSetInfo(replicaSetName, dnsEndPoint, secondaries);
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetPrimary);
            });

            ChangeDescription(dnsEndPoint, description);
        }

        public void MakeSecondary(DnsEndPoint dnsEndPoint, string replicaSetName, DnsEndPoint primary, params DnsEndPoint[] otherSecondaries)
        {
            var description = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.ReplicaSetInfo(replicaSetName, primary, otherSecondaries.Concat(new [] { dnsEndPoint }).ToArray());
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetSecondary);
            });

            ChangeDescription(dnsEndPoint, description);
        }

        public void MakeShardRouter(DnsEndPoint dnsEndPoint)
        {
            var description = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ShardRouter);
            });

            ChangeDescription(dnsEndPoint, description);
        }

        public void MakeStandAlone(DnsEndPoint dnsEndPoint)
        {
            var description = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.StandAlone);
            });

            ChangeDescription(dnsEndPoint, description);
        }
    }
}