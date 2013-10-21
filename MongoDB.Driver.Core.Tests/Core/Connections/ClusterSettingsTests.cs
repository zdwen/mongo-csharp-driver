using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ClusterSettingsTests
    {
        [Test]
        public void Should_set_cluster_type_to_replica_set_when_cluster_type_is_unknown_and_replica_set_name_is_specified()
        {
            var subject = ClusterSettings.Create(x =>
            {
                x.SetReplicaSetName("yeah");
            });

            Assert.AreEqual(ClusterType.ReplicaSet, subject.Type);
        }

        [Test]
        public void Should_set_all_the_hosts_correctly()
        {
            var subject = ClusterSettings.Create(x =>
            {
                x.AddHost("localhost");
                x.AddHost("localhost", 30000);
            });

            Assert.AreEqual(2, subject.Hosts.Count());
            Assert.Contains(new DnsEndPoint("localhost", 27017), subject.Hosts.ToList());
            Assert.Contains(new DnsEndPoint("localhost", 30000), subject.Hosts.ToList());
        }

        [Test]
        public void Should_set_connection_mode_to_single_when_only_one_host_is_specified()
        {
            var subject = ClusterSettings.Create(x =>
            {
                x.AddHost("one");
            });

            Assert.AreEqual(ClusterConnectionMode.Single, subject.ConnectionMode);
        }

        [Test]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void Should_set_connection_mode_to_multiple_when_multiple_hosts_are_specified(int count)
        {
            var subject = ClusterSettings.Create(x =>
            {
                for (int i = 0; i < count; i++)
                {
                    x.AddHost("host" + i);
                }
            });

            Assert.AreEqual(ClusterConnectionMode.Multiple, subject.ConnectionMode);
        }

        [Test]
        public void Should_throw_an_exception_when_cluster_type_is_stand_alone_and_multiple_hosts_are_specified()
        {
            Assert.Throws<ArgumentException>(() =>
                ClusterSettings.Create(x =>
                {
                    x.AddHost("localhost");
                    x.AddHost("other");
                    x.SetType(ClusterType.StandAlone);
                })
            );
        }

        [Test]
        public void Should_throw_an_exception_when_a_replica_set_name_is_specified_with_cluster_type_stand_alone()
        {
            Assert.Throws<ArgumentException>(() =>
                ClusterSettings.Create(x =>
                {
                    x.SetReplicaSetName("yeah");
                    x.SetType(ClusterType.StandAlone);
                })
            );
        }

        [Test]
        public void Should_throw_an_exception_when_a_replica_set_name_is_specified_with_cluster_type_sharded()
        {
            Assert.Throws<ArgumentException>(() =>
                ClusterSettings.Create(x =>
                {
                    x.SetReplicaSetName("yeah");
                    x.SetType(ClusterType.Sharded);
                })
            );
        }
    }
}
