using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Protocol.Messages;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class StreamConnectionTests
    {
        private IStreamFactory _streamFactory;
        private StreamConnection _subject;

        [SetUp]
        public void SetUp()
        {
            _streamFactory = Substitute.For<IStreamFactory>();

            _subject = new StreamConnection(
                StreamConnectionSettings.Defaults,
                new DnsEndPoint("localhost", 27017),
                _streamFactory,
                Substitute.For<IEventPublisher>());
        }

        [Test]
        public void Open_should_change_the_state_of_the_connection_to_open()
        {
            var stream = Substitute.For<Stream>();
            stream.CanRead.Returns(true);
            stream.CanWrite.Returns(true);
            _streamFactory.Create(null).ReturnsForAnyArgs(stream);
            SetupOpen(stream);

            _subject.Open();

            Assert.IsTrue(_subject.IsOpen);
        }

        [Test]
        public void Open_should_be_callable_more_than_once()
        {
            var stream = Substitute.For<Stream>();
            stream.CanRead.Returns(true);
            stream.CanWrite.Returns(true);
            _streamFactory.Create(null).ReturnsForAnyArgs(stream);

            SetupOpen(stream);

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
            Assert.Throws<InvalidOperationException>(() => _subject.Receive());
        }

        [Test]
        public void Receive_should_throw_a_MongoSocketReadTimeoutException_if_a_timeout_socket_exception_is_encountered()
        {
            var stream = Substitute.For<Stream>();
            stream.CanRead.Returns(true);
            stream.CanWrite.Returns(true);
            _streamFactory.Create(null).ReturnsForAnyArgs(stream);
            
            SetupOpen(stream);

            _subject.Open();

            stream.WhenForAnyArgs(x => x.Read(null, 0, 0))
                .Do(c => { throw new SocketException((int)SocketError.TimedOut); });

            Assert.Throws<MongoSocketReadTimeoutException>(() => _subject.Receive());
        }

        [Test]
        public void SendMessage_should_throw_if_the_connection_has_not_been_opened()
        {
            var request = ProtocolMessageHelper.BuildRequestMessage("dummy_cmd");

            Assert.Throws<InvalidOperationException>(() => _subject.Send(request));
        }

        [Test]
        public void Send_should_throw_a_MongoSocketWriteTimeoutException_if_a_timeout_socket_exception_is_encountered()
        {
            var stream = Substitute.For<Stream>();
            stream.CanRead.Returns(true);
            stream.CanWrite.Returns(true);
            _streamFactory.Create(null).ReturnsForAnyArgs(stream);

            SetupOpen(stream);

            _subject.Open();

            stream.WhenForAnyArgs(x => x.Write(null, 0, 0))
                .Do(c => { throw new SocketException((int)SocketError.TimedOut); });

            var request = ProtocolMessageHelper.BuildRequestMessage("dummy_cmd");

            Assert.Throws<MongoSocketWriteTimeoutException>(() => _subject.Send(request));
        }

        private void SetupOpen(Stream stream)
        {
            var bytes = ProtocolMessageHelper.EncodeReplyMessage(new [] { new BsonDocument("connectionId", 42) }).ToList();
            int bytesIndex = 0;
            stream.Read(null, 0, 0).ReturnsForAnyArgs(c =>
            {
                var args = c.Args();
                var buffer = (byte[])args[0];
                var offset = (int)args[1];
                var count = (int)args[2];
                bytes.CopyTo(bytesIndex, buffer, offset, count);
                bytesIndex += count;
                return count;
            });
        }
    }
}