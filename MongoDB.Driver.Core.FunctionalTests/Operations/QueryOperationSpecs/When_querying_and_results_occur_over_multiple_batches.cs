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
            for (int i = 0; i < 10; i++)
            {
                docs.Add(new BsonDocument("_id", i));
            }

            InsertData(docs.ToArray());
        }

        protected override void When()
        {
            var op = new QueryOperation<BsonDocument>
            {
                Collection = _collection,
                BatchSize = 2,
                Query = new BsonDocument()
            };

            _results = ReadCursorToEnd(ExecuteOperation(op));
        }

        [Test]
        public void All_the_documents_should_be_returned()
        {
            Assert.AreEqual(10, _results.Count);
        }
    }
}
