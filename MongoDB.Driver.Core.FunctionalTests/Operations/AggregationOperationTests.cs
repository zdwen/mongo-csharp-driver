using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AggregationOperationTests : DatabaseTest
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            InsertData(
                new BsonDocument("x", 1),
                new BsonDocument("x", 2),
                new BsonDocument("x", 3));
        }

        [Test]
        public void Executing_a_valid_pipeline_should_have_results()
        {
            var pipeline = new [] { new BsonDocument("$match", new BsonDocument()) };

            using (var session = BeginSession())
            {
                var op = new AggregationOperation<BsonDocument>
                {
                    Collection = _collection,
                    Pipeline = pipeline,
                    Session = session
                };

                var count = op.Count();
                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Executing_an_invalid_pipeline_should_throw_a_MongoOperationException()
        {
            var pipeline = new[] { new BsonDocument("$invalid", new BsonDocument()) };

            using (var session = BeginSession())
            {
                var op = new AggregationOperation<BsonDocument>
                {
                    Collection = _collection,
                    Pipeline = pipeline,
                    Session = session
                };

                Assert.Throws<MongoOperationException>(() => op.ToList());
            }
        }
    }
}