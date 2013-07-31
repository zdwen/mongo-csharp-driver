using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class MultiServerClusterTests
    {
        private MockServer _server;
        private MultiServerCluster _subject;

        [SetUp]
        public void Setup()
        {
            _server = new MockServer(new DnsEndPoint("localhost", 27017));
            var serverFactory = Substitute.For<IClusterableServerFactory>();
            serverFactory.Create(null).ReturnsForAnyArgs(_server);

            _subject = new TestMultiServerManager(
                new[] { _server.DnsEndPoint },
                serverFactory);
        }

        [Test]
        public void Description_should_return_a_cluster_description_of_type_Multi()
        {
            _subject.Initialize();

            var description = _subject.Description;

            Assert.AreEqual(ClusterDescriptionType.Multi, description.Type);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_if_not_initialized()
        {
            Assert.Throws<InvalidOperationException>(() => _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void SelectServer_should_return_the_server_if_it_matches()
        {
            _subject.Initialize();

            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));
            _server.SetNextDescription(connected);
            _server.ApplyChanges();

            var selectedServer = _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            Assert.AreEqual(_server.Description.DnsEndPoint, selectedServer.Description.DnsEndPoint);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_if_it_does_not_match_and_already_connected()
        {
            _subject.Initialize();

            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));
            _server.SetNextDescription(connected);
            _server.ApplyChanges();

            var selector = new DelegateServerSelector("never matches", s => false);

            Assert.Throws<MongoDriverException>(() => _subject.SelectServer(selector, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void SelectServer_should_try_to_connect_if_the_server_is_not_already_connected_and_try_matching_again()
        {
            _subject.Initialize();
            var connecting = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connecting));
            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));

            _server.SetNextDescription(connecting);
            _server.ApplyChanges();

            Task.Factory.StartNew(() => 
            {
                var descriptions = new Queue<ServerDescription>(new[] { connecting, connecting, connecting, connected });
                while(descriptions.Count > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    var next = descriptions.Dequeue();
                    _server.SetNextDescription(next);
                    _server.ApplyChanges();
                }
            });

            var selectedServer = _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            Assert.AreEqual(_server.Description.DnsEndPoint, selectedServer.Description.DnsEndPoint);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_after_timing_out_trying_to_select_a_server()
        {
            _subject.Initialize();
            Assert.Throws<MongoDriverException>(() => _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.Zero, CancellationToken.None));
        }

        private class TestMultiServerManager : MultiServerCluster
        {
            public TestMultiServerManager(IEnumerable<DnsEndPoint> dnsEndPoint, IClusterableServerFactory serverFactory)
                : base(MultiServerClusterType.Unknown, dnsEndPoint, serverFactory)
            {
            }

            protected override void HandleUpdatedDescription(ServerDescription description)
            {
                // do nothing...
            }
        }

    }
}