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
    public class When_inserting_multiple_documents_with_ids : Specification
    {
        private List<BsonDocument> _docsToInsert;

        protected override void When()
        {
            _docsToInsert = new List<BsonDocument>
            {
                new BsonDocument("_id", 1).Add("x", 2),
                new BsonDocument("_id", 2).Add("x", 3),
                new BsonDocument("_id", 3).Add("x", 4)
            };
            var op = new InsertOperation()
            {
                Collection = _collection,
                Documents = _docsToInsert,
                DocumentType = typeof(BsonDocument)
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_local_documents_should_have_ids_id()
        {
            Assert.IsTrue(_docsToInsert[0].Elements.Any(x => x.Name == "_id"));
            Assert.IsTrue(_docsToInsert[1].Elements.Any(x => x.Name == "_id"));
            Assert.IsTrue(_docsToInsert[2].Elements.Any(x => x.Name == "_id"));
        }

        [Test]
        public void The_local_document_ids_should_be_unchanged()
        {
            Assert.AreEqual(1, _docsToInsert[0]["_id"].AsInt32);
            Assert.AreEqual(2, _docsToInsert[1]["_id"].AsInt32);
            Assert.AreEqual(3, _docsToInsert[2]["_id"].AsInt32);
        }

        [Test]
        public void The_documents_should_exist_in_the_database()
        {
            var result = FindOne<BsonDocument>(new BsonDocument("_id", 1));
            Assert.AreEqual(2, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("_id", 2));
            Assert.AreEqual(3, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("_id", 3));
            Assert.AreEqual(4, result["x"].AsInt32);
        }
    }
}