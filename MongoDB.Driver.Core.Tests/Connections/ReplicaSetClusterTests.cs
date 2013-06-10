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
    public class ReplicaSetClusterTests
    {
        private MockReplicaSet _replicaSet;
        private ReplicaSetClusterSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new ReplicaSetClusterSettings("funny");
            _replicaSet = new MockReplicaSet(_settings.ReplicaSetName);
        }

        [Test]
        public void When_the_connection_settings_contains_a_stand_alone_instance_it_should_get_removed()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.AddMember(ServerType.StandAlone, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(2, currentDescription.Count);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint == new DnsEndPoint("localhost", 1002)));
        }

        [Test]
        public void When_the_connection_settings_contains_a_mongos_instance_it_should_get_removed()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.AddMember(ServerType.ShardRouter, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(2, currentDescription.Count);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint == new DnsEndPoint("localhost", 1002)));
        }

        [Test]
        public void When_the_connection_settings_are_only_configured_with_a_primary_the_other_servers_should_get_added()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(3, currentDescription.Count);
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), currentPrimary.DnsEndPoint);
        }

        [Test]
        public void When_the_connection_settings_are_only_configured_with_a_secondary_the_other_servers_should_get_added()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1001));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(3, currentDescription.Count);
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), currentPrimary.DnsEndPoint);
        }

        [Test]
        public void When_the_connection_settings_are_configured_with_duplicate_servers_one_should_be_removed()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddAlternateDnsEndPoint(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1002));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.ApplyChanges();

            // ports 1000 and 1002 are the same server, one of them should be removed...
            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(2, currentDescription.Count);
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), currentPrimary.DnsEndPoint);

            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint == new DnsEndPoint("localhost", 1002)));
        }

        [Test]
        public void When_the_connection_settings_has_a_server_which_does_not_match_its_canonical_name_it_should_get_replaced_with_the_correct_one()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddAlternateDnsEndPoint(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1002));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.ApplyChanges();

            // port 1002 is in the connection string, but the server at 1002 reports that it is at 1000
            var subject = CreateSubject(new DnsEndPoint("localhost", 1002), new DnsEndPoint("localhost", 1001));

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(2, currentDescription.Count);
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), currentPrimary.DnsEndPoint);

            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint == new DnsEndPoint("localhost", 1002)));
        }

        [Test]
        public void When_the_primary_reports_a_new_server_it_should_get_added()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001));

            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(3, currentDescription.Count);
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), currentPrimary.DnsEndPoint);

            var newServer = currentDescription.Servers.Single(x => x.DnsEndPoint.Equals(new DnsEndPoint("localhost", 1002)));
            Assert.AreEqual(ServerStatus.Connected, newServer.Status);
            Assert.AreEqual(ServerType.ReplicaSetSecondary, newServer.Type);
        }

        [Test]
        public void When_the_primary_reports_a_dropped_server_it_should_get_removed()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1002));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001), new DnsEndPoint("localhost", 1002));

            _replicaSet.Remove(new DnsEndPoint("localhost", 1001));
            _replicaSet.ApplyChanges();

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            Assert.AreEqual(2, currentDescription.Count);
            Assert.IsFalse(currentDescription.Servers.Any(x => x.DnsEndPoint == new DnsEndPoint("localhost", 1001)));
        }

        [Test]
        public void When_secondary_becomes_primary_the_old_primary_should_get_invalidated()
        {
            _replicaSet.AddMember(ServerType.ReplicaSetPrimary, new DnsEndPoint("localhost", 1000));
            _replicaSet.AddMember(ServerType.ReplicaSetSecondary, new DnsEndPoint("localhost", 1001));
            _replicaSet.ApplyChanges();

            var subject = CreateSubject(new DnsEndPoint("localhost", 1000), new DnsEndPoint("localhost", 1001));

            _replicaSet.ChangeServerType(new DnsEndPoint("localhost", 1001), ServerType.ReplicaSetPrimary);
            _replicaSet.ApplyChanges();

            var currentDescription = (MultiServerClusterDescription)subject.Description;
            var currentPrimary = currentDescription.Servers.Single(x => x.Type == ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1001), currentPrimary.DnsEndPoint);

            var otherServer = currentDescription.Servers.Single(x => x.Type != ServerType.ReplicaSetPrimary);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000), otherServer.DnsEndPoint);
        }

        private ReplicaSetCluster CreateSubject(params DnsEndPoint[] dnsEndPoints)
        {
            var cluster = new ReplicaSetCluster(_settings, dnsEndPoints, _replicaSet);
            cluster.Initialize();
            return cluster;
        }
    }
}