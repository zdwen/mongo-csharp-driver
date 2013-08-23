using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Driver.Core.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ShardedMultiServerClusterTests
    {
        private MockClusterableServerFactory _serverFactory;

        [SetUp]
        public void SetUp()
        {
            _serverFactory = new MockClusterableServerFactory();
        }

        [Test]
        public void Should_report_the_correct_description_after_construction()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            var description = subject.Description;
            Assert.AreEqual(3, description.ServerCount);
            Assert.AreEqual(ClusterType.Sharded, description.Type);
            Assert.IsTrue(description.Servers.All(x => x.Status == ServerStatus.Disconnected));
        }

        [Test]
        public void Should_report_the_correct_description_after_initialize()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            var description = subject.Description;
            Assert.AreEqual(3, description.ServerCount);
            Assert.AreEqual(ClusterType.Sharded, description.Type);
            Assert.IsTrue(description.Servers.All(x => x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Stand_alone_instances_should_be_removed_from_the_cluster()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakeShardRouter(new DnsEndPoint("localhost", 1000));
            _serverFactory.MakeShardRouter(new DnsEndPoint("localhost", 1001));
            _serverFactory.MakeStandAlone(new DnsEndPoint("localhost", 1002));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002));
        }

        [Test]
        public void Replica_Set_members_should_be_removed_from_the_cluster()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakeShardRouter(new DnsEndPoint("localhost", 1000));
            _serverFactory.MakeShardRouter(new DnsEndPoint("localhost", 1001));
            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1002),
                "dummy",
                new DnsEndPoint("localhost", 1003),
                new DnsEndPoint("localhost", 1004));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002));
        }

        private MultiServerCluster CreateSubject(params DnsEndPoint[] dnsEndPoints)
        {
            var settings = new ClusterSettings(ClusterType.Sharded, dnsEndPoints, null);
            return new MultiServerCluster(settings, _serverFactory);
        }
    }
}