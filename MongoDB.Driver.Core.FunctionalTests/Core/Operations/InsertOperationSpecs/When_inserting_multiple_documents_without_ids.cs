using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.InsertOperationSpecs
{
    [TestFixture]
    public class When_inserting_multiple_documents_without_ids : Specification
    {
        private List<BsonDocument> _docsToInsert;

        protected override void When()
        {
            _docsToInsert = new List<BsonDocument>
            {
                new BsonDocument("x", 2),
                new BsonDocument("x", 3),
                new BsonDocument("x", 4)
            };
            var op = new InsertOperation<BsonDocument>()
            {
                Collection = _collection,
                Documents = _docsToInsert
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_local_documents_should_still_not_have_an_id()
        {
            Assert.IsFalse(_docsToInsert[0].Elements.Any(x => x.Name == "_id"));
            Assert.IsFalse(_docsToInsert[1].Elements.Any(x => x.Name == "_id"));
            Assert.IsFalse(_docsToInsert[2].Elements.Any(x => x.Name == "_id"));
        }

        [Test]
        public void The_documents_should_exist_in_the_database()
        {
            var result = FindOne<BsonDocument>(new BsonDocument("x", _docsToInsert[0]["x"]));
            Assert.AreEqual(2, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("x", _docsToInsert[1]["x"]));
            Assert.AreEqual(3, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("x", _docsToInsert[2]["x"]));
            Assert.AreEqual(4, result["x"].AsInt32);
        }
    }
}