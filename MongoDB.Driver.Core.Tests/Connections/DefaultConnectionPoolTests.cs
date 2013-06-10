using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class DefaultConnectionPoolTests
    {
        private MockConnectionFactory _connectionFactory;
        private DefaultChannelProviderSettings _connectionPoolSettings;
        private DefaultChannelProvider _subject;

        [SetUp]
        public void SetUp()
        {
            _connectionPoolSettings = new DefaultChannelProviderSettings(
                connectionMaxIdleTime: TimeSpan.FromMilliseconds(-1),
                connectionMaxLifeTime: TimeSpan.FromMilliseconds(-1),
                maxSize: 4,
                minSize: 2,
                sizeMaintenanceFrequency: TimeSpan.FromMilliseconds(-1),
                maxWaitQueueSize: 2);

            _connectionFactory = new MockConnectionFactory();

            var dnsEndPoint = new DnsEndPoint("localhost", 27017);
            _subject = new DefaultChannelProvider(_connectionPoolSettings, dnsEndPoint, _connectionFactory, new EventPublisher(), new TraceManager());
        }

        [Test]
        public void GetConnection_should_throw_if_unitialized()
        {
            Assert.Throws<InvalidOperationException>(() => _subject.GetChannel());
        }

        [Test]
        public void Disposing_of_a_connection_after_disconnect_should_not_throw_an_exception()
        {
            _subject.Initialize();
            var connection = _subject.GetChannel();
            var connection2 = _subject.GetChannel();

            connection.Dispose();
            _subject.Dispose();
            connection2.Dispose();
        }

        [Test]
        public void GetConnection_should_throw_an_exception_after_disposing()
        {
            _subject.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _subject.GetChannel());
        }

        [Test]
        public void GetConnection_should_create_a_new_connection_when_there_are_none_available_and_the_max_connection_count_has_not_been_reached()
        {
            InitializeAndWaitForMinPoolSize();

            var connections = new List<IChannel>();
            for (int i = 0; i < _connectionPoolSettings.MinSize; i++)
            {
                // checkout the all the connections in the pool
                connections.Add(_subject.GetChannel());
            }

            // this should create a new connection because all the connections in the pool have been checked out
            _subject.GetChannel();

            // we should have gotten more calls to the connection factory...
            Assert.AreEqual(_connectionPoolSettings.MinSize + 1, _connectionFactory.CreatedConnectionCount);
        }

        [Test]
        public void GetConnection_should_throw_exception_when_waiting_too_long_for_a_connection()
        {
            InitializeAndWaitForMinPoolSize();

            var connections = new List<IChannel>();
            // checkout all the connections in the pool so that we have to wait for one to become available
            for (int i = 0; i < _connectionPoolSettings.MaxSize; i++)
            {
                connections.Add(_subject.GetChannel());
            }

            // don't wait for the connection at all
            Assert.Throws<MongoDriverException>(() => _subject.GetChannel(TimeSpan.Zero));
        }

        private void InitializeAndWaitForMinPoolSize()
        {
            _subject.Initialize();
            while (_connectionFactory.CreatedConnectionCount < _connectionPoolSettings.MinSize) ;
        }
    }
}
