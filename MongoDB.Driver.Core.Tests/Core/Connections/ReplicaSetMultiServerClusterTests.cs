using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ReplicaSetMultiServerClusterTests
    {
        private MockClusterableServerFactory _serverFactory;
        private string _replicaSetName;

        [SetUp]
        public void SetUp()
        {
            _replicaSetName = "rs1";
            _serverFactory = new MockClusterableServerFactory();
        }

        [Test]
        public void Should_report_the_correct_description_after_construction()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            var description = subject.Description;
            Assert.AreEqual(3, description.ServerCount);
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.IsTrue(description.Servers.All(x => x.Status == ServerStatus.Disconnected));
        }

        [Test]
        public void Should_report_the_correct_description_after_initialize()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            var description = subject.Description;
            Assert.AreEqual(3, description.ServerCount);
            Assert.AreEqual(ClusterType.ReplicaSet, description.Type);
            Assert.IsTrue(description.Servers.All(x => x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Stand_alone_instances_should_be_removed_from_the_cluster()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000), 
                _replicaSetName, 
                new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1001),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeStandAlone(new DnsEndPoint("localhost", 1002));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002));
        }

        [Test]
        public void Mongos_instances_should_be_removed_from_the_cluster()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1001),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeShardRouter(new DnsEndPoint("localhost", 1002));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002));
        }

        [Test]
        public void Should_add_other_members_of_the_replica_set_when_reported_by_the_primary()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            var currentDescription = subject.Description;
            Assert.AreEqual(3, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1001 && x.Status == ServerStatus.Connecting));
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002 && x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Should_add_other_members_of_the_replica_set_when_reported_by_a_secondary()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000));
            subject.Initialize();

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001), // primary
                new DnsEndPoint("localhost", 1002));

            var currentDescription = subject.Description;
            Assert.AreEqual(3, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1001 && x.Status == ServerStatus.Connecting));
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002 && x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Should_remove_server_reporting_as_someone_else_and_add_the_correct_server()
        {
            var subject = CreateSubject(new DnsEndPoint("other", 1000), new DnsEndPoint("localhost", 1001));
            subject.Initialize();

            // other:1000 will report itself as localhost:1000
            _serverFactory.ChangeDescription(new DnsEndPoint("other", 1000), ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("other", 1000));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetPrimary);
                x.ReplicaSetInfo(_replicaSetName,
                    new DnsEndPoint("localhost", 1000),
                    new DnsEndPoint("localhost", 1001));
            }));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Equals(new DnsEndPoint("localhost", 1000)) && x.Status == ServerStatus.Connecting));
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Equals(new DnsEndPoint("localhost", 1001)) && x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Should_remove_duplicate_servers()
        {
            var subject = CreateSubject(new DnsEndPoint("other", 1000), new DnsEndPoint("localhost", 1000));
            subject.Initialize();

            // other:1000 will report itself as localhost:1000
            _serverFactory.ChangeDescription(new DnsEndPoint("other", 1000), ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(new DnsEndPoint("other", 1000));
                x.Status(ServerStatus.Connected);
                x.Type(ServerType.ReplicaSetPrimary);
                x.ReplicaSetInfo(_replicaSetName,
                    new DnsEndPoint("localhost", 1000),
                    new DnsEndPoint("localhost", 1001));
            }));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Equals(new DnsEndPoint("localhost", 1000)) && x.Status == ServerStatus.Connecting));
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Equals(new DnsEndPoint("localhost", 1001)) && x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Should_add_newly_discovered_members_after_connection()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1001),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000));

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001),
                new DnsEndPoint("localhost", 1002)); // newly discovered

            var currentDescription = subject.Description;
            Assert.AreEqual(3, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002 && x.Status == ServerStatus.Connecting));
        }

        [Test]
        public void Should_drop_members_that_stop_getting_reported()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1001),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1002),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1001));

            // drop 1002
            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001));

            var currentDescription = subject.Description;
            Assert.AreEqual(2, currentDescription.ServerCount);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002));
        }

        [Test]
        public void Should_invalidate_the_old_primary_when_a_new_primary_shows_up()
        {
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));
            subject.Initialize();

            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1000),
                _replicaSetName,
                new DnsEndPoint("localhost", 1001),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1001),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1002));

            _serverFactory.MakeSecondary(
                new DnsEndPoint("localhost", 1002),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1001));

            // 1002 is now the primary
            _serverFactory.MakePrimary(
                new DnsEndPoint("localhost", 1002),
                _replicaSetName,
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1001));

            var currentDescription = subject.Description;
            Assert.AreEqual(3, currentDescription.ServerCount);
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1000 && x.Type == ServerType.Unknown && x.Status == ServerStatus.Connecting));
            Assert.IsTrue(currentDescription.Servers.Any(x => x.DnsEndPoint.Port == 1002 && x.Type == ServerType.ReplicaSetPrimary));
        }

        private MultiServerCluster CreateSubject(params DnsEndPoint[] dnsEndPoints)
        {
            var settings = new ClusterSettings(ClusterType.ReplicaSet, dnsEndPoints, _replicaSetName);
            return new MultiServerCluster(settings, _serverFactory, new NoOpEventPublisher());
        }
    }
}