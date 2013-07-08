using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Operations;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Protocol
{
    [TestFixture]
    public class InsertOperationTests
    {
        private const int BATCH_SIZE = 1024;

        [Test]
        [TestCase(false, InsertFlags.ContinueOnError, 3, 0)]
        [TestCase(false, InsertFlags.None, 3, 2)]
        [TestCase(true, InsertFlags.ContinueOnError, 3, 3)]
        [TestCase(true, InsertFlags.None, 3, 3)]
        public void Should_send_the_proper_number_of_getLastError_messages(bool useWriteConcern, InsertFlags flags, int numBatches, int numGetLastErrors)
        {
            var writeConcern = useWriteConcern ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged;
            var channel = Substitute.For<IServerChannel>();
            channel.Server.Returns(ServerDescriptionBuilder.Build(b =>
            {
                b.MaxDocumentSize(BATCH_SIZE);
                b.MaxMessageSize(BATCH_SIZE);
            }));
            channel.Receive(null).ReturnsForAnyArgs(c => CreateWriteConcernResult(true, null));

            var subject = CreateSubject(flags, writeConcern, numBatches);
            subject.Execute(channel);

            channel.ReceivedWithAnyArgs(numGetLastErrors).Receive(null);
        }

        private IEnumerable<BsonDocument> CreateDocumentBatch(int numBatches)
        {
            const int headerSize = 34;
            const int docsPerBatch = 2;

            int docOverhead = 0;
            using (var memStream = new MemoryStream())
            using (var writer = BsonWriter.Create(memStream))
            {
                var plain = new BsonDocument("bytes", new byte[0]);
                BsonSerializer.Serialize(writer, plain);
                writer.Flush();
                docOverhead = (int)memStream.Length;
            }

            int docSize = (BATCH_SIZE - headerSize) / docsPerBatch;

            var bytes = new byte[docSize - docOverhead];
            for (int i = 0; i < numBatches; i++)
            {
                for (int j = 0; j < docsPerBatch; j++)
                {
                    yield return new BsonDocument("bytes", bytes);
                }
            }
        }

        private InsertOperation CreateSubject(InsertFlags flags, WriteConcern writeConcern, int numBatches)
        {
            return new InsertOperation(
                new CollectionNamespace("admin", "YAY"),
                new BsonBinaryReaderSettings(),
                new BsonBinaryWriterSettings(),
                writeConcern,
                false, // don't generate ids, it would change the size of the documents...
                true,
                typeof(BsonDocument),
                CreateDocumentBatch(numBatches),
                flags,
                0);
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
    }
}