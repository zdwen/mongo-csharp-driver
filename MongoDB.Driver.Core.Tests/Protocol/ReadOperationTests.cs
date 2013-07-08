using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Mocks;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Support;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Protocol
{
    [TestFixture]
    public class ReadOperationTests
    {
        [Test]
        public void WrapQuery_should_return_original_query_when_not_using_a_shard_router_and_no_options_are_specified()
        {
            var subject = new TestReadOperation();
            var server = ServerDescriptionBuilder.Build(x => x.Type(ServerType.StandAlone));

            var query = new BsonDocument("x", 1);

            var result = subject.TestWrapQuery(server, query, null, null);

            Assert.AreEqual(query, result);
        }

        [Test]
        public void WrapQuery_should_add_options_when_they_are_present()
        {
            var subject = new TestReadOperation();
            var server = ServerDescriptionBuilder.Build(x => x.Type(ServerType.StandAlone));

            var query = new BsonDocument("x", 1);
            var options = new BsonDocument("option1", "a");

            var result = subject.TestWrapQuery(server, query, options, null);

            Assert.AreEqual(query, result["$query"]);
            Assert.AreEqual("a", result["option1"].AsString);
        }

        [Test]
        [TestCase(ReadPreferenceMode.Nearest, true, true)]
        [TestCase(ReadPreferenceMode.Nearest, false, true)]
        [TestCase(ReadPreferenceMode.Primary, true, false)]
        [TestCase(ReadPreferenceMode.Primary, false, false)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred, true, true)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred, false, true)]
        [TestCase(ReadPreferenceMode.Secondary, true, true)]
        [TestCase(ReadPreferenceMode.Secondary, false, true)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred, true, true)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred, false, false)]
        public void WrapQuery_should_add_read_preference_when_targetting_a_secondary_on_a_sharded_system_with_tagsets(ReadPreferenceMode readPreferenceMode, bool useTagSets, bool shouldBeWrapped)
        {
            var subject = new TestReadOperation();
            var server = ServerDescriptionBuilder.Build(x => x.Type(ServerType.ShardRouter));

            var query = new BsonDocument("x", 1);

            var readPreference = new ReadPreference(readPreferenceMode);
            if (useTagSets)
            {
                var tagSet = new ReplicaSetTagSet();
                tagSet.Add("foo", "bar");
                readPreference = new ReadPreference(readPreferenceMode, new[] { tagSet });
            }

            var result = subject.TestWrapQuery(server, query, null, readPreference);
            var formattedReadPreference = new BsonDocument
            {
                { "mode", MongoUtils.ToCamelCase(readPreferenceMode.ToString()) },
                { "tags", new BsonArray().Add(new BsonDocument("foo", "bar")), useTagSets }
            };

            if (shouldBeWrapped)
            {
                Assert.AreEqual(query, result["$query"]);
                Assert.AreEqual(formattedReadPreference, result["$readPreference"]);
            }
            else
            {
                Assert.AreEqual(query, result);
            }
        }

        private class TestReadOperation : ReadOperation
        {
            public TestReadOperation()
                : base(new CollectionNamespace("foo", "bar"), new BsonBinaryReaderSettings(), new BsonBinaryWriterSettings())
            {
            }

            public BsonDocument TestWrapQuery(ServerDescription server, BsonDocument query, BsonDocument options, ReadPreference readPreference)
            {
                var result = WrapQuery(server, query, options, readPreference);

                // wrap query uses a BsonDocumentWrapper which makes checking its contents impossible.
                var doc = new BsonDocument();
                using (var writer = new BsonDocumentWriter(doc, new BsonDocumentWriterSettings()))
                {
                    BsonSerializer.Serialize(writer, result);
                    writer.Flush();
                }

                return doc;
            }
        }
    }
}