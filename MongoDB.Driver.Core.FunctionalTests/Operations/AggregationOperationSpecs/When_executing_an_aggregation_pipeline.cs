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
    public class When_executing_an_aggregation_pipeline : Specification
    {
        private List<BsonDocument> _results;

        protected override void Given()
        {
            var docs = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                docs.Add(new BsonDocument("_id", i));
            }

            InsertData(docs.ToArray());
        }

        protected override void When()
        {
            var pipeline = new [] { new BsonDocument("$match", new BsonDocument()) };

            using (var session = BeginSession())
            {
                var op = new AggregateOperation<BsonDocument>
                {
                    BatchSize = 2,
                    Collection = _collection,
                    Pipeline = pipeline,
                    Session = session
                };

                _results = op.ToList();
            }
        }

        [Test]
        public void All_the_documents_should_be_returned()
        {
            Assert.AreEqual(10, _results.Count);
        }
    }
}