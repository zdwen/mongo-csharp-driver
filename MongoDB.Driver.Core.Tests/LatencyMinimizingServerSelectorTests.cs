using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    public class LatencyMinimizingServerSelectorTests
    {
        private List<ServerDescription> _servers;

        [SetUp]
        public void SetUp()
        {
            _servers = new List<ServerDescription>();

            _servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                n.DnsEndPoint(new DnsEndPoint("localhost", 1000));
                n.AveragePingTime(TimeSpan.FromMilliseconds(0));
                n.Status(ServerStatus.Connected);
                n.Type(ServerType.ShardRouter);
            }));
            _servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                n.DnsEndPoint(new DnsEndPoint("localhost", 1001));
                n.AveragePingTime(TimeSpan.FromMilliseconds(20));
                n.Status(ServerStatus.Connected);
                n.Type(ServerType.ShardRouter);
            }));
            _servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                n.DnsEndPoint(new DnsEndPoint("localhost", 1002));
                n.AveragePingTime(TimeSpan.FromMilliseconds(30));
                n.Status(ServerStatus.Connected);
                n.Type(ServerType.ShardRouter);
            }));
            _servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                n.DnsEndPoint(new DnsEndPoint("localhost", 1003));
                n.AveragePingTime(TimeSpan.FromMilliseconds(50));
                n.Status(ServerStatus.Connected);
                n.Type(ServerType.ShardRouter);
            }));
        }

        [Test]
        public void SelectServers_should_return_all_servers_when_the_allowed_latency_is_infinite()
        {
            var subject = new LatencyLimitingServerSelector(Timeout.InfiniteTimeSpan);

            var servers = subject.SelectServers(_servers);

            Assert.AreEqual(_servers.Count, servers.Count());
        }

        [Test]
        public void SelectServers_should_only_return_the_servers_within_the_allowed_latency()
        {
            var subject = new LatencyLimitingServerSelector(TimeSpan.FromMilliseconds(29));

            Assert.Contains(1000, _servers.Select(x => x.DnsEndPoint.Port).ToList());
            Assert.Contains(1001, _servers.Select(x => x.DnsEndPoint.Port).ToList());
        }
    }
}