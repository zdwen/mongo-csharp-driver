using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var subject = new ClusterFactory(serverFactory);
            var settings = ClusterSettings.Create(x => x.AddHost("localhost"));

            var result = subject.Create(settings);

            Assert.IsInstanceOf<SingleServerCluster>(result);
        }

        [Test]
        public void Should_create_a_single_server_cluster_when_connection_mode_is_multiple()
        {
            var serverFactory = Substitute.For<IClusterableServerFactory>();

            var subject = new ClusterFactory(serverFactory);
            var settings = ClusterSettings.Create(x =>
            {
               x.AddHost("localhost");
               x.AddHost("otherhost");
            });

            var result = subject.Create(settings);

            Assert.IsInstanceOf<MultiServerCluster>(result);
        }
    }
}
