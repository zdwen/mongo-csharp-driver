using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.AggregationOperation
{
    [TestFixture]
    public class When_executing_a_valid_pipeline : Specification
    {
        private List<BsonDocument> _results;

        protected override void Given()
        {
            InsertData(
                new BsonDocument("x", 1),
                new BsonDocument("x", 2),
                new BsonDocument("x", 3));
        }

        protected override void When()
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

                _results = op.ToList();
            }
        }

        [Test]
        public void All_the_documents_should_come_back()
        {
            Assert.AreEqual(3, _results.Count);
        }
    }
}