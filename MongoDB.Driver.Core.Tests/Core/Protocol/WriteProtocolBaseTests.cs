using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Sessions;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Protocol
{
    [TestFixture]
    public class WriteProtocolBaseTests
    {
        [Test]
        public void When_not_using_a_write_concern_a_getLastError_message_should_not_be_piggy_backed()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });

            var subject = CreateSubject(WriteConcern.Unacknowledged);
            subject.Execute(channel);

            Assert.AreEqual(subject.BufferLengthWithoutWriteConcern, sentBufferLength);
        }

        [Test]
        public void When_using_a_write_concern_a_getLastError_message_should_be_piggy_backed()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, null));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            subject.Execute(channel);

            Assert.Greater(sentBufferLength, subject.BufferLengthWithoutWriteConcern);
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_is_not_ok_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(false, "an error"));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute(channel));
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_has_an_err_message_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, "an error"));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute(channel));
        }

        private TestWriteProtocol CreateSubject(WriteConcern writeConcern)
        {
            return new TestWriteProtocol(new CollectionNamespace("admin", "YAY"), writeConcern);
        }

        private ReplyMessage CreateWriteConcernResult(bool ok, string err)
        {
            var doc = new BsonDocument
            {
                { "ok", ok }
            };

            if (err != null)
            {
                doc["err"] = err;
            }

            return ProtocolMessageHelper.BuildReplyMessage(new[] { doc });
        }

        private class TestWriteProtocol : WriteProtocolBase<WriteConcernResult>
        {
            public TestWriteProtocol(CollectionNamespace collection, WriteConcern writeConcern)
                : base(collection, BsonBinaryReaderSettings.Defaults, writeConcern, BsonBinaryWriterSettings.Defaults)
            {
            }

            public int BufferLengthWithoutWriteConcern { get; set; }

            public override WriteConcernResult Execute(IChannel channel)
            {
                var deleteMessage = new DeleteMessage(
                    Collection,
                    new BsonDocument(),
                    DeleteFlags.Single,
                    WriterSettings);

                SendPacketWithWriteConcernResult sendMessageResult;
                using (var request = new BufferedRequestPacket())
                {
                    request.AddMessage(deleteMessage);
                    BufferLengthWithoutWriteConcern = (int)request.Stream.Length;

                    sendMessageResult = SendPacketWithWriteConcern(channel, request, WriteConcern);
                }

                return ReadWriteConcernResult(channel, sendMessageResult);
            }
        }
    }
}