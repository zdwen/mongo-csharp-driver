using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Operations;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Protocol
{
    [TestFixture]
    public class WriteOperationTests
    {
        [Test]
        public void When_not_using_a_write_concern_a_getLastError_message_should_not_be_piggy_backed()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IServerChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Server.Returns(ServerDescriptionBuilder.Build(b => { }));

            var subject = CreateSubject(WriteConcern.Unacknowledged);
            subject.Execute(channel);

            Assert.AreEqual(subject.BufferLengthWithoutWriteConcern, sentBufferLength);
        }

        [Test]
        public void When_using_a_write_concern_a_getLastError_message_should_be_piggy_backed()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IServerChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Server.Returns(ServerDescriptionBuilder.Build(b => { }));

            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, null));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            subject.Execute(channel);

            Assert.Greater(sentBufferLength, subject.BufferLengthWithoutWriteConcern);
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_is_not_ok_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IServerChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Server.Returns(ServerDescriptionBuilder.Build(b => { }));

            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(false, "an error"));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute(channel));
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_has_an_err_message_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IServerChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Server.Returns(ServerDescriptionBuilder.Build(b => { }));

            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, "an error"));

            var subject = CreateSubject(WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute(channel));
        }

        private TestWriteOperation CreateSubject(WriteConcern writeConcern)
        {
            return new TestWriteOperation(
                new CollectionNamespace("admin", "YAY"),
                new BsonBinaryReaderSettings(),
                new BsonBinaryWriterSettings(),
                writeConcern);
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

            return ProtocolHelper.BuildReplyMessage(new[] { doc });
        }

        private class TestWriteOperation : WriteOperation
        {
            public TestWriteOperation(CollectionNamespace collectionNamespace, BsonBinaryReaderSettings readerSettings, BsonBinaryWriterSettings writerSettings, WriteConcern writeConcern)
                : base(collectionNamespace, readerSettings, writerSettings, writeConcern)
            { }

            public int BufferLengthWithoutWriteConcern { get; set; }

            public WriteConcernResult Execute(IServerChannel channel)
            {
                var readerSettings = GetServerAdjustedReaderSettings(channel.Server);
                var writerSettings = GetServerAdjustedWriterSettings(channel.Server);

                var deleteMessage = new DeleteMessage(
                    CollectionNamespace,
                    new BsonDocument(),
                    DeleteFlags.Single,
                    writerSettings);

                SendPacketWithWriteConcernResult sendMessageResult;
                using (var request = new BufferedRequestPacket())
                {
                    request.AddMessage(deleteMessage);
                    BufferLengthWithoutWriteConcern = (int)request.Stream.Length;

                    sendMessageResult = SendPacketWithWriteConcern(channel, request, WriteConcern, writerSettings);
                }

                return ReadWriteConcernResult(channel, sendMessageResult, readerSettings);
            }
        }
    }
}