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
            using (var session = BeginSession())
            {
                _docsToInsert = new List<BsonDocument>
                {
                    new BsonDocument("x", 2),
                    new BsonDocument("x", 3),
                    new BsonDocument("x", 4)
                };
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = _docsToInsert,
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                op.Execute();
            }
        }

        [Test]
        public void The_local_documents_should_have_been_assigned_an_id()
        {
            Assert.IsTrue(_docsToInsert[0].Elements.Any(x => x.Name == "_id"));
            Assert.IsTrue(_docsToInsert[1].Elements.Any(x => x.Name == "_id"));
            Assert.IsTrue(_docsToInsert[2].Elements.Any(x => x.Name == "_id"));
        }

        [Test]
        public void The_local_document_ids_should_be_the_first_elements()
        {
            var firstElement = _docsToInsert[0].GetElement(0);
            Assert.AreEqual("_id", firstElement.Name);

            firstElement = _docsToInsert[1].GetElement(0);
            Assert.AreEqual("_id", firstElement.Name);

            firstElement = _docsToInsert[2].GetElement(0);
            Assert.AreEqual("_id", firstElement.Name);
        }

        [Test]
        public void The_document_should_exist_in_the_database()
        {
            var result = FindOne<BsonDocument>(new BsonDocument("_id", _docsToInsert[0]["_id"]));
            Assert.AreEqual(2, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("_id", _docsToInsert[1]["_id"]));
            Assert.AreEqual(3, result["x"].AsInt32);

            result = FindOne<BsonDocument>(new BsonDocument("_id", _docsToInsert[2]["_id"]));
            Assert.AreEqual(4, result["x"].AsInt32);
        }
    }
}