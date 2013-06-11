using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class DefaultConnectionTests
    {
        private IStreamFactory _streamFactory;
        private DefaultConnection _subject;

        [SetUp]
        public void SetUp()
        {
            _streamFactory = Substitute.For<IStreamFactory>();

            _subject = new DefaultConnection(
                new DnsEndPoint("localhost", 27017),
                _streamFactory,
                Substitute.For<IEventPublisher>(),
                new TraceManager());
        }

        [Test]
        public void Open_should_change_the_state_of_the_connection_to_open()
        {
            _subject.Open();

            Assert.IsTrue(_subject.IsOpen);
        }

        [Test]
        public void Open_should_be_callable_more_than_once()
        {
            _subject.Open();
            _subject.Open();
            _subject.Open();

            Assert.IsTrue(_subject.IsOpen);
        }

        [Test]
        public void Open_should_throw_a_MongoConnectTimeoutException_if_a_timeout_socket_exception_is_encountered()
        {
            _streamFactory.WhenForAnyArgs(x => x.Create(null))
                .Do(c => { throw new SocketException((int)SocketError.TimedOut); });

            Assert.Throws<MongoConnectTimeoutException>(() => _subject.Open());
        }

        [Test]
        public void ReceiveMessage_should_throw_if_the_connection_has_not_been_opened()
        {
            Assert.Throws<InvalidOperationException>(() => _subject.ReceiveMessage());
        }

        [Test]
        public void Receive_should_throw_a_MongoSocketReadTimeoutException_if_a_timeout_socket_exception_is_encountered()
        {
            var stream = Substitute.For<Stream>();
            stream.WhenForAnyArgs(x => x.Read(null, 0, 0))
                .Do(c => { throw new SocketException((int)SocketError.TimedOut); });

            _streamFactory.Create(null).ReturnsForAnyArgs(stream);

            _subject.Open();

            Assert.Throws<MongoSocketReadTimeoutException>(() => _subject.ReceiveMessage());
        }

        [Test]
        public void SendMessage_should_throw_if_the_connection_has_not_been_opened()
        {
            var request = ProtocolHelper.BuildRequestMessage("dummy_cmd");

            Assert.Throws<InvalidOperationException>(() => _subject.SendMessage(request));
        }

        [Test]
        public void Send_should_throw_a_MongoSocketWriteTimeoutException_if_a_timeout_socket_exception_is_encountered()
        {
            var stream = Substitute.For<Stream>();
            stream.WhenForAnyArgs(x => x.Write(null, 0, 0))
                .Do(c => { throw new SocketException((int)SocketError.TimedOut); });

            _streamFactory.Create(null).ReturnsForAnyArgs(stream);

            _subject.Open();

            var request = ProtocolHelper.BuildRequestMessage("dummy_cmd");

            Assert.Throws<MongoSocketWriteTimeoutException>(() => _subject.SendMessage(request));
        }
    }
}