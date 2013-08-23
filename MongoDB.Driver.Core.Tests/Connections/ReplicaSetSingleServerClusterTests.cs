using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ReplicaSetSingleServerClusterTests
    {
        private MockServer _server;
        private SingleServerCluster _subject;

        [SetUp]
        public void Setup()
        {
            _server = new MockServer(new DnsEndPoint("localhost", 27017));
            var serverFactory = Substitute.For<IClusterableServerFactory>();
            serverFactory.Create(null).ReturnsForAnyArgs(_server);

            _subject = new SingleServerCluster(new ClusterSettings(ClusterType.ReplicaSet, new [] { _server.DnsEndPoint }, "name"), serverFactory);
            _subject.Initialize();
        }

        [Test]
        public void Description_should_be_updated_when_the_server_is_connected()
        {
            _server.SetDescription(ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("localhost", 27017));
                x.ReplicaSetInfo("name", new DnsEndPoint("localhost", 27017));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetPrimary);
            }));

            var description = _subject.Description;
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.AreEqual(1, description.ServerCount);
            Assert.AreEqual(ServerStatus.Connected, description.Servers.Single().Status);
            Assert.AreEqual(ServerType.ReplicaSetPrimary, description.Servers.Single().Type);
        }

        [Test]
        public void Should_throw_an_exception_when_server_is_a_member_of_the_wrong_replica_set()
        {
            _server.SetDescription(ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("localhost", 27017));
                x.ReplicaSetInfo("wrong_name", new DnsEndPoint("localhost", 27017));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetPrimary);
            }));

            var description = _subject.Description;
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.AreEqual(0, description.ServerCount);
        }

        [Test]
        public void Description_should_have_no_servers_when_actual_server_is_a_shard_router()
        {
            _server.SetDescription(ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("localhost", 27017));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ShardRouter);
            }));

            var description = _subject.Description;
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.AreEqual(0, description.ServerCount);
        }

        [Test]
        public void Description_should_have_no_servers_when_actual_server_is_a_stand_alone()
        {
            _server.SetDescription(ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("localhost", 27017));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.StandAlone);
            }));

            var description = _subject.Description;
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.AreEqual(0, description.ServerCount);
        }
    }
}