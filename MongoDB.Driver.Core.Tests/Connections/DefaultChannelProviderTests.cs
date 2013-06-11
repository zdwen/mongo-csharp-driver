using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
    public class DefaultChannelProviderTests
    {
        private IConnectionFactory _connectionFactory;
        private DefaultChannelProviderSettings _channelProviderSettings;
        private DefaultChannelProvider _subject;

        [SetUp]
        public void SetUp()
        {
            _channelProviderSettings = new DefaultChannelProviderSettings(
                connectionMaxIdleTime: TimeSpan.FromMilliseconds(-1),
                connectionMaxLifeTime: TimeSpan.FromMilliseconds(-1),
                maxSize: 4,
                minSize: 2,
                sizeMaintenanceFrequency: TimeSpan.FromMilliseconds(-1),
                maxWaitQueueSize: 2);

            _connectionFactory = Substitute.For<IConnectionFactory>();

            var dnsEndPoint = new DnsEndPoint("localhost", 27017);
            _subject = new DefaultChannelProvider(_channelProviderSettings, dnsEndPoint, _connectionFactory, new EventPublisher(), new TraceManager());
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
            var channel = _subject.GetChannel();
            var channel2 = _subject.GetChannel();

            channel.Dispose();
            _subject.Dispose();
            channel2.Dispose();
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

            var channels = new List<IChannel>();
            for (int i = 0; i < _channelProviderSettings.MinSize; i++)
            {
                // checkout the all the channels in the pool
                channels.Add(_subject.GetChannel());
            }

            // this should create a new connection because all the channels in the pool have been checked out
            _subject.GetChannel();

            // we should have gotten more calls to the connection factory...
            _connectionFactory.ReceivedWithAnyArgs(_channelProviderSettings.MinSize + 1).Create(null);
        }

        [Test]
        public void GetConnection_should_throw_exception_when_waiting_too_long_for_a_connection()
        {
            InitializeAndWaitForMinPoolSize();

            var channels = new List<IChannel>();
            // checkout all the channels in the pool so that we have to wait for one to become available
            for (int i = 0; i < _channelProviderSettings.MaxSize; i++)
            {
                channels.Add(_subject.GetChannel());
            }

            // don't wait for the channels at all
            Assert.Throws<MongoDriverException>(() => _subject.GetChannel(TimeSpan.Zero));
        }

        [Test]
        public void ConnectionChannel_ReceiveMessage_should_throw_an_exception_if_the_received_message_is_not_the_requested_message()
        {
            InitializeAndWaitForMinPoolSize();

            var replyMessage = ProtocolHelper.BuildReplyMessage(new[] { new BsonDocument() }, 3);

            var channel = _subject.GetChannel();
            var connection = (IConnection)channel.GetType().GetProperty("Connection").GetValue(channel, null);
            connection.ReceiveMessage().ReturnsForAnyArgs(replyMessage);

            Assert.Throws<MongoProtocolException>(() => channel.ReceiveMessage(new ReceiveMessageParameters(0)));
        }

        private void InitializeAndWaitForMinPoolSize()
        {
            int count = 0;
            _connectionFactory.Create(null).ReturnsForAnyArgs(c =>
            {
                count++;
                var conn = Substitute.For<IConnection>();
                conn.IsOpen.Returns(true);
                return conn;
            });

            _subject.Initialize();
            while (count < _channelProviderSettings.MinSize) ;
        }
    }
}