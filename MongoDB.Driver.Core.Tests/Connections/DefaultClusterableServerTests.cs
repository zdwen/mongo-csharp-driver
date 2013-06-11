using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MongoDB.Driver;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System.IO;
using System.Net;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using NSubstitute;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class DefaultClusterableServerTests
    {
        private DnsEndPoint _dnsEndPoint;
        private MockConnectionFactory _connectionFactory;
        private MockChannelProvider _channelProvider;
        private DefaultClusterableServerSettings _serverSettings;

        [SetUp]
        public void SetUp()
        {
            _serverSettings = new DefaultClusterableServerSettings(
                connectRetryFrequency: TimeSpan.FromMilliseconds(-1),
                heartbeatFrequency: TimeSpan.FromMilliseconds(-1),
                maxDocumentSizeDefault: 1024 * 4,
                maxMessageSizeDefault: 1024 * 8);

            _dnsEndPoint = new DnsEndPoint("localhost", 27017);

            _connectionFactory = new MockConnectionFactory();
            _channelProvider = new MockChannelProvider(_dnsEndPoint, _connectionFactory);
        }

        [Test]
        public void GetConnection_should_throw_if_unitialized()
        {
            var subject = CreateSubject();

            Assert.Throws<InvalidOperationException>(() => subject.GetChannel());
        }

        [Test]
        public void Disposed_should_put_the_server_in_a_disposed_state()
        {
            SetupLookupDescriptionResults();
            var subject = CreateSubject();

            subject.Dispose();

            Assert.AreEqual(ServerStatus.Disposed, subject.Description.Status);
        }

        [Test]
        public void Disposing_of_a_connection_after_disconnect_should_not_throw_an_exception()
        {
            SetupLookupDescriptionResults();
            var subject = CreateSubject();
            subject.Initialize();

            var connection = subject.GetChannel();
            var connection2 = subject.GetChannel();

            connection.Dispose();
            subject.Dispose();
            connection2.Dispose();
        }

        [Test]
        public void GetConnection_should_throw_an_exception_after_disposing()
        {
            SetupLookupDescriptionResults();
            var subject = CreateSubject();

            subject.Dispose();

            Assert.Throws<ObjectDisposedException>(() => subject.GetChannel());
        }

        [Test]
        public void Description_should_return_defaults_when_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            var description = subject.Description;

            Assert.AreEqual(_serverSettings.MaxDocumentSizeDefault, description.MaxDocumentSize);
            Assert.AreEqual(_serverSettings.MaxMessageSizeDefault, description.MaxMessageSize);
            Assert.IsNull(description.ReplicaSetInfo);
            Assert.AreEqual(ServerType.Unknown, description.Type);
            Assert.AreEqual(ServerStatus.Disposed, description.Status);
        }

        [Test]
        public void Description_should_return_defaults_when_connecting_but_cannot_connect()
        {
            _connectionFactory.RegisterOpenCallback(() => new MongoDriverException("AHH!!!"));
            var subject = CreateSubject();
            subject.Initialize();

            var description = subject.Description;

            Assert.AreEqual(_serverSettings.MaxDocumentSizeDefault, description.MaxDocumentSize);
            Assert.AreEqual(_serverSettings.MaxMessageSizeDefault, description.MaxMessageSize);
            Assert.IsNull(description.ReplicaSetInfo);
            Assert.AreEqual(ServerType.Unknown, description.Type);
            Assert.AreEqual(ServerStatus.Connecting, description.Status);
        }

        [Test]
        public void Description_should_return_connected_when_connected()
        {
            SetupLookupDescriptionResults();
            var subject = CreateSubject();
            subject.Initialize();

            // let the information get pulled back...
            Thread.Sleep(4000);

            var description = subject.Description;

            Assert.AreEqual(_serverSettings.MaxDocumentSizeDefault, description.MaxDocumentSize);
            Assert.AreEqual(_serverSettings.MaxMessageSizeDefault, description.MaxMessageSize);
            Assert.IsNull(description.ReplicaSetInfo);
            Assert.AreEqual(ServerType.StandAlone, description.Type);
            Assert.AreEqual(ServerStatus.Connected, description.Status);
        }

        [Test]
        public void Invalidate_should_change_description_to_connecting_and_update_against_the_server()
        {
            SetupLookupDescriptionResults();
            var subject = CreateSubject();
            subject.Initialize();

            // let the information get pulled back...
            Thread.Sleep(4000);

            var updates = new List<ServerDescription>();
            subject.DescriptionUpdated += (o, e) => updates.Add(e.Value);
            subject.Invalidate();

            // let the information get pulled back again...
            Thread.Sleep(4000);

            // 2 updates should have come, the first the change to connecting, the second back to connected
            Assert.AreEqual(2, updates.Count);
            var firstDescription = updates[0];
            Assert.AreEqual(ServerStatus.Connecting, firstDescription.Status);
        }

        private DefaultClusterableServer CreateSubject()
        {
            return new DefaultClusterableServer(_serverSettings, _dnsEndPoint, _channelProvider, _connectionFactory, new EventPublisher(), new TraceManager());
        }

        private void SetupLookupDescriptionResults()
        {
            var isMasterResult = new BsonDocument
            {
                { "ok", 1},
                { "me", _dnsEndPoint.ToString() },
                { "ismaster", 1 }
            };

            var buildInfoResult = new BsonDocument
            {
                { "ok", 1},
                { "bits", 64 },
                { "gitVersion", "git version" },
                { "sysInfo", "system info" },
                { "version", "1.2.3" }
            };

            _connectionFactory.RegisterResponse("ismaster", isMasterResult);
            _connectionFactory.RegisterResponse("buildinfo", buildInfoResult);
        }
    }
}
