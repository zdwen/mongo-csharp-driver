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
    public class ClusterTests
    {
        private TestCluster _subject;

        [SetUp]
        public void Setup()
        {
            var server = Substitute.For<IClusterableServer>();
            var serverFactory = Substitute.For<IClusterableServerFactory>();
            serverFactory.Create(null).ReturnsForAnyArgs(server);

            _subject = new TestCluster(serverFactory);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_if_not_initialized()
        {
            Assert.Throws<InvalidOperationException>(() => _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void SelectServer_should_throw_an_exception_if_disposed()
        {
            _subject.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void SelectServer_should_return_the_server_if_it_matches()
        {
            _subject.Initialize();

            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));
            _subject.SetDescription(ClusterType.StandAlone, connected);

            var selectedServer = _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            Assert.IsNotNull(selectedServer);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_if_it_does_not_match_and_already_connected()
        {
            _subject.Initialize();

            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));
            _subject.SetDescription(ClusterType.StandAlone, connected);

            var selector = new DelegateServerSelector("never matches", s => false);

            Assert.Throws<MongoDriverException>(() => _subject.SelectServer(selector, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void SelectServer_should_try_to_connect_if_the_server_is_not_already_connected_and_try_matching_again()
        {
            _subject.Initialize();
            var connecting = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connecting));
            var connected = ServerDescriptionBuilder.Build(b => b.Status(ServerStatus.Connected));

            _subject.SetDescription(ClusterType.StandAlone, connecting);

            Task.Factory.StartNew(() => 
            {
                var descriptions = new Queue<ServerDescription>(new[] { connecting, connecting, connecting, connected });
                while(descriptions.Count > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    var next = descriptions.Dequeue();
                    _subject.SetDescription(ClusterType.StandAlone, next);
                }
            });

            var selectedServer = _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            Assert.IsNotNull(selectedServer);
        }

        [Test]
        public void SelectServer_should_throw_an_exception_after_timing_out_trying_to_select_a_server()
        {
            _subject.Initialize();
            Assert.Throws<MongoDriverException>(() => _subject.SelectServer(ConnectedServerSelector.Instance, TimeSpan.Zero, CancellationToken.None));
        }

        private class TestCluster : Cluster
        {
            public TestCluster(IClusterableServerFactory serverFactory)
                : base(serverFactory)
            {
                SetDescription(ClusterType.Unknown, ServerDescriptionBuilder.Connecting(new DnsEndPoint("localhost", 1000)));
            }

            public void SetDescription(ClusterType type, ServerDescription description)
            {
                UpdateDescription(new ClusterDescription(
                    type,
                    new [] { description }));
            }

            protected override bool TryGetServer(ServerDescription description, out IServer server)
            {
                server = CreateServer(description.DnsEndPoint);
                return true;
            }
        }
    }
}