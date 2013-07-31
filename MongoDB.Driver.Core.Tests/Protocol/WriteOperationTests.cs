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
using MongoDB.Driver.Core.Sessions;
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
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });

            var channelProvider = Substitute.For<IServerChannelProvider>();
            channelProvider.Server.Returns(ServerDescriptionBuilder.Build(b => { }));
            channelProvider.GetChannel(Timeout.InfiniteTimeSpan, CancellationToken.None).ReturnsForAnyArgs(channel);

            var session = Substitute.For<ISession>();
            session.CreateServerChannelProvider(null).ReturnsForAnyArgs(channelProvider);

            var subject = CreateSubject(session, WriteConcern.Unacknowledged);
            subject.Execute();

            Assert.AreEqual(subject.BufferLengthWithoutWriteConcern, sentBufferLength);
        }

        [Test]
        public void When_using_a_write_concern_a_getLastError_message_should_be_piggy_backed()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, null));

            var channelProvider = Substitute.For<IServerChannelProvider>();
            channelProvider.Server.Returns(ServerDescriptionBuilder.Build(b => { }));
            channelProvider.GetChannel(Timeout.InfiniteTimeSpan, CancellationToken.None).ReturnsForAnyArgs(channel);

            var session = Substitute.For<ISession>();
            session.CreateServerChannelProvider(null).ReturnsForAnyArgs(channelProvider);

            var subject = CreateSubject(session, WriteConcern.Acknowledged);
            subject.Execute();

            Assert.Greater(sentBufferLength, subject.BufferLengthWithoutWriteConcern);
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_is_not_ok_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(false, "an error"));

            var channelProvider = Substitute.For<IServerChannelProvider>();
            channelProvider.Server.Returns(ServerDescriptionBuilder.Build(b => { }));
            channelProvider.GetChannel(Timeout.InfiniteTimeSpan, CancellationToken.None).ReturnsForAnyArgs(channel);

            var session = Substitute.For<ISession>();
            session.CreateServerChannelProvider(null).ReturnsForAnyArgs(channelProvider);

            var subject = CreateSubject(session, WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute());
        }

        [Test]
        public void When_a_write_concern_is_specified_and_getLastError_has_an_err_message_an_exception_should_be_thrown()
        {
            int sentBufferLength = 0;
            var channel = Substitute.For<IChannel>();
            channel.WhenForAnyArgs(c => c.Send(null)).Do(c => { sentBufferLength = c.Arg<IRequestPacket>().Length; });
            channel.Receive(null).ReturnsForAnyArgs(CreateWriteConcernResult(true, "an error"));

            var channelProvider = Substitute.For<IServerChannelProvider>();
            channelProvider.Server.Returns(ServerDescriptionBuilder.Build(b => { }));
            channelProvider.GetChannel(Timeout.InfiniteTimeSpan, CancellationToken.None).ReturnsForAnyArgs(channel);

            var session = Substitute.For<ISession>();
            session.CreateServerChannelProvider(null).ReturnsForAnyArgs(channelProvider);

            var subject = CreateSubject(session, WriteConcern.Acknowledged);
            Assert.Throws<MongoWriteConcernException>(() => subject.Execute());
        }

        private TestWriteOperation CreateSubject(ISession session, WriteConcern writeConcern)
        {
            return new TestWriteOperation(session, new CollectionNamespace("admin", "YAY"), writeConcern);
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

        private class TestWriteOperation : WriteOperation<WriteConcernResult>
        {
            public TestWriteOperation(ISession session, CollectionNamespace collection, WriteConcern writeConcern)
            {
                Session = session;
                Collection = collection;
                WriteConcern = writeConcern;
            }

            public int BufferLengthWithoutWriteConcern { get; set; }

            public override WriteConcernResult Execute()
            {
                using (var channelProvider = Session.CreateServerChannelProvider(null))
                using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
                {
                    var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
                    var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);

                    var deleteMessage = new DeleteMessage(
                        Collection,
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
}