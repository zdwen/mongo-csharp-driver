using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.QueryOperationSpecs
{
    public class When_querying_without_a_filter : Specification
    {
        private List<BsonDocument> _results;

        protected override void Given()
        {
            var docs = new List<BsonDocument>();
            for (int i = 0; i < 100; i++)
            {
                docs.Add(new BsonDocument("_id", i));
            }

            InsertData(docs.ToArray());
        }

        protected override void When()
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<BsonDocument>
                {
                    Collection = _collection,
                    BatchSize = 50, // will cause 2 batches...
                    Query = new BsonDocument(),
                    Session = session
                };

                _results = findOp.ToList();
            }
        }

        [Test]
        public void All_the_documents_should_be_returned()
        {
            Assert.AreEqual(100, _results.Count);
        }
    }
}
