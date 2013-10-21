using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Driver.Core.Connections;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    [TestFixture]
    public class ReadPreferenceServerSelectorTests
    {
        [Test]
        public void ReadPreference_Primary_should_return_the_primary_when_the_read_preference_is_primary()
        {
            var servers = GetConnectedServers();
            var rp = ReadPreference.Primary;
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1000, server.DnsEndPoint.Port);
        }

        [Test]
        public void ReadPreference_Primary_should_return_empty_when_no_primary_is_available()
        {
            var servers = GetConnectedServers(primaryConnected: false);
            var rp = ReadPreference.Primary;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.IsEmpty(servers);
        }

        [Test]
        public void ReadPreference_PrimaryPreferred_should_return_the_primary_when_it_is_available()
        {
            var servers = GetConnectedServers(secondariesConnected: false);
            var rp = ReadPreference.PrimaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1000, server.DnsEndPoint.Port);
        }

        [Test]
        public void ReadPreference_PrimaryPreferred_should_return_a_secondary_when_the_primary_is_not_available()
        {
            var servers = GetConnectedServers(primaryConnected: false);
            var rp = ReadPreference.PrimaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.Contains(1001, servers.Select(x => x.DnsEndPoint.Port).ToList());
            Assert.Contains(1002, servers.Select(x => x.DnsEndPoint.Port).ToList());
        }

        [Test]
        public void ReadPreference_PrimaryPreferred_should_return_empty_when_primary_and_secondaries_are_unavailable()
        {
            var servers = GetConnectedServers(primaryConnected: false, secondariesConnected: false);
            var rp = ReadPreference.PrimaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.IsEmpty(servers);
        }

        [Test]
        public void ReadPreference_SecondaryPreferred_should_return_a_secondary_when_one_is_available()
        {
            var servers = GetConnectedServers();
            var rp = ReadPreference.SecondaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers);

            Assert.Contains(1001, servers.Select(x => x.DnsEndPoint.Port).ToList());
            Assert.Contains(1002, servers.Select(x => x.DnsEndPoint.Port).ToList());
        }

        [Test]
        public void ReadPreference_SecondaryPreferred_should_return_the_primary_when_no_secondaries_are_available()
        {
            var servers = GetConnectedServers(secondariesConnected: false);
            var rp = ReadPreference.SecondaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1000, server.DnsEndPoint.Port);
        }

        [Test]
        public void ReadPreference_SecondaryPreferred_should_return_empty_when_secondaries_and_primary_are_unavailable()
        {
            var servers = GetConnectedServers(primaryConnected: false, secondariesConnected: false);
            var rp = ReadPreference.SecondaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.IsEmpty(servers);
        }

        [Test]
        public void ReadPreference_Secondary_should_return_a_secondary_when_one_is_available()
        {
            var servers = GetConnectedServers();
            var rp = ReadPreference.SecondaryPreferred;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.Contains(1001, servers.Select(x => x.DnsEndPoint.Port).ToList());
            Assert.Contains(1002, servers.Select(x => x.DnsEndPoint.Port).ToList());
        }

        [Test]
        public void ReadPreference_Secondary_should_return_empty_when_secondaries_are_unavailable()
        {
            var servers = GetConnectedServers(secondariesConnected: false);
            var rp = ReadPreference.Secondary;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.IsEmpty(servers);
        }

        [Test]
        public void ReadPreference_Nearest_should_return_a_primary_or_secondary_when_one_is_available()
        {
            var servers = GetConnectedServers();
            var rp = ReadPreference.Nearest;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.Contains(1000, servers.Select(x => x.DnsEndPoint.Port).ToList());
            Assert.Contains(1001, servers.Select(x => x.DnsEndPoint.Port).ToList());
        }

        [Test]
        public void ReadPreference_Nearest_should_return_empty_when_primary_and_secondaries_are_unavailable()
        {
            var servers = GetConnectedServers(primaryConnected: false, secondariesConnected: false);
            var rp = ReadPreference.Nearest;
            var subject = new ReadPreferenceServerSelector(rp);

            servers = subject.SelectServers(servers);

            Assert.IsEmpty(servers);
        }

        [Test]
        public void ReadPreference_Primary_should_ignore_tag_sets()
        {
            var servers = GetConnectedServers(includeTagSets: true);
            var tagSet = new ReplicaSetTagSet { { "not_exist", "true" } };
            var rp = new ReadPreference(ReadPreferenceMode.Primary, new [] { tagSet });
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1000, server.DnsEndPoint.Port);
        }

        [Test]
        [TestCase(ReadPreferenceMode.Nearest)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred)]
        [TestCase(ReadPreferenceMode.Secondary)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred)]
        public void ReadPreferences_with_secondaries_should_take_a_single_tag_into_account(ReadPreferenceMode mode)
        {
            var servers = GetConnectedServers(primaryConnected: false, includeTagSets: true);
            var tagSet = new ReplicaSetTagSet { { "a", "true" } };
            var rp = new ReadPreference(mode, new[] { tagSet });
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1001, server.DnsEndPoint.Port);
        }

        [Test]
        [TestCase(ReadPreferenceMode.Nearest)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred)]
        [TestCase(ReadPreferenceMode.Secondary)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred)]
        public void ReadPreference_with_secondaries_should_take_multiple_tags_into_account(ReadPreferenceMode mode)
        {
            var servers = GetConnectedServers(primaryConnected: false, includeTagSets: true);
            var tagSet = new ReplicaSetTagSet { { "a", "true" }, { "c", "true" } };
            var rp = new ReadPreference(mode, new[] { tagSet });
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1001, server.DnsEndPoint.Port);
        }

        [Test]
        [TestCase(ReadPreferenceMode.Nearest)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred)]
        [TestCase(ReadPreferenceMode.Secondary)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred)]
        public void ReadPreference_with_secondaries_should_take_multiple_tags_into_account_regardless_of_order(ReadPreferenceMode mode)
        {
            var servers = GetConnectedServers(primaryConnected: false, includeTagSets: true);
            var tagSet = new ReplicaSetTagSet { { "c", "true" }, { "a", "true" } };
            var rp = new ReadPreference(mode, new[] { tagSet });
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1001, server.DnsEndPoint.Port);
        }

        [Test]
        [TestCase(ReadPreferenceMode.Nearest)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred)]
        [TestCase(ReadPreferenceMode.Secondary)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred)]
        public void ReadPreference_with_secondaries_should_filter_on_second_tag_set_when_first_yields_no_results(ReadPreferenceMode mode)
        {
            var servers = GetConnectedServers(primaryConnected: false, includeTagSets: true);
            // matches no servers
            var tagSet1 = new ReplicaSetTagSet{ { "a", "true" }, { "d", "true" } };
            // mathces 1 server
            var tagSet2 = new ReplicaSetTagSet { { "a", "true" }, { "c", "true" } };
            var rp = new ReadPreference(mode, new[] { tagSet1, tagSet2 });
            var subject = new ReadPreferenceServerSelector(rp);

            var server = subject.SelectServers(servers).Single();

            Assert.AreEqual(1001, server.DnsEndPoint.Port);
        }

        private IEnumerable<ServerDescription> GetConnectedServers(bool primaryConnected=true, bool secondariesConnected=true, bool includeTagSets = false)
        {
            var servers = new List<ServerDescription>();
            servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                if (includeTagSets)
                {
                    n.ReplicaSetInfo(new ReplicaSetInfo("rs", new DnsEndPoint("localhost", 1000), new DnsEndPoint[0], new Dictionary<string, string> { { "a", "true" }, { "b", "true" } }, null));
                }
                n.DnsEndPoint(new DnsEndPoint("localhost", 1000));
                n.AveragePingTime(TimeSpan.FromMilliseconds(10));
                n.Status(primaryConnected ? ServerStatus.Connected : ServerStatus.Connecting);
                n.Type(ServerType.ReplicaSetPrimary);
            }));
            servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                if (includeTagSets)
                {
                    n.ReplicaSetInfo(new ReplicaSetInfo("rs", new DnsEndPoint("localhost", 1000), new DnsEndPoint[0], new Dictionary<string, string> { { "a", "true" }, { "c", "true" } }, null));
                }
                n.DnsEndPoint(new DnsEndPoint("localhost", 1001));
                n.AveragePingTime(TimeSpan.FromMilliseconds(20));
                n.Status(secondariesConnected ? ServerStatus.Connected : ServerStatus.Connecting);
                n.Type(ServerType.ReplicaSetSecondary);
            }));
            servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                if (includeTagSets)
                {
                    n.ReplicaSetInfo(new ReplicaSetInfo("rs", new DnsEndPoint("localhost", 1000), new DnsEndPoint[0], new Dictionary<string, string> { { "b", "true" }, { "c", "true" } }, null));
                }
                n.DnsEndPoint(new DnsEndPoint("localhost", 1002));
                n.AveragePingTime(TimeSpan.FromMilliseconds(30));
                n.Status(secondariesConnected ? ServerStatus.Connected : ServerStatus.Connecting);
                n.Type(ServerType.ReplicaSetSecondary);
            }));
            servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                if (includeTagSets)
                {
                    n.ReplicaSetInfo(new ReplicaSetInfo("rs", new DnsEndPoint("localhost", 1000), new DnsEndPoint[0], new Dictionary<string, string> { { "b", "true" }, { "d", "true" } }, null));
                }
                n.DnsEndPoint(new DnsEndPoint("localhost", 1003));
                n.AveragePingTime(TimeSpan.FromMilliseconds(40));
                n.Status(secondariesConnected ? ServerStatus.Connected : ServerStatus.Connecting);
                n.Type(ServerType.ReplicaSetSecondary);
            }));
            servers.Add(ServerDescriptionBuilder.Build(n =>
            {
                n.DnsEndPoint(new DnsEndPoint("localhost", 1004));
                n.AveragePingTime(TimeSpan.FromMilliseconds(50));
                n.Status(ServerStatus.Connected);
                n.Type(ServerType.ReplicaSetArbiter);
            }));

            return servers;
        }
    }
}