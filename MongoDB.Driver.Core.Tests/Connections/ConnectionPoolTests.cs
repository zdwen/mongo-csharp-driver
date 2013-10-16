using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Protocol;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class ConnectionPoolTests
    {
        private IConnectionFactory _connectionFactory;
        private ConnectionPoolSettings _connectionPoolSettings;
        private ConnectionPool _subject;

        [SetUp]
        public void SetUp()
        {
            _connectionPoolSettings = new ConnectionPoolSettings(
                connectionMaxIdleTime: TimeSpan.FromMilliseconds(-1),
                connectionMaxLifeTime: TimeSpan.FromMilliseconds(-1),
                maxSize: 4,
                minSize: 2,
                sizeMaintenanceFrequency: TimeSpan.FromMilliseconds(-1),
                maxWaitQueueSize: 2);

            _connectionFactory = Substitute.For<IConnectionFactory>();

            var dnsEndPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), dnsEndPoint);
            _subject = new ConnectionPool(serverId, _connectionPoolSettings, dnsEndPoint, _connectionFactory, new NoOpEventPublisher());
        }

        [Test]
        public void GetConnection_should_throw_if_unitialized()
        {
            Assert.Throws<InvalidOperationException>(() => _subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void Disposing_of_a_connection_after_disconnect_should_not_throw_an_exception()
        {
            _subject.Initialize();
            var connection1 = _subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);
            var connection2 = _subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            connection1.Dispose();
            _subject.Dispose();
            connection2.Dispose();
        }

        [Test]
        public void GetConnection_should_throw_an_exception_after_disposing()
        {
            _subject.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
        }

        [Test]
        public void GetConnection_should_create_a_new_connection_when_there_are_none_available_and_the_max_connection_count_has_not_been_reached()
        {
            InitializeAndWaitForMinPoolSize();

            var connections = new List<IConnection>();
            for (int i = 0; i < _connectionPoolSettings.MinSize; i++)
            {
                // checkout the all the connections in the pool
                connections.Add(_subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
            }

            // this should create a new connection because all the connections in the pool have been checked out
            _subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None);

            // we should have gotten more calls to the connection factory...
            _connectionFactory.ReceivedWithAnyArgs(_connectionPoolSettings.MinSize + 1).Create(null, null);
        }

        [Test]
        public void GetConnection_should_throw_exception_when_waiting_too_long_for_a_connection()
        {
            InitializeAndWaitForMinPoolSize();

            var connections = new List<IConnection>();
            // checkout all the connections in the pool so that we have to wait for one to become available
            for (int i = 0; i < _connectionPoolSettings.MaxSize; i++)
            {
                connections.Add(_subject.GetConnection(TimeSpan.FromMilliseconds(Timeout.Infinite), CancellationToken.None));
            }

            // don't wait for the connections at all
            Assert.Throws<MongoDriverException>(() => _subject.GetConnection(TimeSpan.Zero, CancellationToken.None));
        }

        private void InitializeAndWaitForMinPoolSize()
        {
            int count = 0;
            _connectionFactory.Create(null, null).ReturnsForAnyArgs(c =>
            {
                count++;
                var conn = Substitute.For<IConnection>();
                conn.IsOpen.Returns(true);
                return conn;
            });

            _subject.Initialize();
            if (!SpinWait.SpinUntil(() => count >= _connectionPoolSettings.MinSize, 4000))
            {
                Assert.Fail("Min connection count was never reached.");
            }
        }
    }
}