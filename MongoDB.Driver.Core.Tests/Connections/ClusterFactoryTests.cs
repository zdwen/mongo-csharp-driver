using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Events;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ClusterFactoryTests
    {
        [Test]
        public void Should_create_a_single_server_cluster_when_connection_mode_is_single()
        {
            var serverFactory = Substitute.For<IClusterableServerFactory>();

            var settings = ClusterSettings.Create(x => x.AddHost("localhost"));
            var subject = new ClusterFactory(settings, serverFactory, new NoOpEventPublisher());

            var result = subject.Create();

            Assert.IsInstanceOf<SingleServerCluster>(result);
        }

        [Test]
        public void Should_create_a_single_server_cluster_when_connection_mode_is_multiple()
        {
            var serverFactory = Substitute.For<IClusterableServerFactory>();

            var settings = ClusterSettings.Create(x =>
            {
               x.AddHost("localhost");
               x.AddHost("otherhost");
            });
            var subject = new ClusterFactory(settings, serverFactory, new NoOpEventPublisher());

            var result = subject.Create();

            Assert.IsInstanceOf<MultiServerCluster>(result);
        }
    }
}
