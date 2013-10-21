using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class IsMasterResultTests
    {
        [Test]
        public void GetMaxDocumentSize_should_return_default_if_unspecified()
        {
            var doc = new BsonDocument();
            var isMasterResult = new IsMasterResult(doc);
            var result = isMasterResult.GetMaxDocumentSize(100);
            Assert.AreEqual(100, result);
        }

        [Test]
        public void GetMaxDocumentSize_should_return_value_if_specified()
        {
            var doc = new BsonDocument("maxBsonObjectSize", 200);
            var isMasterResult = new IsMasterResult(doc);
            var result = isMasterResult.GetMaxDocumentSize(100);
            Assert.AreEqual(200, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_the_max_document_size_plus_1MB_if_it_is_specified_and_larger_than_the_default_max_message_size()
        {
            var doc = new BsonDocument();
            var isMasterResult = new IsMasterResult(doc);
            var result = isMasterResult.GetMaxMessageSize(100, 200);
            Assert.AreEqual(1124, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_the_default_if_it_is_specified_and_larger_than_the_default_max_message_size_minus_1MB()
        {
            var doc = new BsonDocument();
            var isMasterResult = new IsMasterResult(doc);
            var result = isMasterResult.GetMaxMessageSize(100, 2048);
            Assert.AreEqual(2048, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_value_if_specified()
        {
            var doc = new BsonDocument("maxMessageSizeBytes", 300);
            var isMasterResult = new IsMasterResult(doc);
            var result = isMasterResult.GetMaxMessageSize(100, 200);

            Assert.AreEqual(300, result);
        }

        [TestCase("{setName: \"funny\", ismaster: true}", ServerType.ReplicaSetPrimary)]
        [TestCase("{setName: \"funny\", secondary: true}", ServerType.ReplicaSetSecondary)]
        [TestCase("{setName: \"funny\", arbiterOnly: true}", ServerType.ReplicaSetArbiter)]
        [TestCase("{setName: \"funny\" }", ServerType.ReplicaSetOther)]
        [TestCase("{isreplicaset: 1 }", ServerType.ReplicaSetOther)]
        [TestCase("{msg: \"isdbgrid\" }", ServerType.ShardRouter)]
        [TestCase("{msg: \"something\" }", ServerType.StandAlone)]
        public void GetServerType_should_return_correct_server_type(string json, ServerType expected)
        {
            var doc = BsonSerializer.Deserialize<BsonDocument>(json);
            var isMasterResult = new IsMasterResult(doc);
            var serverType = isMasterResult.ServerType;
            Assert.AreEqual(expected, serverType);
        }

        [Test]
        public void GetReplicaSetInfo_should_return_null_if_the_isMasterResult_is_not_a_replica_set()
        {
            var doc = new BsonDocument();
            var isMasterResult = new IsMasterResult(doc);
            var replicaSetInfo = isMasterResult.GetReplicaSetInfo(AddressFamily.InterNetwork);
            Assert.IsNull(replicaSetInfo);
        }

        [Test]
        public void GetReplicaSetInfo_should_return_correct_info_when_the_isMasterResult_is_a_replica_set()
        {
            var doc = new BsonDocument("setName", "funny")
                .Add("primary", "localhost:1000")
                .Add("hosts", new BsonArray(new[] { "localhost:1000", "localhost:1001" }))
                .Add("passives", new BsonArray(new[] { "localhost:1002" }))
                .Add("arbiters", new BsonArray(new[] { "localhost:1003" }))
                .Add("tags", new BsonDocument("tag1", "a").Add("tag2", "b"));
            var isMasterResult = new IsMasterResult(doc);
            var replicaSetInfo = isMasterResult.GetReplicaSetInfo(AddressFamily.InterNetwork);

            Assert.AreEqual("funny", replicaSetInfo.Name);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000, AddressFamily.InterNetwork), replicaSetInfo.Primary);
            Assert.AreEqual(4, replicaSetInfo.Members.Count());
            Assert.AreEqual(2, replicaSetInfo.Tags.Count());
        }
    }
}