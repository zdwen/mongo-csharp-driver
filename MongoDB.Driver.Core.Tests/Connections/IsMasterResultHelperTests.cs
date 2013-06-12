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
    public class IsMasterResultHelperTests
    {
        [Test]
        public void GetMaxDocumentSize_should_return_default_if_unspecified()
        {
            var isMasterResult = new BsonDocument();

            var result = IsMasterResultHelper.GetMaxDocumentSize(isMasterResult, 100);

            Assert.AreEqual(100, result);
        }

        [Test]
        public void GetMaxDocumentSize_should_return_value_if_specified()
        {
            var isMasterResult = new BsonDocument("maxBsonObjectSize", 200);

            var result = IsMasterResultHelper.GetMaxDocumentSize(isMasterResult, 100);

            Assert.AreEqual(200, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_the_max_document_size_plus_1MB_if_it_is_specified_and_larger_than_the_default_max_message_size()
        {
            var isMasterResult = new BsonDocument();

            var result = IsMasterResultHelper.GetMaxMessageSize(isMasterResult, 100, 200);

            Assert.AreEqual(1124, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_the_default_if_it_is_specified_and_larger_than_the_default_max_message_size_minus_1MB()
        {
            var isMasterResult = new BsonDocument();

            var result = IsMasterResultHelper.GetMaxMessageSize(isMasterResult, 100, 2048);

            Assert.AreEqual(2048, result);
        }

        [Test]
        public void GetMaxMessageSize_should_return_value_if_specified()
        {
            var isMasterResult = new BsonDocument("maxMessageSizeBytes", 300);

            var result = IsMasterResultHelper.GetMaxMessageSize(isMasterResult, 100, 200);

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
            var isMasterResult = BsonSerializer.Deserialize<BsonDocument>(json);

            var serverType = IsMasterResultHelper.GetServerType(isMasterResult);

            Assert.AreEqual(expected, serverType);
        }

        [Test]
        public void GetReplicaSetInfo_should_return_null_if_the_isMasterResult_is_not_a_replica_set()
        {
            var isMasterResult = new BsonDocument();

            var replicaSetInfo = IsMasterResultHelper.GetReplicaSetInfo(AddressFamily.InterNetwork, isMasterResult);

            Assert.IsNull(replicaSetInfo);
        }

        [Test]
        public void GetReplicaSetInfo_should_return_correct_info_when_the_isMasterResult_is_a_replica_set()
        {
            var isMasterResult = new BsonDocument("setName", "funny")
                .Add("primary", "localhost:1000")
                .Add("hosts", new BsonArray(new[] { "localhost:1000", "localhost:1001" }))
                .Add("passives", new BsonArray(new[] { "localhost:1002" }))
                .Add("arbiters", new BsonArray(new[] { "localhost:1003" }))
                .Add("tags", new BsonDocument("tag1", "a").Add("tag2", "b"));

            var replicaSetInfo = IsMasterResultHelper.GetReplicaSetInfo(AddressFamily.InterNetwork, isMasterResult);

            Assert.AreEqual("funny", replicaSetInfo.Name);
            Assert.AreEqual(new DnsEndPoint("localhost", 1000, AddressFamily.InterNetwork), replicaSetInfo.Primary);
            Assert.AreEqual(4, replicaSetInfo.Members.Count());
            Assert.AreEqual(2, replicaSetInfo.Tags.Count());
        }
    }
}