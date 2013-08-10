using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.QueryOperationSpecs
{
    public class When_querying_with_field_selection : Specification
    {
        private List<BsonDocument> _results;

        protected override void Given()
        {
            var docs = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                docs.Add(new BsonDocument("_id", i).Add("x", i * 2).Add("y", i * 3));
            }

            InsertData(docs.ToArray());
        }

        protected override void When()
        {
            var op = new QueryOperation<BsonDocument>
            {
                Collection = _collection,
                Fields = new BsonDocument("x", 1).Add("_id", 0),
                Query = new BsonDocument(),
            };

            _results = ReadCursorToEnd(ExecuteOperation(op));
        }

        [Test]
        public void All_the_documents_should_be_returned()
        {
            Assert.AreEqual(10, _results.Count);
        }

        [Test]
        public void All_the_documents_should_only_have_1_element()
        {
            Assert.IsTrue(_results.All(doc => doc.ElementCount == 1));
        }
    }
}
