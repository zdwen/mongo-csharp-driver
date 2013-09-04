using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    public class DbConfigurationTests
    {
        private DbConfiguration _configuration;
        private ICluster _cluster;

        [SetUp]
        public void SetUp()
        {
            _configuration = new DbConfiguration();
            var clusterFactory = Substitute.For<IClusterFactory>();
            _cluster = Substitute.For<ICluster>();
            clusterFactory.Create().Returns(_cluster);
            _configuration.Register(clusterFactory);
        }

        [Test]
        public void BuildSessionFactory_should_return_a_valid_default_session_factory()
        {
            var sessionFactory = _configuration.BuildSessionFactory();

            _cluster.Received().Initialize();
            Assert.AreSame(sessionFactory.Cluster, _cluster);
            Assert.IsNotNull(sessionFactory.Configuration);
        }
    }
}